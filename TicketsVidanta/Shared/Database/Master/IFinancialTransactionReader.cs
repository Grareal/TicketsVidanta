namespace TicketsVidanta.Shared.Database.Master;

public interface IFinancialTransactionReader
{
    Task<IReadOnlyList<FinancialTransactionCandidate>> ReadAsync(CancellationToken cancellationToken);
}
