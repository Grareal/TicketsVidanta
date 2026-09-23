namespace TicketsVidanta.Shared.Configuration;

public sealed class TransactionRoutingOptions
{
    public const string SectionName = "TransactionRouting";
    public List<TransactionRoutingRule> Rules { get; init; } = [];
}

public sealed class TransactionRoutingRule
{
    public string TcGroup { get; init; } = string.Empty;
    public string TrxCode { get; init; } = string.Empty;
    public string SourceSystem { get; init; } = string.Empty;
}
