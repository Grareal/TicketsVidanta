namespace TicketsVidanta.Shared.TicketGeneration;

/// <summary>Almacén sin persistencia para pruebas unitarias del pipeline.</summary>
public sealed class NullGeneratedTicketStore : IGeneratedTicketStore
{
    public Task SaveAsync(string fileName, GeneratedTicket ticket, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
