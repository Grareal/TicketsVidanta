using System.Data;
using Microsoft.Data.SqlClient;
using TicketsVidanta.Features.DatabaseConfiguration;
using TicketsVidanta.Shared.Database;

namespace TicketsVidanta.Features.SyncEngine;

internal sealed class TableSyncService(
    ISyncCatalogRepository syncCatalog,
    ISqlConnectionFactory localConnectionFactory,
    IConfiguration configuration,
    IConfigurationRepository configRepository,
    ConnectionSecretProtector protector,
    ILogger<TableSyncService> logger) : ITableSyncService
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> RemoteColumnTypeCache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<SyncBatchResult> SyncTableAsync(SyncTableCatalogEntry entry, CancellationToken cancellationToken)
    {
        var batchId = await syncCatalog.StartBatchLogAsync(entry.CatalogId, entry.SourceSystem, entry.Resort, entry.RemoteTable, cancellationToken);
        var sourceConnectionString = await ResolveConnectionStringAsync(entry.ConnectionName, cancellationToken);

        if (string.IsNullOrWhiteSpace(sourceConnectionString))
        {
            var msg = $"No se encontró la cadena de conexión '{entry.ConnectionName}' para {entry.SourceSystem}:{entry.Resort}.";
            await TryCompleteBatchLogAsync(batchId, 0, 0, "Failed", msg, cancellationToken);
            return new SyncBatchResult(entry.CatalogId, entry.SourceSystem, entry.Resort, entry.RemoteTable, 0, 0, entry.LastExtractedDate, entry.LastExtractedKey, false, msg);
        }

        var initDate = entry.InitialExtractDate.ToDateTime(TimeOnly.MinValue);
        var fromDate = entry.LastExtractedDate.AddMinutes(-entry.OverlapMinutes);
        if (fromDate < initDate) fromDate = initDate;

        var isDateBased = !string.IsNullOrWhiteSpace(entry.DateColumn) &&
                          !string.Equals(entry.DateColumn, "NONE", StringComparison.OrdinalIgnoreCase) &&
                          !string.Equals(entry.DateColumn, entry.KeyColumn, StringComparison.OrdinalIgnoreCase);

        var remoteQuery = isDateBased ? $"""
            SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
            SET LOCK_TIMEOUT 3000;
            SELECT TOP (@BatchSize) *
            FROM {entry.RemoteTable}
            WHERE {entry.DateColumn} >= @InitDate
              AND ({entry.DateColumn} > @FromDate OR ({entry.DateColumn} = @FromDate AND {entry.KeyColumn} > @LastKey))
            ORDER BY {entry.DateColumn} ASC, {entry.KeyColumn} ASC;
            """
            : $"""
            SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
            SET LOCK_TIMEOUT 3000;
            SELECT TOP (@BatchSize) *
            FROM {entry.RemoteTable}
            WHERE {entry.KeyColumn} > @LastKey
            ORDER BY {entry.KeyColumn} ASC;
            """;

        try
        {
            await using var sourceConnection = new SqlConnection(sourceConnectionString);
            await sourceConnection.OpenAsync(cancellationToken);

            await using var remoteCommand = new SqlCommand(remoteQuery, sourceConnection) { CommandTimeout = 45 };
            remoteCommand.Parameters.AddWithValue("@BatchSize", entry.BatchSize);
            remoteCommand.Parameters.AddWithValue("@LastKey", entry.LastExtractedKey);
            if (isDateBased)
            {
                var dateColName = entry.DateColumn.Contains('.') ? entry.DateColumn.Split('.').Last() : entry.DateColumn;
                var colType = await GetRemoteColumnTypeAsync(sourceConnection, entry.RemoteTable, dateColName, cancellationToken);

                if (colType is "int" or "bigint" or "smallint" or "tinyint")
                {
                    var initInt = initDate.Year * 10000 + initDate.Month * 100 + initDate.Day;
                    var fromInt = fromDate.Year * 10000 + fromDate.Month * 100 + fromDate.Day;
                    remoteCommand.Parameters.Add(new SqlParameter("@InitDate", SqlDbType.Int) { Value = initInt });
                    remoteCommand.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.Int) { Value = fromInt });
                }
                else if (colType is "varchar" or "char" or "nvarchar" or "nchar" or "text")
                {
                    var initStr = initDate.ToString("yyyyMMdd");
                    var fromStr = fromDate.ToString("yyyyMMdd");
                    remoteCommand.Parameters.Add(new SqlParameter("@InitDate", SqlDbType.VarChar, 20) { Value = initStr });
                    remoteCommand.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.VarChar, 20) { Value = fromStr });
                }
                else
                {
                    // Fallback estándar para date, datetime, datetime2, smalldatetime o tipo no resuelto
                    remoteCommand.Parameters.Add(new SqlParameter("@InitDate", SqlDbType.DateTime) { Value = initDate });
                    remoteCommand.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate });
                }
            }

            var dataTable = new DataTable();
            await using (var reader = await remoteCommand.ExecuteReaderAsync(cancellationToken))
            {
                dataTable.Load(reader);
            }

            var rowsExtracted = dataTable.Rows.Count;
            if (rowsExtracted == 0)
            {
                await TryCompleteBatchLogAsync(batchId, 0, 0, "Success", null, cancellationToken);
                return new SyncBatchResult(entry.CatalogId, entry.SourceSystem, entry.Resort, entry.RemoteTable, 0, 0, entry.LastExtractedDate, entry.LastExtractedKey, true, null);
            }

            // Asegurar columna Resort en el DataTable local
            if (!dataTable.Columns.Contains("Resort"))
            {
                var resortCol = dataTable.Columns.Add("Resort", typeof(string));
                foreach (DataRow row in dataTable.Rows)
                    row[resortCol] = entry.Resort;
            }

            // Identificar nuevos watermarks de manera flexible y resiliente (sin Convert.ToDateTime directo)
            var lastRow = dataTable.Rows[^1];
            var nextWatermarkDate = entry.LastExtractedDate;
            if (isDateBased)
            {
                var dateColName = entry.DateColumn.Contains('.') ? entry.DateColumn.Split('.').Last() : entry.DateColumn;
                nextWatermarkDate = FlexibleDateParser.Parse(lastRow[dateColName], entry.LastExtractedDate);
            }

            var keyColName = entry.KeyColumn.Contains('.') ? entry.KeyColumn.Split('.').Last() : entry.KeyColumn;
            var nextWatermarkKey = Convert.ToString(lastRow[keyColName]) ?? entry.LastExtractedKey;

            // Upsert a la base local TicketsVidanta
            var rowsUpserted = await UpsertToLocalStagingAsync(entry.LocalTable, entry.Resort, dataTable, cancellationToken);

            await syncCatalog.UpdateWatermarkAsync(entry.CatalogId, nextWatermarkDate, nextWatermarkKey, cancellationToken);
            await TryCompleteBatchLogAsync(batchId, rowsExtracted, rowsUpserted, "Success", null, cancellationToken);

            logger.LogInformation("Sincronización exitosa {RemoteTable} -> {LocalTable}. Extraídos={Extracted}, Upserted={Upserted}",
                entry.RemoteTable, entry.LocalTable, rowsExtracted, rowsUpserted);

            return new SyncBatchResult(entry.CatalogId, entry.SourceSystem, entry.Resort, entry.RemoteTable, rowsExtracted, rowsUpserted, nextWatermarkDate, nextWatermarkKey, true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sincronizando {RemoteTable} de {Resort}", entry.RemoteTable, entry.Resort);
            await TryCompleteBatchLogAsync(batchId, 0, 0, "Failed", ex.Message, cancellationToken);
            return new SyncBatchResult(entry.CatalogId, entry.SourceSystem, entry.Resort, entry.RemoteTable, 0, 0, entry.LastExtractedDate, entry.LastExtractedKey, false, ex.Message);
        }
    }

    private async Task TryCompleteBatchLogAsync(long batchId, int extracted, int upserted, string status, string? error, CancellationToken ct)
    {
        try
        {
            await syncCatalog.CompleteBatchLogAsync(batchId, extracted, upserted, status, error, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo actualizar el log de sincronización para BatchId={BatchId}", batchId);
        }
    }

    private async Task<string> GetRemoteColumnTypeAsync(SqlConnection sourceConnection, string remoteTable, string columnName, CancellationToken ct)
    {
        var cacheKey = $"{sourceConnection.Database}:{remoteTable}:{columnName}";
        if (RemoteColumnTypeCache.TryGetValue(cacheKey, out var cachedType))
            return cachedType;

        var parts = remoteTable.Split('.');
        var schema = parts.Length == 2 ? parts[0] : null;
        var table = parts.Length == 2 ? parts[1] : parts[0];

        const string sql = """
            SELECT LOWER(DATA_TYPE)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @Table
              AND COLUMN_NAME = @Col
              AND (@Schema IS NULL OR TABLE_SCHEMA = @Schema);
            """;

        try
        {
            await using var cmd = new SqlCommand(sql, sourceConnection) { CommandTimeout = 10 };
            cmd.Parameters.AddWithValue("@Table", table);
            cmd.Parameters.AddWithValue("@Col", columnName);
            cmd.Parameters.AddWithValue("@Schema", (object?)schema ?? DBNull.Value);
            var result = await cmd.ExecuteScalarAsync(ct);
            if (result is string dt && !string.IsNullOrWhiteSpace(dt))
            {
                var lowerDt = dt.ToLowerInvariant();
                RemoteColumnTypeCache[cacheKey] = lowerDt;
                return lowerDt;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo consultar INFORMATION_SCHEMA para {RemoteTable}.{ColumnName}. Se usará fallback.", remoteTable, columnName);
        }

        return "unknown";
    }

    private async Task<int> UpsertToLocalStagingAsync(string localTable, string resort, DataTable dataTable, CancellationToken cancellationToken)
    {
        await using var localConn = localConnectionFactory.CreateConnection();
        await localConn.OpenAsync(cancellationToken);

        var tempTableName = $"#tmp_sync_{Guid.NewGuid():N}";
        var columnDefinitions = new List<string>();

        foreach (DataColumn col in dataTable.Columns)
        {
            var sqlDataType = MapToSqlType(col.DataType);
            columnDefinitions.Add($"[{col.ColumnName}] {sqlDataType}");
        }

        var createTempSql = $"CREATE TABLE {tempTableName} ({string.Join(", ", columnDefinitions)});";
        await using (var createCmd = new SqlCommand(createTempSql, localConn))
        {
            await createCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        using (var bulkCopy = new SqlBulkCopy(localConn))
        {
            bulkCopy.DestinationTableName = tempTableName;
            bulkCopy.BulkCopyTimeout = 60;
            foreach (DataColumn col in dataTable.Columns)
                bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
        }

        // Obtener columnas comunes entre la tabla destino y la tabla temporal
        var commonColumns = await GetMatchingColumnsAsync(localConn, localTable, dataTable, cancellationToken);
        if (commonColumns.Count == 0)
            throw new InvalidOperationException($"No se encontraron columnas coincidentes entre {localTable} y los datos extraídos.");

        var colList = string.Join(", ", commonColumns.Select(c => $"[{c}]"));
        var mergeSql = BuildMergeSql(localTable, tempTableName, resort, commonColumns);

        await using var mergeCmd = new SqlCommand(mergeSql, localConn) { CommandTimeout = 60 };
        return await mergeCmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string BuildMergeSql(string localTable, string tempTable, string resort, List<string> columns)
    {
        var normTable = localTable.ToLowerInvariant();
        var insertCols = string.Join(", ", columns.Select(c => $"[{c}]"));
        var srcCols = string.Join(", ", columns.Select(c => $"src.[{c}]"));

        if (normTable.Contains("hotche"))
        {
            return $"""
                MERGE {localTable} AS tgt
                USING {tempTable} AS src
                   ON tgt.Resort = src.Resort
                  AND tgt.caja = src.caja
                  AND tgt.turno = src.turno
                  AND tgt.folio = src.folio
                WHEN MATCHED THEN
                    UPDATE SET tgt.total = src.total, tgt.tip = src.tip, tgt.sub_total = src.sub_total, tgt.ExtractDateUtc = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ({insertCols}, ExtractDateUtc)
                    VALUES ({srcCols}, SYSUTCDATETIME());
                """;
        }

        if (normTable.Contains("hotcom"))
        {
            return $"""
                INSERT INTO {localTable} ({insertCols}, ExtractDateUtc)
                SELECT {srcCols}, SYSUTCDATETIME()
                FROM {tempTable} src
                WHERE NOT EXISTS (
                    SELECT 1 FROM {localTable} tgt
                    WHERE tgt.Resort = src.Resort
                      AND tgt.caja = src.caja
                      AND tgt.turno = src.turno
                      AND tgt.folio = src.folio
                      AND tgt.platillo = src.platillo
                );
                """;
        }

        if (normTable.Contains("hotayb"))
        {
            return $"""
                MERGE {localTable} AS tgt
                USING {tempTable} AS src
                   ON tgt.Resort = src.Resort AND tgt.codigo = src.codigo
                WHEN MATCHED THEN
                    UPDATE SET tgt.descrip = src.descrip, tgt.ExtractDateUtc = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ({insertCols}, ExtractDateUtc)
                    VALUES ({srcCols}, SYSUTCDATETIME());
                """;
        }

        if (normTable.Contains("spares"))
        {
            return $"""
                MERGE {localTable} AS tgt
                USING {tempTable} AS src
                   ON tgt.Resort = src.Resort AND tgt.res_Clave = src.res_Clave
                WHEN MATCHED THEN
                    UPDATE SET tgt.cli_Nombre = src.cli_Nombre, tgt.cli_Apellido = src.cli_Apellido, tgt.ExtractDateUtc = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ({insertCols}, ExtractDateUtc)
                    VALUES ({srcCols}, SYSUTCDATETIME());
                """;
        }

        if (normTable.Contains("spapgo"))
        {
            return $"""
                INSERT INTO {localTable} ({insertCols}, ExtractDateUtc)
                SELECT {srcCols}, SYSUTCDATETIME()
                FROM {tempTable} src
                WHERE NOT EXISTS (
                    SELECT 1 FROM {localTable} tgt
                    WHERE tgt.Resort = src.Resort
                      AND tgt.pgo_Fecha = src.pgo_Fecha
                      AND tgt.pgo_Cheque = src.pgo_Cheque
                      AND tgt.pgo_CargoA = src.pgo_CargoA
                      AND ISNULL(tgt.pgo_Secuencia, '') = ISNULL(src.pgo_Secuencia, '')
                      AND ISNULL(tgt.pgo_PuntoVenta, '') = ISNULL(src.pgo_PuntoVenta, '')
                );
                """;
        }

        if (normTable.Contains("spasrp"))
        {
            return $"""
                INSERT INTO {localTable} ({insertCols}, ExtractDateUtc)
                SELECT {srcCols}, SYSUTCDATETIME()
                FROM {tempTable} src
                WHERE NOT EXISTS (
                    SELECT 1 FROM {localTable} tgt
                    WHERE tgt.Resort = src.Resort
                      AND tgt.srp_Cheque = src.srp_Cheque
                      AND tgt.srp_ClaveReservacion = src.srp_ClaveReservacion
                      AND tgt.srp_Nombre = src.srp_Nombre
                );
                """;
        }

        // Genérico fallback por defecto
        return $"""
            INSERT INTO {localTable} ({insertCols}, ExtractDateUtc)
            SELECT {srcCols}, SYSUTCDATETIME() FROM {tempTable} src;
            """;
    }

    private static async Task<List<string>> GetMatchingColumnsAsync(SqlConnection connection, string tableName, DataTable dataTable, CancellationToken ct)
    {
        var parts = tableName.Split('.');
        var schema = parts.Length == 2 ? parts[0] : "stg";
        var table = parts.Length == 2 ? parts[1] : parts[0];

        const string sql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @Table;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Schema", schema);
        command.Parameters.AddWithValue("@Table", table);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var dbColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(ct))
            dbColumns.Add(reader.GetString(0));

        var matching = new List<string>();
        foreach (DataColumn col in dataTable.Columns)
        {
            if (dbColumns.Contains(col.ColumnName))
                matching.Add(col.ColumnName);
        }
        return matching;
    }

    private static string MapToSqlType(Type type)
    {
        if (type == typeof(int) || type == typeof(short)) return "int NULL";
        if (type == typeof(long)) return "bigint NULL";
        if (type == typeof(decimal)) return "decimal(19,4) NULL";
        if (type == typeof(double) || type == typeof(float)) return "float NULL";
        if (type == typeof(bool)) return "bit NULL";
        if (type == typeof(DateTime)) return "datetime NULL";
        if (type == typeof(TimeSpan)) return "time(0) NULL";
        if (type == typeof(byte[])) return "varbinary(max) NULL";
        return "nvarchar(max) NULL";
    }

    private async Task<string?> ResolveConnectionStringAsync(string connectionName, CancellationToken cancellationToken)
    {
        var configStr = configuration.GetConnectionString(connectionName);
        if (!string.IsNullOrWhiteSpace(configStr)) return configStr.Trim();

        var connections = await configRepository.GetConnectionsAsync(cancellationToken);
        var match = connections.FirstOrDefault(c => string.Equals(c.Name, connectionName, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            var stored = await configRepository.GetConnectionAsync(match.Id, cancellationToken);
            if (stored is not null)
                return protector.Unprotect(stored.ProtectedConnectionString);
        }

        return null;
    }
}

