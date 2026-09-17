using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Shared.Database.Master;

public sealed class SqlMasterTransactionRepository(
    ISqlConnectionFactory connectionFactory,
    IOptions<ProcessingOptions> processingOptions) : IMasterTransactionRepository
{
    public async Task<IReadOnlyList<MasterTransaction>> GetPendingTransactionsAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        const string sql = """
            ;WITH Claimable AS
            (
                SELECT TOP (@BatchSize) *
                FROM dbo.MasterTransactions WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE
                    (Status IN ('Pending', 'Failed') OR
                     (Status = 'Processing' AND LeaseUntilUtc < SYSUTCDATETIME()))
                    AND AttemptCount < @MaxAttempts
                ORDER BY CreatedAtUtc, Id
            )
            UPDATE Claimable
               SET Status = 'Processing',
                   AttemptCount = AttemptCount + 1,
                   ClaimedAtUtc = SYSUTCDATETIME(),
                   LeaseUntilUtc = DATEADD(SECOND, @LeaseSeconds, SYSUTCDATETIME()),
                   UpdatedAtUtc = SYSUTCDATETIME(),
                   LastError = NULL
            OUTPUT CONVERT(nvarchar(30), inserted.Id), inserted.Resort, inserted.ReservationId,
                   inserted.CheckNumber, inserted.Room, inserted.Reference, inserted.SourceSystem;
            """;

        var result = new List<MasterTransaction>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        command.Parameters.AddWithValue("@MaxAttempts", processingOptions.Value.MaxAttempts);
        command.Parameters.AddWithValue("@LeaseSeconds", processingOptions.Value.ClaimLeaseSeconds);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new MasterTransaction(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5), reader.GetString(6)));
        }

        return result;
    }

    public Task MarkAsProcessedAsync(string transactionId, Guid correlationId, CancellationToken cancellationToken) =>
        MarkAsync(transactionId, correlationId, "Completed", null, cancellationToken);

    public Task MarkAsFailedAsync(string transactionId, Guid correlationId, string errorMessage,
        CancellationToken cancellationToken) =>
        MarkAsync(transactionId, correlationId, "Failed", errorMessage, cancellationToken);

    private async Task MarkAsync(string transactionId, Guid correlationId, string status,
        string? errorMessage, CancellationToken cancellationToken)
    {
        if (!long.TryParse(transactionId, out var id))
            throw new ArgumentException("El identificador de transacción SQL no es válido.", nameof(transactionId));

        const string sql = """
            UPDATE dbo.MasterTransactions
               SET Status=@Status, CorrelationId=@CorrelationId, LastError=@LastError,
                   CompletedAtUtc=CASE WHEN @Status='Completed' THEN SYSUTCDATETIME() ELSE NULL END,
                   LeaseUntilUtc=NULL, UpdatedAtUtc=SYSUTCDATETIME()
             WHERE Id=@Id AND Status='Processing';
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@CorrelationId", correlationId);
        command.Parameters.AddWithValue("@LastError", (object?)errorMessage ?? DBNull.Value);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != 1)
            throw new InvalidOperationException($"No se pudo actualizar la transacción maestra {id}.");
    }
}
