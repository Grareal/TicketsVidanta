namespace TicketsVidanta.Features.Tickets.ProcesarCheque;

public interface ITicketProcessor
{
    Task<Response> ProcessAsync(Request request, CancellationToken cancellationToken);
}
