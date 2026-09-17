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

        var result = await client.FindReservationAsync("123456", Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal("https://gateway.example/rsv/v1/hotels/TEST/reservations/123456", captured!.RequestUri!.ToString());
        Assert.Equal("app-key", captured.Headers.GetValues("x-app-key").Single());
        Assert.Equal("TEST", captured.Headers.GetValues("x-hotelid").Single());
        Assert.Equal("Bearer", captured.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task UploadDocumentAsync_UsesOfficialAttachmentPayload()
    {
        string? payload = null;
        var handler = new StubHandler(request =>
        {
            payload = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            var response = new HttpResponseMessage(HttpStatusCode.Created);
            response.Headers.Location = new Uri("https://gateway.example/med/config/v1/fileAttachments/DOC-1");
            return response;
        });
        var client = CreateClient(handler);

        var result = await client.UploadDocumentAsync(
            new DocumentUploadRequest("123456", "ticket.png", "image/png", new byte[] { 1, 2, 3 }, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("DOC-1", result.DocumentId);
        using var json = JsonDocument.Parse(payload!);
        Assert.Equal("Reservation", json.RootElement.GetProperty("linkType").GetString());
        Assert.Equal("123456", json.RootElement.GetProperty("linkId").GetString());
        Assert.Equal("AQID", json.RootElement.GetProperty("fileAttachment").GetString());
    }

    private static OperaCloudClient CreateClient(HttpMessageHandler handler)
    {
        var options = Options.Create(new OperaCloudOptions
        {
            UseMock = false,
            GatewayUrl = "https://gateway.example",
            AppKey = "app-key",
            HotelId = "TEST",
            AttachmentUserName = "INTEGRATION"
        });
        return new OperaCloudClient(new HttpClient(handler), new StubTokenProvider(), options);
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
