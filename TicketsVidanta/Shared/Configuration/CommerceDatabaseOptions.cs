namespace TicketsVidanta.Shared.Configuration;

public sealed class CommerceDatabaseOptions
{
    public const string SectionName = "CommerceDatabases";
    public Dictionary<string, CommerceDatabaseDefinition> Connections { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class CommerceDatabaseDefinition
{
    public string Provider { get; init; } = string.Empty;
    public string ConnectionStringName { get; init; } = string.Empty;
}
