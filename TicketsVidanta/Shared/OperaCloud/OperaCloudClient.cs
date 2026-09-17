using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Exceptions;

namespace TicketsVidanta.Shared.OperaCloud;

public sealed class OperaCloudClient(
    HttpClient httpClient,
    IOperaCloudTokenProvider tokenProvider,
    IOptions<OperaCloudOptions> options) : IOperaCloudClient
{
    public async Task<ReservationLookupResult> FindReservationAsync(
        string reservationId,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var value = options.Value;
        var path = $"/rsv/v1/hotels/{Uri.EscapeDataString(value.HotelId)}/reservations/{Uri.EscapeDataString(reservationId)}";
        using var request = await CreateRequestAsync(HttpMethod.Get, path, correlationId, cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return new ReservationLookupResult(false, reservationId);
        if (!response.IsSuccessStatusCode)
            throw new OperaCloudException(await ErrorAsync("consulta de reserva", response, cancellationToken));
        return new ReservationLookupResult(true, reservationId);
    }

    public async Task<DocumentUploadResult> UploadDocumentAsync(
        DocumentUploadRequest request,
        CancellationToken cancellationToken)
    {
        var value = options.Value;
        using var message = await CreateRequestAsync(
            HttpMethod.Post, "/med/config/v1/fileAttachments", request.CorrelationId, cancellationToken);
        message.Content = JsonContent.Create(new
        {
            fileName = request.FileName,
            linkId = request.ReservationId,
            overwriteExistingFileYN = "N",
            description = value.AttachmentDescription,
            linkType = "Reservation",
            hotelId = value.HotelId,
            userName = value.AttachmentUserName,
            globalYN = "N",
            fileAttachment = Convert.ToBase64String(request.Content.Span)
        });

        using var response = await httpClient.SendAsync(message, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Created)
            return new DocumentUploadResult(false, null,
                await ErrorAsync("carga de adjunto", response, cancellationToken));

        var location = response.Headers.Location?.ToString();
        var documentId = string.IsNullOrWhiteSpace(location)
            ? null
            : location.TrimEnd('/').Split('/').Last();
        return new DocumentUploadResult(true, documentId, null);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string path,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var value = options.Value;
        var message = new HttpRequestMessage(method, BuildUri(value.GatewayUrl, path));
        message.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", await tokenProvider.GetAccessTokenAsync(requestId, cancellationToken));
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.TryAddWithoutValidation("x-app-key", value.AppKey);
        message.Headers.TryAddWithoutValidation("x-hotelid", value.HotelId);
        message.Headers.TryAddWithoutValidation("X-Request-Id", requestId.ToString());
        if (!string.IsNullOrWhiteSpace(value.ExternalSystemCode))
            message.Headers.TryAddWithoutValidation("x-externalSystem", value.ExternalSystemCode);
        return message;
    }

    private static Uri BuildUri(string gatewayUrl, string path) =>
        new(new Uri(gatewayUrl.TrimEnd('/') + "/"), path.TrimStart('/'));

    private static async Task<string> ErrorAsync(
        string operation,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Length > 500) body = body[..500];
        return $"OHIP rechazó la {operation} con HTTP {(int)response.StatusCode}: {body}";
    }
}
