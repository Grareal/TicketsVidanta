namespace TicketsVidanta.Shared.Database.Master;

public interface IMasterTransactionRepository
{
    Task<IReadOnlyList<MasterTransaction>> GetPendingTransactionsAsync(int batchSize, CancellationToken cancellationToken);
    Task MarkAsProcessedAsync(string transactionId, Guid correlationId, CancellationToken cancellationToken);
    Task MarkAsFailedAsync(string transactionId, Guid correlationId, string errorMessage, CancellationToken cancellationToken);
}
