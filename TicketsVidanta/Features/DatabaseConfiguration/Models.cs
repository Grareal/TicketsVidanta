namespace TicketsVidanta.Features.DatabaseConfiguration;

public sealed record DatabaseConnectionSummary(
    Guid Id, string Name, string Provider, string? ServerName, string? DatabaseName, bool IsEnabled, DateTimeOffset UpdatedAtUtc);

public sealed record SaveDatabaseConnectionRequest(
    Guid? Id, string? Name, string? ConnectionString, bool IsEnabled = true);

public sealed record TestDatabaseConnectionRequest(Guid? Id, string? ConnectionString);

public sealed record DatabaseTableInfo(string Schema, string Name, IReadOnlyList<DatabaseColumnInfo> Columns);
public sealed record DatabaseColumnInfo(string Name, string DataType, bool IsNullable);

public sealed record TicketProfile(
    Guid Id,
    string Name,
    string SourceSystem,
    string? Resort,
    Guid ConnectionId,
    bool IsEnabled,
    string BaseSchema,
    string BaseTable,
    string? DetailSchema,
    string? DetailTable,
    string? BaseJoinColumn,
    string? DetailJoinColumn,
    string ReservationColumn,
    string CheckNumberColumn,
    string? ResortColumn,
    IReadOnlyDictionary<string, string> FieldMappings,
    string? CurrencyConstant,
    int MaxRows,
    DateTimeOffset UpdatedAtUtc);

public sealed record SaveTicketProfileRequest(
    Guid? Id,
    string? Name,
    string? SourceSystem,
    string? Resort,
    Guid ConnectionId,
    bool IsEnabled,
    string? BaseSchema,
    string? BaseTable,
    string? DetailSchema,
    string? DetailTable,
    string? BaseJoinColumn,
    string? DetailJoinColumn,
    string? ReservationColumn,
    string? CheckNumberColumn,
    string? ResortColumn,
    Dictionary<string, string>? FieldMappings,
    string? CurrencyConstant,
    int MaxRows = 250);

public sealed record TransactionRoute(
    Guid Id, string TcGroup, string TrxCode, string SourceSystem, bool IsEnabled, DateTimeOffset UpdatedAtUtc);

public sealed record SaveTransactionRouteRequest(
    Guid? Id, string? TcGroup, string? TrxCode, string? SourceSystem, bool IsEnabled = true);

internal sealed record StoredDatabaseConnection(
    Guid Id, string Name, string Provider, string ProtectedConnectionString, bool IsEnabled, DateTimeOffset UpdatedAtUtc);

internal sealed record RuntimeTicketProfile(TicketProfile Profile, string ConnectionString);

internal sealed record RuntimeConfigurationSnapshot(
    IReadOnlyList<TransactionRoute> Routes,
    IReadOnlyList<RuntimeTicketProfile> Profiles)
{
    public static RuntimeConfigurationSnapshot Empty { get; } = new([], []);
}
