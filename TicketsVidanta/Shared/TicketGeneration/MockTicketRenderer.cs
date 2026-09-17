using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.TicketGeneration;

public sealed class MockTicketRenderer : ITicketRenderer
{
    private const string TestImagePath =
        @"C:\Users\enriquemeza\Desktop\udf\base64\ticket.jpg";

    public Task<GeneratedTicket> RenderAsync(
        CheckProcessingContext context,
        CheckDetail detail,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var imageBytes = File.ReadAllBytes(TestImagePath);

        Console.WriteLine($"Imagen cargada: {imageBytes.Length} bytes");

        return Task.FromResult(
            new GeneratedTicket(
                imageBytes,
                "image/jpeg"));
    }
}