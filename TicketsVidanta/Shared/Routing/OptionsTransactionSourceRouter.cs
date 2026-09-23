using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Shared.Routing;

public sealed class OptionsTransactionSourceRouter(IOptions<TransactionRoutingOptions> options)
    : ITransactionSourceRouter
{
    public string? Resolve(string? tcGroup, string? trxCode)
    {
        if (string.IsNullOrWhiteSpace(tcGroup) || string.IsNullOrWhiteSpace(trxCode))
            return null;

        var matches = options.Value.Rules.Where(rule =>
            string.Equals(rule.TcGroup.Trim(), tcGroup.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(rule.TrxCode.Trim(), trxCode.Trim(), StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();

        return matches.Length == 1 && !string.IsNullOrWhiteSpace(matches[0].SourceSystem)
            ? matches[0].SourceSystem.Trim()
            : null;
    }
}
