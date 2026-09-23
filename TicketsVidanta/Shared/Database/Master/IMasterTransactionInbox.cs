namespace TicketsVidanta.Shared.Database.Master;

public interface IMasterTransactionInbox
{
    Task<int> AddMissingAsync(IReadOnlyList<FinancialTransactionCandidate> transactions, CancellationToken cancellationToken);
}
