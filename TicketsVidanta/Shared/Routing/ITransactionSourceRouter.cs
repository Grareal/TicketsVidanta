namespace TicketsVidanta.Shared.Routing;

public interface ITransactionSourceRouter
{
    string? Resolve(string? tcGroup, string? trxCode);
}
