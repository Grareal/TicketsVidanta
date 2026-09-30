namespace TicketsVidanta.Shared.Database.Master;

public interface IFinancialTransactionStatusWriter
{
    Task MarkProcessedAsync(
        string resort,
        string checkNumber,
        string status,
        CancellationToken cancellationToken);
}
