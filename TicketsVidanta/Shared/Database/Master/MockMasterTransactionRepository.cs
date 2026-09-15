namespace TicketsVidanta.Shared.Database.Master;

/// <summary>Repositorio sin persistencia que expone un registro de ejemplo una sola vez.</summary>
public sealed class MockMasterTransactionRepository : IMasterTransactionRepository
{
    private int _served;

    public Task<IReadOnlyList<MasterTransaction>> GetPendingTransactionsAsync(int batchSize, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<MasterTransaction> result = Interlocked.Exchange(ref _served, 1) == 0
            ? [new("MOCK-TX-001", "TEST", "123456", "CHK-001", "100", "MOCK", "MOCK")]
            : [];
        return Task.FromResult(result.Take(batchSize).ToArray() as IReadOnlyList<MasterTransaction>);
    }

    public Task MarkAsProcessedAsync(string transactionId, Guid correlationId, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task MarkAsFailedAsync(string transactionId, Guid correlationId, string errorMessage, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
