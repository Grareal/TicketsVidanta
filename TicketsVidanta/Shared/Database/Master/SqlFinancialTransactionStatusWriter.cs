using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Shared.Database.Master;

public sealed class SqlFinancialTransactionStatusWriter(
    IConfiguration configuration,
    IOptions<FinancialTransactionSourceOptions> options) : IFinancialTransactionStatusWriter
{
    public async Task MarkProcessedAsync(
        string resort,
        string checkNumber,
        string status,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE [TCADBOPE].[dbo].[FINANCIAL_TRANSACTIONS_P_DET_CLOUD]
               SET PROCESADO = 1,
                   PROCESADO_DATE = GETUTCDATE(),
                   ESTATUS_PROCESADO = @Status
             WHERE RESORT = @Resort
               AND CHEQUE_NUMBER = @CheckNumber
               AND ISNULL(PROCESADO, 0) = 0;
            """;
        var connectionString = configuration.GetConnectionString(options.Value.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("No está configurada la conexión de transacciones financieras.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Resort", resort.Trim());
        command.Parameters.AddWithValue("@CheckNumber", checkNumber.Trim());
        command.Parameters.AddWithValue("@Status", status);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
