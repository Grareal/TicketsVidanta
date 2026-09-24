using Microsoft.Data.SqlClient;

namespace TicketsVidanta.Features.DatabaseConfiguration;

internal sealed class DatabaseMetadataService(
    IConfigurationRepository repository,
    ConnectionSecretProtector protector)
{
    public async Task<string> TestAsync(Guid? id, string? rawConnectionString, CancellationToken cancellationToken)
    {
        var value = await ResolveAsync(id, rawConnectionString, cancellationToken);
        var builder = new SqlConnectionStringBuilder(value) { ConnectTimeout = Math.Min(new SqlConnectionStringBuilder(value).ConnectTimeout, 10) };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return $"Conexión correcta a {connection.DataSource} / {connection.Database}.";
    }

    public async Task<IReadOnlyList<DatabaseTableInfo>> GetSchemaAsync(Guid id, CancellationToken cancellationToken)
    {
        var value = await ResolveAsync(id, null, cancellationToken);
        const string sql = """
            SELECT s.name,t.name,c.name,ty.name,c.is_nullable
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id=t.schema_id
            INNER JOIN sys.columns c ON c.object_id=t.object_id
            INNER JOIN sys.types ty ON ty.user_type_id=c.user_type_id
            WHERE t.is_ms_shipped=0
            ORDER BY s.name,t.name,c.column_id;
            """;
        await using var connection = new SqlConnection(value);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 20 };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var groups = new Dictionary<(string Schema, string Table), List<DatabaseColumnInfo>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var key = (reader.GetString(0), reader.GetString(1));
            if (!groups.TryGetValue(key, out var columns)) groups[key] = columns = [];
            columns.Add(new(reader.GetString(2), reader.GetString(3), reader.GetBoolean(4)));
        }
        return groups.Select(x => new DatabaseTableInfo(x.Key.Schema, x.Key.Table, x.Value)).ToArray();
    }

    private async Task<string> ResolveAsync(Guid? id, string? raw, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(raw)) return raw.Trim();
        if (id is null) throw new ArgumentException("Indica una conexión guardada o una cadena para probar.");
        var stored = await repository.GetConnectionAsync(id.Value, cancellationToken)
            ?? throw new KeyNotFoundException("La conexión no existe.");
        return protector.Unprotect(stored.ProtectedConnectionString);
    }
}
