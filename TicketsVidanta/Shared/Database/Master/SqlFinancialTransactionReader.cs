using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Routing;
using System.Text.RegularExpressions;

namespace TicketsVidanta.Shared.Database.Master;

public sealed partial class SqlFinancialTransactionReader(
    IConfiguration configuration,
    IOptions<FinancialTransactionSourceOptions> sourceOptions,
    IOptions<TransactionRoutingOptions> routingOptions,
    ITransactionSourceRouter router) : IFinancialTransactionReader
{
    public async Task<IReadOnlyList<FinancialTransactionCandidate>> ReadAsync(CancellationToken cancellationToken)
    {
        var rules = router is ITransactionRouteCatalog catalog
            ? catalog.GetRules()
            : routingOptions.Value.Rules;
        if (rules.Count == 0) return [];
        var source = sourceOptions.Value;
        var resort = Column(source.ResortColumn);
        var transactionDate = Column(source.TransactionDateColumn);
        var businessDate = Column(source.BusinessDateColumn);
        var transactionNumber = Column(source.TransactionNumberColumn);
        var tcGroupColumn = Column(source.TcGroupColumn);
        var trxCodeColumn = Column(source.TrxCodeColumn);
        var checkNumber = Column(source.CheckNumberColumn);
        var reservationId = Column(source.ReservationIdColumn);
        var room = Column(source.RoomColumn);
        var reference = Column(source.ReferenceColumn);
        var remark = Column(source.RemarkColumn);

        var predicates = new List<string>();
        await using var command = new SqlCommand();
        for (var index = 0; index < rules.Count; index++)
        {
            predicates.Add($"({tcGroupColumn}=@Group{index} AND CONVERT(nvarchar(80), {trxCodeColumn})=@Code{index})");
            command.Parameters.AddWithValue($"@Group{index}", rules[index].TcGroup);
            command.Parameters.AddWithValue($"@Code{index}", rules[index].TrxCode);
        }

        command.CommandText = $"""
            SELECT TOP (@BatchSize)
                {resort}, {transactionDate}, {businessDate}, {transactionNumber}, {tcGroupColumn}, {trxCodeColumn},
                {checkNumber}, {reservationId}, {room}, {reference}, {remark}
            FROM {Table(source.Schema, source.Table)}
            WHERE ({string.Join(" OR ", predicates)})
              AND {transactionDate} >= DATEADD(DAY, -@LookbackDays, GETDATE())
              AND {resort} IS NOT NULL
              AND {reservationId} IS NOT NULL
              AND {checkNumber} IS NOT NULL
            ORDER BY {transactionDate} DESC;
            """;
        command.Parameters.AddWithValue("@BatchSize", sourceOptions.Value.BatchSize);
        command.Parameters.AddWithValue("@LookbackDays", sourceOptions.Value.LookbackDays);

        var connectionString = configuration.GetConnectionString(sourceOptions.Value.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"La cadena '{sourceOptions.Value.ConnectionStringName}' de la consulta maestra no está configurada.");

        await using var connection = new SqlConnection(connectionString);
        command.Connection = connection;
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<FinancialTransactionCandidate>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var tcGroup = Value(reader, 4)!;
            var trxCode = Value(reader, 5)!;
            var sourceSystem = router.Resolve(tcGroup, trxCode);
            if (sourceSystem is null) continue;
            result.Add(new FinancialTransactionCandidate(
                Value(reader, 0)!, Date(reader, 1), Date(reader, 2), Value(reader, 3), tcGroup,
                trxCode, Value(reader, 6)!, Value(reader, 7)!, Value(reader, 8),
                Value(reader, 9), Value(reader, 10), sourceSystem));
        }
        return result;
    }

    private static string? Value(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal))?.Trim();
    private static DateTime? Date(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal));

    private static string Table(string schema, string table) => $"{Column(schema)}.{Column(table)}";
    private static string Column(string identifier) => IdentifierRegex().IsMatch(identifier)
        ? $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]"
        : throw new InvalidOperationException($"Identificador SQL invalido en FinancialTransactionSource: {identifier}");

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_@$#]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}
