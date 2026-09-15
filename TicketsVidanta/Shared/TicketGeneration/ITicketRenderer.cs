using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.TicketGeneration;

public interface ITicketRenderer
{
    Task<GeneratedTicket> RenderAsync(
        CheckProcessingContext context,
        CheckDetail detail,
        CancellationToken cancellationToken);
}
