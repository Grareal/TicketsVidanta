using Microsoft.Data.SqlClient;
using TicketsVidanta.Shared.Database;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.Processing;

public sealed class SqlProcessingRegistry(ISqlConnectionFactory connectionFactory) : IProcessingRegistry
{
    public async Task<bool> HasAlreadyBeenProcessedAsync(ProcessingKey key, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1) 1 FROM dbo.ProcessingRecords
            WHERE Resort=@Resort AND ReservationId=@ReservationId AND CheckNumber=@CheckNumber
              AND SourceSystem=@SourceSystem AND Status='Completed';
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateKeyCommand(connection, sql, key);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<bool> TryRegisterStartedAsync(ProcessingKey key, Guid correlationId, CancellationToken cancellationToken)
    {
        const string retrySql = """
            UPDATE dbo.ProcessingRecords
               SET CorrelationId=@CorrelationId, Status='Resolving', StartedAtUtc=SYSUTCDATETIME(),
                   CompletedAtUtc=NULL, ErrorMessage=NULL
             WHERE Resort=@Resort AND ReservationId=@ReservationId AND CheckNumber=@CheckNumber
               AND SourceSystem=@SourceSystem AND Status='Failed';
            """;
        const string sql = """
            INSERT dbo.ProcessingRecords
                (Resort, ReservationId, CheckNumber, SourceSystem, CorrelationId, Status, StartedAtUtc)
            VALUES
                (@Resort, @ReservationId, @CheckNumber, @SourceSystem, @CorrelationId, 'Resolving', SYSUTCDATETIME());
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using (var retryCommand = CreateKeyCommand(connection, retrySql, key))
        {
            retryCommand.Parameters.AddWithValue("@CorrelationId", correlationId);
            if (await retryCommand.ExecuteNonQueryAsync(cancellationToken) == 1)
                return true;
        }
        await using var command = CreateKeyCommand(connection, sql, key);
        command.Parameters.AddWithValue("@CorrelationId", correlationId);
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return false;
        }
    }

    public Task RegisterCompletedAsync(ProcessingKey key, Guid correlationId, CancellationToken cancellationToken) =>
        UpdateAsync(key, correlationId, ProcessingStatus.Completed, null, cancellationToken);

    public Task RegisterFailedAsync(ProcessingKey key, Guid correlationId, string errorMessage, CancellationToken cancellationToken) =>
        UpdateAsync(key, correlationId, ProcessingStatus.Failed, errorMessage, cancellationToken);

    public async Task<ProcessingRecord?> GetAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Resort, ReservationId, CheckNumber, SourceSystem, CorrelationId,
                   Status, StartedAtUtc, CompletedAtUtc, ErrorMessage
            FROM dbo.ProcessingRecords WHERE CorrelationId=@CorrelationId;
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CorrelationId", correlationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var key = new ProcessingKey(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
        return new ProcessingRecord(
            key, reader.GetGuid(4), Enum.Parse<ProcessingStatus>(reader.GetString(5), true),
            reader.GetDateTimeOffset(6), reader.IsDBNull(7) ? null : reader.GetDateTimeOffset(7),
            reader.IsDBNull(8) ? null : reader.GetString(8));
    }

    private async Task UpdateAsync(ProcessingKey key, Guid correlationId, ProcessingStatus status,
        string? errorMessage, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.ProcessingRecords
               SET Status=@Status, CompletedAtUtc=SYSUTCDATETIME(), ErrorMessage=@ErrorMessage
             WHERE Resort=@Resort AND ReservationId=@ReservationId AND CheckNumber=@CheckNumber
               AND SourceSystem=@SourceSystem AND CorrelationId=@CorrelationId;
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateKeyCommand(connection, sql, key);
        command.Parameters.AddWithValue("@CorrelationId", correlationId);
        command.Parameters.AddWithValue("@Status", status.ToString());
        command.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static SqlCommand CreateKeyCommand(SqlConnection connection, string sql, ProcessingKey key)
    {
        var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Resort", key.Resort);
        command.Parameters.AddWithValue("@ReservationId", key.ReservationId);
        command.Parameters.AddWithValue("@CheckNumber", key.CheckNumber);
        command.Parameters.AddWithValue("@SourceSystem", key.SourceSystem);
        return command;
    }
}
