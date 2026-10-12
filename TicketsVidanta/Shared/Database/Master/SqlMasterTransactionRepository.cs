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
        MasterTransactionSelection? selection,
        CancellationToken cancellationToken)
    {
        var filters = new List<string>();
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand { Connection = connection };

        var statusPredicate = """
            (Status IN ('Pending', 'Failed') OR
             (Status = 'Processing' AND LeaseUntilUtc < SYSUTCDATETIME()))
            """;
        if (selection is not null)
        {
            // A controlled run must never retry or recover an already claimed document.
            statusPredicate = "Status = 'Pending'";
            AddDocumentFilter(command, filters, selection.Documents);
            AddInFilter(command, filters, "SourceSystem", "SourceSystem", selection.SourceSystems);
            AddInFilter(command, filters, "Resort", "Resort", selection.Resorts);
            AddInFilter(command, filters, "TcGroup", "TcGroup", selection.TcGroups);
            AddInFilter(command, filters, "TrxCode", "TrxCode", selection.TrxCodes);
        }

        command.CommandText = $"""
            ;WITH Claimable AS
            (
                SELECT TOP (@BatchSize) *
                FROM dbo.MasterTransactions WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE {statusPredicate}
                    AND AttemptCount < @MaxAttempts
                    {string.Join(Environment.NewLine, filters)}
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
                   inserted.CheckNumber, inserted.Room, inserted.Reference, inserted.SourceSystem,
                   inserted.TcGroup, inserted.TrxCode;
            """;

        var result = new List<MasterTransaction>();
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        command.Parameters.AddWithValue("@MaxAttempts", processingOptions.Value.MaxAttempts);
        command.Parameters.AddWithValue("@LeaseSeconds", processingOptions.Value.ClaimLeaseSeconds);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new MasterTransaction(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5), reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8)));
        }

        return result;
    }

    private static void AddDocumentFilter(
        SqlCommand command,
        ICollection<string> filters,
        IReadOnlyList<MasterTransactionDocument> documents)
    {
        if (documents.Count == 0) return;

        var predicates = new List<string>();
        for (var index = 0; index < documents.Count; index++)
        {
            var document = documents[index];
            predicates.Add($"(Resort=@DocumentResort{index} AND ReservationId=@DocumentReservation{index} AND CheckNumber=@DocumentCheck{index})");
            command.Parameters.AddWithValue($"@DocumentResort{index}", document.Resort);
            command.Parameters.AddWithValue($"@DocumentReservation{index}", document.ReservationId);
            command.Parameters.AddWithValue($"@DocumentCheck{index}", document.CheckNumber);
        }
        filters.Add($"AND ({string.Join(" OR ", predicates)})");
    }

    private static void AddInFilter(
        SqlCommand command,
        ICollection<string> filters,
        string column,
        string parameterPrefix,
        IReadOnlyList<string> values)
    {
        if (values.Count == 0) return;

        var parameters = new List<string>();
        for (var index = 0; index < values.Count; index++)
        {
            var parameter = $"@{parameterPrefix}{index}";
            parameters.Add(parameter);
            command.Parameters.AddWithValue(parameter, values[index]);
        }
        filters.Add($"AND {column} IN ({string.Join(", ", parameters)})");
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
