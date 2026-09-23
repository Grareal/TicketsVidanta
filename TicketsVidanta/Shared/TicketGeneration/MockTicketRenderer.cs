using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.TicketGeneration;

public sealed class MockTicketRenderer : ITicketRenderer
{
    private static readonly byte[] PlaceholderPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M/wHwAF/gL+Xw3LAAAAAElFTkSuQmCC");

    public Task<GeneratedTicket> RenderAsync(
        CheckProcessingContext context,
        CheckDetail detail,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            new GeneratedTicket(
                PlaceholderPng,
                "image/png"));
    }
}
