using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Exceptions;

namespace TicketsVidanta.Shared.OperaCloud;

public sealed class OperaCloudTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<OperaCloudOptions> options) : IOperaCloudTokenProvider
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAtUtc;

    public async Task<string> GetAccessTokenAsync(Guid requestId, CancellationToken cancellationToken)
    {
        if (_accessToken is not null && _expiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(5))
            return _accessToken;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && _expiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(5))
                return _accessToken;

            var value = options.Value;
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(value.GatewayUrl, value.TokenPath));
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{value.ClientId}:{value.ClientSecret}")));
            request.Headers.TryAddWithoutValidation("x-app-key", value.AppKey);
            request.Headers.TryAddWithoutValidation("X-Request-Id", requestId.ToString());

            var fields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = value.GrantType
            };
            if (string.Equals(value.GrantType, "password", StringComparison.OrdinalIgnoreCase))
            {
                fields["username"] = value.Username;
                fields["password"] = value.Password;
            }
            else
            {
                fields["scope"] = value.Scope;
                request.Headers.TryAddWithoutValidation("enterpriseId", value.EnterpriseId);
            }
            request.Content = new FormUrlEncodedContent(fields);

            var client = httpClientFactory.CreateClient("OperaCloudAuth");
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new OperaCloudException($"OAuth OHIP respondió {(int)response.StatusCode}: {Limit(body)}");

            using var json = JsonDocument.Parse(body);
            if (!json.RootElement.TryGetProperty("access_token", out var tokenElement))
                throw new OperaCloudException("OAuth OHIP no devolvió access_token.");

            _accessToken = tokenElement.GetString()
                ?? throw new OperaCloudException("OAuth OHIP devolvió un access_token vacío.");
            var expiresIn = json.RootElement.TryGetProperty("expires_in", out var expiresElement)
                && expiresElement.TryGetInt32(out var seconds) ? seconds : 3600;
            _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            return _accessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static Uri BuildUri(string gatewayUrl, string path) =>
        new(new Uri(gatewayUrl.TrimEnd('/') + "/"), path.TrimStart('/'));

    private static string Limit(string value) => value.Length <= 500 ? value : value[..500];
}
