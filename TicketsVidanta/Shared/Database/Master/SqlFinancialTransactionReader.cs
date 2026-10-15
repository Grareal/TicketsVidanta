using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketsVidanta.Features.DatabaseConfiguration;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Routing;

namespace TicketsVidanta.Shared.Database.Master;

internal sealed partial class SqlFinancialTransactionReader(
    IConfiguration configuration,
    IOptions<FinancialTransactionSourceOptions> sourceOptions,
    IOptions<TransactionRoutingOptions> routingOptions,
    ITransactionSourceRouter router,
    IConfigurationRepository? configRepository = null,
    ConnectionSecretProtector? protector = null,
    ILogger<SqlFinancialTransactionReader>? logger = null) : IFinancialTransactionReader
{
    public async Task<IReadOnlyList<FinancialTransactionCandidate>> ReadAsync(CancellationToken cancellationToken)
    {
        var rules = router is ITransactionRouteCatalog catalog
            ? catalog.GetRules()
            : routingOptions.Value.Rules;
        if (rules.Count == 0)
        {
            logger?.LogWarning("[SqlFinancialTransactionReader] No hay reglas de enrutamiento activas cargadas desde ConfigTransactionRoutes ni configuración.");
            return [];
        }

        var source = sourceOptions.Value;
        var lookbackDays = source.LookbackDays;
        var toDate = DateTime.Now;
        var fromDate = toDate.Date.AddDays(-lookbackDays);

        logger?.LogInformation(
            "[SqlFinancialTransactionReader] Rango de búsqueda: Desde={FromDate:yyyy-MM-dd HH:mm:ss} Hasta={ToDate:yyyy-MM-dd HH:mm:ss} (LookbackDays={LookbackDays}, BatchSize={BatchSize})",
            fromDate, toDate, lookbackDays, source.BatchSize);

        logger?.LogInformation(
            "[SqlFinancialTransactionReader] Reglas activas ({Count}): {Rules}",
            rules.Count,
            string.Join(", ", rules.Select(r => $"{r.TcGroup}+{r.TrxCode}->{r.SourceSystem}")));

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
            // Do not wrap the source columns in CONVERT: it prevents seeks on the source index.
            // A varchar parameter can still be implicitly converted when TRX_CODE is numeric.
            predicates.Add($"({tcGroupColumn}=@Group{index} AND {trxCodeColumn}=@Code{index})");
            command.Parameters.Add(new SqlParameter($"@Group{index}", System.Data.SqlDbType.VarChar, 80) { Value = rules[index].TcGroup });
            command.Parameters.Add(new SqlParameter($"@Code{index}", System.Data.SqlDbType.VarChar, 80) { Value = rules[index].TrxCode });
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
        if (string.IsNullOrWhiteSpace(connectionString) && configRepository is not null && protector is not null)
        {
            var connections = await configRepository.GetConnectionsAsync(cancellationToken);
            var match = connections.FirstOrDefault(c =>
                string.Equals(c.Name, sourceOptions.Value.ConnectionStringName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Name, "Base de datos principal", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.DatabaseName, "TCADBOPE", StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                var stored = await configRepository.GetConnectionAsync(match.Id, cancellationToken);
                if (!string.IsNullOrWhiteSpace(stored?.ProtectedConnectionString))
                    connectionString = protector.Unprotect(stored.ProtectedConnectionString);
            }
        }

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"La cadena '{sourceOptions.Value.ConnectionStringName}' de la consulta maestra no está configurada ni en appsettings ni en ConfigDatabaseConnections.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        logger?.LogInformation(
            "[SqlFinancialTransactionReader] Conectando a Servidor={DataSource}, Base={InitialCatalog}, Tabla={Schema}.{Table}",
            builder.DataSource, builder.InitialCatalog, source.Schema, source.Table);

        await using var connection = new SqlConnection(connectionString);
        command.Connection = connection;
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<FinancialTransactionCandidate>();
        var rawCount = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            rawCount++;
            var resortVal = Value(reader, 0)!;
            var trxDate = Date(reader, 1);
            var busDate = Date(reader, 2);
            var trxNo = Value(reader, 3);
            var tcGroup = Value(reader, 4)!;
            var trxCode = Value(reader, 5)!;
            var checkNum = Value(reader, 6)!;
            var resvId = Value(reader, 7)!;
            var roomVal = Value(reader, 8);
            var refVal = Value(reader, 9);
            var remarkVal = Value(reader, 10);

            var sourceSystem = router.Resolve(tcGroup, trxCode);

            if (rawCount <= 10)
            {
                logger?.LogInformation(
                    "[SqlFinancialTransactionReader] Muestra #{Index}: Resort={Resort}, TrxDate={TrxDate:yyyy-MM-dd}, TrxNo={TrxNo}, TC_GROUP={TcGroup}, TRX_CODE={TrxCode}, Cheque={Cheque}, Hab={Room} -> Ruta: {RouteMatch}",
                    rawCount, resortVal, trxDate, trxNo, tcGroup, trxCode, checkNum, roomVal,
                    sourceSystem is not null ? $"COINCIDE ({sourceSystem})" : "SIN COINCIDENCIA (Descartado)");
            }

            if (sourceSystem is null) continue;

            result.Add(new FinancialTransactionCandidate(
                resortVal, trxDate, busDate, trxNo, tcGroup,
                trxCode, checkNum, resvId, roomVal,
                refVal, remarkVal, sourceSystem));
        }

        logger?.LogInformation(
            "[SqlFinancialTransactionReader] Filas leídas de {Schema}.{Table}: {RawCount}. Coincidencias con rutas válidas: {MatchedCount}.",
            source.Schema, source.Table, rawCount, result.Count);

        if (rawCount == 0)
        {
            logger?.LogWarning(
                "[SqlFinancialTransactionReader] Cantidad leída = 0 registros. " +
                "Verifique si los TRX_CODE en ConfigTransactionRoutes coinciden con los valores reales en {Schema}.{Table}. " +
                "Filtros enviados a SQL: {Predicates}",
                source.Schema, source.Table, string.Join(" OR ", predicates));
        }

        return result;
    }

    private static string? Value(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal))?.Trim();
    private static DateTime? Date(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetValue(ordinal);

        return value switch
        {
            DateTime dt => dt,
            DateTimeOffset dto => dto.DateTime,
            string s when DateTime.TryParse(s, out var parsed) => parsed,
            _ => throw new InvalidOperationException(
                    $"Tipo de fecha no soportado: {value.GetType().FullName}")
        };
    }
    private static string Table(string schema, string table) => $"{Column(schema)}.{Column(table)}";
    private static string Column(string identifier) => IdentifierRegex().IsMatch(identifier)
        ? $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]"
        : throw new InvalidOperationException($"Identificador SQL invalido en FinancialTransactionSource: {identifier}");

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_@$#]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}
