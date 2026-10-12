using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Shared.Database.Master;

/// <summary>Repositorio sin persistencia que expone un registro de ejemplo una sola vez.</summary>
public sealed class MockMasterTransactionRepository : IMasterTransactionRepository
{
    private int _served;

    public Task<IReadOnlyList<MasterTransaction>> GetPendingTransactionsAsync(
        int batchSize,
        MasterTransactionSelection? selection,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<MasterTransaction> result = Interlocked.Exchange(ref _served, 1) == 0
            ? [new("MOCK-TX-001", "TEST", "123456", "CHK-001", "100", "MOCK", "MOCK")]
            : [];
        if (selection is not null)
            result = result.Where(transaction => Matches(transaction, selection)).ToArray();
        return Task.FromResult(result.Take(batchSize).ToArray() as IReadOnlyList<MasterTransaction>);
    }

    public Task MarkAsProcessedAsync(string transactionId, Guid correlationId, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task MarkAsFailedAsync(string transactionId, Guid correlationId, string errorMessage, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    private static bool Matches(MasterTransaction transaction, MasterTransactionSelection selection)
    {
        static bool In(IReadOnlyList<string> values, string? value) =>
            values.Count == 0 || values.Contains(value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        var documentMatches = selection.Documents.Count == 0 || selection.Documents.Any(document =>
            string.Equals(document.Resort, transaction.Resort, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(document.ReservationId, transaction.ReservationId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(document.CheckNumber, transaction.CheckNumber, StringComparison.OrdinalIgnoreCase));
        return documentMatches && In(selection.SourceSystems, transaction.SourceSystem) &&
               In(selection.Resorts, transaction.Resort) && In(selection.TcGroups, transaction.TcGroup) &&
               In(selection.TrxCodes, transaction.TrxCode);
    }
}
