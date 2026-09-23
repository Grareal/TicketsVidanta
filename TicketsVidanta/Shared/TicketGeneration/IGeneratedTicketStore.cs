namespace TicketsVidanta.Shared.TicketGeneration;

public interface IGeneratedTicketStore
{
    Task SaveAsync(string fileName, GeneratedTicket ticket, CancellationToken cancellationToken);
}
