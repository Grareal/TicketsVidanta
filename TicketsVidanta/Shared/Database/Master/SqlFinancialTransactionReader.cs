using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Routing;

namespace TicketsVidanta.Shared.Database.Master;

public sealed class SqlFinancialTransactionReader(
    IConfiguration configuration,
    IOptions<FinancialTransactionSourceOptions> sourceOptions,
    IOptions<TransactionRoutingOptions> routingOptions,
    ITransactionSourceRouter router) : IFinancialTransactionReader
{
    public async Task<IReadOnlyList<FinancialTransactionCandidate>> ReadAsync(CancellationToken cancellationToken)
    {
        var rules = routingOptions.Value.Rules;
        if (rules.Count == 0) return [];

        var predicates = new List<string>();
        await using var command = new SqlCommand();
        for (var index = 0; index < rules.Count; index++)
        {
            predicates.Add($"(TC_GROUP=@Group{index} AND CONVERT(nvarchar(80), TRX_CODE)=@Code{index})");
            command.Parameters.AddWithValue($"@Group{index}", rules[index].TcGroup);
            command.Parameters.AddWithValue($"@Code{index}", rules[index].TrxCode);
        }

        command.CommandText = $"""
            SELECT TOP (@BatchSize)
                RESORT, TRX_DATE, BUSINESS_DATE, TRX_NO, TC_GROUP, TRX_CODE,
                CHEQUE_NUMBER, RESV_NAME_ID, ROOM, REFERENCE, REMARK
            FROM [TCADBOPE].[dbo].[FINANCIAL_TRANSACTIONS_P_DET_CLOUD]
            WHERE ({string.Join(" OR ", predicates)})
              AND TRX_DATE >= DATEADD(DAY, -@LookbackDays, GETDATE())
              AND RESORT IS NOT NULL
              AND RESV_NAME_ID IS NOT NULL
              AND CHEQUE_NUMBER IS NOT NULL
            ORDER BY TRX_DATE DESC;
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
}
