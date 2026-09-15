namespace TicketsVidanta.Shared.Processing;

public interface IProcessingRegistry
{
    Task<bool> HasAlreadyBeenProcessedAsync(ProcessingKey key, CancellationToken cancellationToken);
    Task<bool> TryRegisterStartedAsync(ProcessingKey key, Guid correlationId, CancellationToken cancellationToken);
    Task RegisterCompletedAsync(ProcessingKey key, Guid correlationId, CancellationToken cancellationToken);
    Task RegisterFailedAsync(ProcessingKey key, Guid correlationId, string errorMessage, CancellationToken cancellationToken);
    Task<ProcessingRecord?> GetAsync(Guid correlationId, CancellationToken cancellationToken);
}
