using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.OperaCloud;

namespace TicketsVidanta.Tests;

public sealed class OperaCloudClientTests
{
    [Fact]
    public async Task FindReservationAsync_UsesOfficialReservationRouteAndHeaders()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = CreateClient(handler);

        var result = await client.FindReservationAsync("VINV", "123456", Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal("https://gateway.example/rsv/v1/hotels/OPERA-VINV/reservations/123456", captured!.RequestUri!.ToString());
        Assert.Equal("app-key", captured.Headers.GetValues("x-app-key").Single());
        Assert.Equal("OPERA-VINV", captured.Headers.GetValues("x-hotelid").Single());
        Assert.Equal("Bearer", captured.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task UploadDocumentAsync_UsesOfficialGuestCheckPayload()
    {
        string? payload = null;
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(request =>
        {
            captured = request;
            payload = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            var response = new HttpResponseMessage(HttpStatusCode.Created);
            response.Headers.Location = new Uri("https://gateway.example/csh/v1/hotels/OPERA-VINV/check/CHK-1");
            return response;
        });
        var client = CreateClient(handler);

        var result = await client.UploadDocumentAsync(
            new DocumentUploadRequest("VINV", "123456", "CHK-1", "ticket.jpg", "image/jpeg", new byte[] { 1, 2, 3 }, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("CHK-1", result.DocumentId);
        Assert.Equal("https://gateway.example/csh/v1/hotels/OPERA-VINV/check/CHK-1", captured!.RequestUri!.ToString());
        Assert.Equal("OPERA-VINV", captured.Headers.GetValues("x-hotelid").Single());
        using var json = JsonDocument.Parse(payload!);
        Assert.Equal("AQID", json.RootElement.GetProperty("checkDetails").GetProperty("checkImage").GetString());
        Assert.False(json.RootElement.GetProperty("checkDetails").GetProperty("checkImage").GetString()!.StartsWith("data:"));
    }

    [Fact]
    public void HotelCatalog_UsesDefaultResortAndRejectsUnknownResort()
    {
        var options = Options.Create(new OperaCloudOptions
        {
            DefaultResort = "VINV",
            HotelIdsByResort = new Dictionary<string, string> { ["vinv"] = "OPERA-VINV" }
        });
        var catalog = new OptionsOperaHotelCatalog(options);

        Assert.Equal("OPERA-VINV", catalog.ResolveHotelId(null));
        Assert.Equal("OPERA-VINV", catalog.ResolveHotelId(" VINV "));
        var exception = Assert.Throws<InvalidOperationException>(() => catalog.ResolveHotelId("VILC"));
        Assert.Contains("VILC", exception.Message);
    }

    private static OperaCloudClient CreateClient(HttpMessageHandler handler)
    {
        var options = Options.Create(new OperaCloudOptions
        {
            UseMock = false,
            GatewayUrl = "https://gateway.example",
            AppKey = "app-key",
            DefaultResort = "VINV",
            HotelIdsByResort = new Dictionary<string, string> { ["VINV"] = "OPERA-VINV" }
        });
        return new OperaCloudClient(
            new HttpClient(handler), new StubTokenProvider(), new OptionsOperaHotelCatalog(options), options);
    }

    private sealed class StubTokenProvider : IOperaCloudTokenProvider
    {
        public Task<string> GetAccessTokenAsync(Guid requestId, CancellationToken cancellationToken) =>
            Task.FromResult("token");
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
