using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Shared.Routing;

public interface ITransactionRouteCatalog
{
    IReadOnlyList<TransactionRoutingRule> GetRules();
}
