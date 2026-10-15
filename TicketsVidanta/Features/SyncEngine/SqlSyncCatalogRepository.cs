using Microsoft.Data.SqlClient;
using TicketsVidanta.Shared.Database;

namespace TicketsVidanta.Features.SyncEngine;

internal sealed class SqlSyncCatalogRepository(ISqlConnectionFactory connectionFactory) : ISyncCatalogRepository
{
    public async Task<IReadOnlyList<SyncTableCatalogEntry>> GetActiveEntriesAsync(CancellationToken cancellationToken) =>
        await QueryEntriesAsync("SELECT CatalogId,SourceSystem,Resort,ConnectionName,RemoteTable,LocalTable,DateColumn,KeyColumn,InitialExtractDate,LastExtractedDate,LastExtractedKey,BatchSize,OverlapMinutes,IntervalSeconds,IsEnabled,CreatedAtUtc,UpdatedAtUtc FROM etl.SyncTableCatalog WHERE IsEnabled=1 ORDER BY CatalogId", cancellationToken);

    public async Task<IReadOnlyList<SyncTableCatalogEntry>> GetAllEntriesAsync(CancellationToken cancellationToken) =>
        await QueryEntriesAsync("SELECT CatalogId,SourceSystem,Resort,ConnectionName,RemoteTable,LocalTable,DateColumn,KeyColumn,InitialExtractDate,LastExtractedDate,LastExtractedKey,BatchSize,OverlapMinutes,IntervalSeconds,IsEnabled,CreatedAtUtc,UpdatedAtUtc FROM etl.SyncTableCatalog ORDER BY SourceSystem,Resort,RemoteTable", cancellationToken);

    public async Task<SyncTableCatalogEntry?> GetEntryByIdAsync(int catalogId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CatalogId,SourceSystem,Resort,ConnectionName,RemoteTable,LocalTable,DateColumn,KeyColumn,InitialExtractDate,LastExtractedDate,LastExtractedKey,BatchSize,OverlapMinutes,IntervalSeconds,IsEnabled,CreatedAtUtc,UpdatedAtUtc FROM etl.SyncTableCatalog WHERE CatalogId=@Id";
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", catalogId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadEntry(reader) : null;
    }

    public async Task<int> SaveEntryAsync(SaveSyncCatalogRequest request, CancellationToken cancellationToken)
    {
        var initDate = request.InitialExtractDate ?? new DateOnly(2026, 7, 1);
        if (request.CatalogId is { } existingId and > 0)
        {
            const string updateSql = """
                UPDATE etl.SyncTableCatalog
                   SET SourceSystem = @SourceSystem,
                       Resort = @Resort,
                       ConnectionName = @ConnectionName,
                       RemoteTable = @RemoteTable,
                       LocalTable = @LocalTable,
                       DateColumn = @DateColumn,
                       KeyColumn = @KeyColumn,
                       InitialExtractDate = @InitDate,
                       BatchSize = @BatchSize,
                       OverlapMinutes = @OverlapMinutes,
                       IntervalSeconds = @IntervalSeconds,
                       IsEnabled = @IsEnabled,
                       UpdatedAtUtc = SYSUTCDATETIME()
                 WHERE CatalogId = @Id;
                """;
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(updateSql, connection);
            command.Parameters.AddWithValue("@Id", existingId);
            AddEntryParameters(command, request, initDate);
            await command.ExecuteNonQueryAsync(cancellationToken);
            //Validacion general de la informacion 
            return existingId;
        }

        const string insertSql = """
            INSERT etl.SyncTableCatalog
                (SourceSystem, Resort, ConnectionName, RemoteTable, LocalTable, DateColumn, KeyColumn,
                 InitialExtractDate, LastExtractedDate, LastExtractedKey, BatchSize, OverlapMinutes, IntervalSeconds, IsEnabled)
            OUTPUT inserted.CatalogId
            VALUES
                (@SourceSystem, @Resort, @ConnectionName, @RemoteTable, @LocalTable, @DateColumn, @KeyColumn,
                 @InitDate, CAST(@InitDate AS datetime2(3)), '0', @BatchSize, @OverlapMinutes, @IntervalSeconds, @IsEnabled);
            """;
        await using var insertConn = connectionFactory.CreateConnection();
        await insertConn.OpenAsync(cancellationToken);
        await using var insertCmd = new SqlCommand(insertSql, insertConn);
        AddEntryParameters(insertCmd, request, initDate);
        return Convert.ToInt32(await insertCmd.ExecuteScalarAsync(cancellationToken));
    }

