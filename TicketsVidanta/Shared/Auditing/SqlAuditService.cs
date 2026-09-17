using Microsoft.Data.SqlClient;
using TicketsVidanta.Shared.Database;

namespace TicketsVidanta.Shared.Auditing;

public sealed class SqlAuditService(ISqlConnectionFactory connectionFactory) : IAuditService
{
    public async Task WriteAsync(TicketProcessingAudit audit, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT dbo.TicketProcessingAudit
                (Id, CorrelationId, Resort, ReservationId, CheckNumber, SourceSystem, Status,
                 AttemptCount, StartedAtUtc, CompletedAtUtc, OperaDocumentId, FileName, ErrorMessage)
            VALUES
                (@Id, @CorrelationId, @Resort, @ReservationId, @CheckNumber, @SourceSystem, @Status,
                 @AttemptCount, @StartedAtUtc, @CompletedAtUtc, @OperaDocumentId, @FileName, @ErrorMessage);
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", audit.Id);
        command.Parameters.AddWithValue("@CorrelationId", audit.CorrelationId);
        command.Parameters.AddWithValue("@Resort", audit.Resort);
        command.Parameters.AddWithValue("@ReservationId", audit.ReservationId);
        command.Parameters.AddWithValue("@CheckNumber", audit.CheckNumber);
        command.Parameters.AddWithValue("@SourceSystem", audit.SourceSystem);
        command.Parameters.AddWithValue("@Status", audit.Status.ToString());
        command.Parameters.AddWithValue("@AttemptCount", audit.AttemptCount);
        command.Parameters.AddWithValue("@StartedAtUtc", audit.StartedAt);
        command.Parameters.AddWithValue("@CompletedAtUtc", (object?)audit.CompletedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("@OperaDocumentId", (object?)audit.OperaDocumentId ?? DBNull.Value);
        command.Parameters.AddWithValue("@FileName", (object?)audit.FileName ?? DBNull.Value);
        command.Parameters.AddWithValue("@ErrorMessage", (object?)audit.ErrorMessage ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
