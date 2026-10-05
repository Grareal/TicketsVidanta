using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Shared.Routing;

public sealed class OptionsTransactionSourceRouter(IOptions<TransactionRoutingOptions> options)
    : ITransactionSourceRouter
{
    public string? Resolve(
    string? tcGroup,
    string? trxCode,
    string? resort)
{
    if (string.IsNullOrWhiteSpace(tcGroup) ||
        string.IsNullOrWhiteSpace(trxCode))
        return null;

    var matches = options.Value.Rules.Where(rule =>
    string.Equals(rule.TcGroup.Trim(), tcGroup.Trim(), StringComparison.OrdinalIgnoreCase)
    &&
    string.Equals(rule.TrxCode.Trim(), trxCode.Trim(), StringComparison.OrdinalIgnoreCase)
    &&
    (
                string.Equals(
            rule.Resort?.Trim(),
            resort?.Trim(),
            StringComparison.OrdinalIgnoreCase)
    )
)
.Take(2)
.ToArray();

/*Console.WriteLine(
    $"ROUTER => TcGroup={tcGroup}, TrxCode={trxCode}, Resort={resort}");

foreach (var match in matches)
{
    Console.WriteLine(
        $"MATCH => RuleResort={match.Resort}, SourceSystem={match.SourceSystem}");
}
*/
    return matches.Length == 1 &&
           !string.IsNullOrWhiteSpace(matches[0].SourceSystem)
        ? matches[0].SourceSystem.Trim()
        : null;
}
}