    public async Task DeleteEntryAsync(int catalogId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE etl.SyncTableCatalog WHERE CatalogId=@Id";
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", catalogId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateWatermarkAsync(int catalogId, DateTime lastDate, string lastKey, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE etl.SyncTableCatalog
               SET LastExtractedDate = @LastDate,
                   LastExtractedKey = @LastKey,
                   UpdatedAtUtc = SYSUTCDATETIME()
             WHERE CatalogId = @Id;
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", catalogId);
        command.Parameters.AddWithValue("@LastDate", lastDate);
        command.Parameters.AddWithValue("@LastKey", lastKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<long> StartBatchLogAsync(int catalogId, string sourceSystem, string resort, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT etl.SyncBatchLog (CatalogId, SourceSystem, Resort, TableName, StartedAtUtc, Status)
            OUTPUT inserted.BatchId
            VALUES (@CatalogId, @SourceSystem, @Resort, @TableName, SYSUTCDATETIME(), 'Running');
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CatalogId", catalogId);
        command.Parameters.AddWithValue("@SourceSystem", sourceSystem);
        command.Parameters.AddWithValue("@Resort", resort);
        command.Parameters.AddWithValue("@TableName", tableName);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task CompleteBatchLogAsync(long batchId, int rowsExtracted, int rowsUpserted, string status, string? error, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE etl.SyncBatchLog
               SET CompletedAtUtc = SYSUTCDATETIME(),
                   RowsExtracted = @RowsExtracted,
                   RowsUpserted = @RowsUpserted,
                   Status = @Status,
                   ErrorMessage = @Error
             WHERE BatchId = @BatchId;
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchId", batchId);
        command.Parameters.AddWithValue("@RowsExtracted", rowsExtracted);
        command.Parameters.AddWithValue("@RowsUpserted", rowsUpserted);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@Error", (object?)error ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SyncBatchLogEntry>> GetRecentBatchLogsAsync(int top, CancellationToken cancellationToken)
    {
        var sql = $"SELECT TOP ({Math.Clamp(top, 1, 500)}) BatchId,CatalogId,SourceSystem,Resort,TableName,StartedAtUtc,CompletedAtUtc,RowsExtracted,RowsUpserted,Status,ErrorMessage FROM etl.SyncBatchLog ORDER BY BatchId DESC";
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<SyncBatchLogEntry>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SyncBatchLogEntry(
                reader.GetInt64(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetDateTimeOffset(5),
                reader.IsDBNull(6) ? null : reader.GetDateTimeOffset(6),
                reader.GetInt32(7),
                reader.GetInt32(8),
                reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetString(10)));
        }
        return result;
    }

    private async Task<IReadOnlyList<SyncTableCatalogEntry>> QueryEntriesAsync(string sql, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<SyncTableCatalogEntry>();
        while (await reader.ReadAsync(cancellationToken))
            list.Add(ReadEntry(reader));
        return list;
    }

    private static SyncTableCatalogEntry ReadEntry(SqlDataReader reader) => new(
        reader.GetInt32(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.GetString(6),
        reader.GetString(7),
        DateOnly.FromDateTime(reader.GetDateTime(8)),
        reader.GetDateTime(9),
        reader.GetString(10),
        reader.GetInt32(11),
        reader.GetInt32(12),
        reader.GetInt32(13),
        reader.GetBoolean(14),
        reader.GetDateTimeOffset(15),
        reader.GetDateTimeOffset(16));

    private static void AddEntryParameters(SqlCommand command, SaveSyncCatalogRequest request, DateOnly initDate)
    {
        command.Parameters.AddWithValue("@SourceSystem", (request.SourceSystem ?? "").Trim());
        command.Parameters.AddWithValue("@Resort", (request.Resort ?? "").Trim());
        command.Parameters.AddWithValue("@ConnectionName", (request.ConnectionName ?? "").Trim());
        command.Parameters.AddWithValue("@RemoteTable", (request.RemoteTable ?? "").Trim());
        command.Parameters.AddWithValue("@LocalTable", (request.LocalTable ?? "").Trim());
        command.Parameters.AddWithValue("@DateColumn", (request.DateColumn ?? "").Trim());
        command.Parameters.AddWithValue("@KeyColumn", (request.KeyColumn ?? "").Trim());
        command.Parameters.AddWithValue("@InitDate", initDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@BatchSize", request.BatchSize <= 0 ? 5000 : request.BatchSize);
        command.Parameters.AddWithValue("@OverlapMinutes", Math.Max(0, request.OverlapMinutes));
        command.Parameters.AddWithValue("@IntervalSeconds", Math.Max(10, request.IntervalSeconds));
        command.Parameters.AddWithValue("@IsEnabled", request.IsEnabled);
    }
}

