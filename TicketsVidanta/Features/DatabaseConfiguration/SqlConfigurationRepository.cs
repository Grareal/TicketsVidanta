using System.Text.Json;
using Microsoft.Data.SqlClient;
using TicketsVidanta.Shared.Database;
using TicketsVidanta.Shared.TicketGeneration;

namespace TicketsVidanta.Features.DatabaseConfiguration;

internal sealed class SqlConfigurationRepository(
    ISqlConnectionFactory connectionFactory,
    ConnectionSecretProtector protector) : IConfigurationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<DatabaseConnectionSummary>> GetConnectionsAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT Id,Name,Provider,ServerName,DatabaseName,IsEnabled,UpdatedAtUtc FROM dbo.ConfigDatabaseConnections ORDER BY Name";
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<DatabaseConnectionSummary>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), GetString(reader, 3), GetString(reader, 4), reader.GetBoolean(5), reader.GetDateTimeOffset(6)));
        return result;
    }

    public async Task<StoredDatabaseConnection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT Id,Name,Provider,ProtectedConnectionString,IsEnabled,UpdatedAtUtc FROM dbo.ConfigDatabaseConnections WHERE Id=@Id";
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetBoolean(4), reader.GetDateTimeOffset(5))
            : null;
    }

    public async Task<Guid> SaveConnectionAsync(SaveDatabaseConnectionRequest request, CancellationToken cancellationToken)
    {
        var name = Required(request.Name, "nombre");
        var id = request.Id ?? Guid.NewGuid();
        var existing = request.Id is null ? null : await GetConnectionAsync(id, cancellationToken);
        var protectedValue = string.IsNullOrWhiteSpace(request.ConnectionString)
            ? existing?.ProtectedConnectionString ?? throw new ArgumentException("La cadena de conexión es obligatoria al crear una conexión.")
            : protector.Protect(request.ConnectionString.Trim());
        var connectionBuilder = new SqlConnectionStringBuilder(
            string.IsNullOrWhiteSpace(request.ConnectionString)
                ? protector.Unprotect(protectedValue)
                : request.ConnectionString.Trim());
        const string sql = """
            UPDATE dbo.ConfigDatabaseConnections SET Name=@Name,Provider='SqlServer',ProtectedConnectionString=@Secret,
                ServerName=@ServerName,DatabaseName=@DatabaseName,IsEnabled=@Enabled,UpdatedAtUtc=SYSUTCDATETIME() WHERE Id=@Id;
            IF @@ROWCOUNT=0 INSERT dbo.ConfigDatabaseConnections(Id,Name,Provider,ServerName,DatabaseName,ProtectedConnectionString,IsEnabled)
                VALUES(@Id,@Name,'SqlServer',@ServerName,@DatabaseName,@Secret,@Enabled);
            """;
        await ExecuteAsync(sql, cancellationToken,
            new("@Id", id), new("@Name", name), new("@ServerName", Db(connectionBuilder.DataSource)),
            new("@DatabaseName", Db(connectionBuilder.InitialCatalog)), new("@Secret", protectedValue), new("@Enabled", request.IsEnabled));
        return id;
    }

    public Task DeleteConnectionAsync(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync("DELETE dbo.ConfigDatabaseConnections WHERE Id=@Id", cancellationToken, new SqlParameter("@Id", id));

    public async Task<IReadOnlyList<TicketProfile>> GetProfilesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id,Name,SourceSystem,Resort,ConnectionId,IsEnabled,BaseSchema,BaseTable,DetailSchema,DetailTable,
                   BaseJoinColumn,DetailJoinColumn,ReservationColumn,CheckNumberColumn,ResortColumn,FieldMappingsJson,
                   CustomQuerySql,CurrencyConstant,MaxRows,UpdatedAtUtc
            FROM dbo.ConfigTicketProfiles ORDER BY Name;
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<TicketProfile>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(ReadProfile(reader));
        return result;
    }

    public async Task<Guid> SaveProfileAsync(SaveTicketProfileRequest request, CancellationToken cancellationToken)
    {
        ValidateProfile(request);
        var id = request.Id ?? Guid.NewGuid();
        var mappings = (request.FieldMappings ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(x => x.Key.Trim(), x => x.Value.Trim(), StringComparer.OrdinalIgnoreCase);
        var customQuery = string.IsNullOrWhiteSpace(request.CustomQuerySql)
            ? null
            : ConfiguredQueryValidator.ValidateAndNormalize(request.CustomQuerySql);
        const string sql = """
            UPDATE dbo.ConfigTicketProfiles SET Name=@Name,SourceSystem=@SourceSystem,Resort=@Resort,ConnectionId=@ConnectionId,
              IsEnabled=@Enabled,BaseSchema=@BaseSchema,BaseTable=@BaseTable,DetailSchema=@DetailSchema,DetailTable=@DetailTable,
              BaseJoinColumn=@BaseJoinColumn,DetailJoinColumn=@DetailJoinColumn,ReservationColumn=@ReservationColumn,
              CheckNumberColumn=@CheckNumberColumn,ResortColumn=@ResortColumn,FieldMappingsJson=@Mappings,CustomQuerySql=@CustomQuerySql,
              CurrencyConstant=@Currency,MaxRows=@MaxRows,UpdatedAtUtc=SYSUTCDATETIME() WHERE Id=@Id;
            IF @@ROWCOUNT=0 INSERT dbo.ConfigTicketProfiles
              (Id,Name,SourceSystem,Resort,ConnectionId,IsEnabled,BaseSchema,BaseTable,DetailSchema,DetailTable,
               BaseJoinColumn,DetailJoinColumn,ReservationColumn,CheckNumberColumn,ResortColumn,FieldMappingsJson,CustomQuerySql,CurrencyConstant,MaxRows)
              VALUES(@Id,@Name,@SourceSystem,@Resort,@ConnectionId,@Enabled,@BaseSchema,@BaseTable,@DetailSchema,@DetailTable,
               @BaseJoinColumn,@DetailJoinColumn,@ReservationColumn,@CheckNumberColumn,@ResortColumn,@Mappings,@CustomQuerySql,@Currency,@MaxRows);
            """;
        await ExecuteAsync(sql, cancellationToken,
            new("@Id", id), new("@Name", request.Name!.Trim()), new("@SourceSystem", request.SourceSystem!.Trim()),
            new("@Resort", Db(request.Resort)), new("@ConnectionId", request.ConnectionId), new("@Enabled", request.IsEnabled),
            new("@BaseSchema", Db(request.BaseSchema)), new("@BaseTable", Db(request.BaseTable)),
            new("@DetailSchema", Db(request.DetailSchema)), new("@DetailTable", Db(request.DetailTable)),
            new("@BaseJoinColumn", Db(request.BaseJoinColumn)), new("@DetailJoinColumn", Db(request.DetailJoinColumn)),
            new("@ReservationColumn", Db(request.ReservationColumn)), new("@CheckNumberColumn", Db(request.CheckNumberColumn)),
            new("@ResortColumn", Db(request.ResortColumn)), new("@Mappings", JsonSerializer.Serialize(mappings, JsonOptions)),
            new("@CustomQuerySql", Db(customQuery)),
            new("@Currency", Db(request.CurrencyConstant)), new("@MaxRows", request.MaxRows));
        return id;
    }

    public Task DeleteProfileAsync(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync("DELETE dbo.ConfigTicketProfiles WHERE Id=@Id", cancellationToken, new SqlParameter("@Id", id));

    public async Task<IReadOnlyList<TransactionRoute>> GetRoutesAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT Id,TcGroup,TrxCode,SourceSystem,IsEnabled,UpdatedAtUtc FROM dbo.ConfigTransactionRoutes ORDER BY TcGroup,TrxCode";
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<TransactionRoute>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetBoolean(4), reader.GetDateTimeOffset(5)));
        return result;
    }

    public async Task<Guid> SaveRouteAsync(SaveTransactionRouteRequest request, CancellationToken cancellationToken)
    {
        var id = request.Id ?? Guid.NewGuid();
        const string sql = """
            UPDATE dbo.ConfigTransactionRoutes SET TcGroup=@Group,TrxCode=@Code,SourceSystem=@Source,IsEnabled=@Enabled,
              UpdatedAtUtc=SYSUTCDATETIME() WHERE Id=@Id;
            IF @@ROWCOUNT=0 INSERT dbo.ConfigTransactionRoutes(Id,TcGroup,TrxCode,SourceSystem,IsEnabled)
              VALUES(@Id,@Group,@Code,@Source,@Enabled);
            """;
        await ExecuteAsync(sql, cancellationToken, new("@Id", id), new("@Group", Required(request.TcGroup, "TC Group")),
            new("@Code", Required(request.TrxCode, "TRX Code")), new("@Source", Required(request.SourceSystem, "sistema origen")),
            new("@Enabled", request.IsEnabled));
        return id;
    }

    public Task DeleteRouteAsync(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync("DELETE dbo.ConfigTransactionRoutes WHERE Id=@Id", cancellationToken, new SqlParameter("@Id", id));

    public async Task<IReadOnlyList<TicketTemplate>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id,Name,SourceSystem,Resort,PointOfSale,IsEnabled,LayoutJson,UpdatedAtUtc
            FROM dbo.ConfigTicketTemplates ORDER BY Name;
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<TicketTemplate>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var layout = JsonSerializer.Deserialize<TicketLayout>(reader.GetString(6), JsonOptions)
                ?? throw new InvalidOperationException($"La plantilla {reader.GetString(1)} no contiene un layout valido.");
            result.Add(new TicketTemplate(
                reader.GetGuid(0), reader.GetString(1), GetString(reader, 2), GetString(reader, 3), GetString(reader, 4),
                reader.GetBoolean(5), layout, reader.GetDateTimeOffset(7)));
        }
        return result;
    }

    public async Task<Guid> SaveTemplateAsync(SaveTicketTemplateRequest request, CancellationToken cancellationToken)
    {
        TicketTemplateValidator.Validate(request);
        var id = request.Id ?? Guid.NewGuid();
        var matchKey = TicketTemplateValidator.MatchKey(request.SourceSystem, request.Resort, request.PointOfSale);
        const string sql = """
            UPDATE dbo.ConfigTicketTemplates
               SET Name=@Name,SourceSystem=@SourceSystem,Resort=@Resort,PointOfSale=@PointOfSale,
                   MatchKey=@MatchKey,IsEnabled=@Enabled,LayoutJson=@Layout,UpdatedAtUtc=SYSUTCDATETIME()
             WHERE Id=@Id;
            IF @@ROWCOUNT=0
                INSERT dbo.ConfigTicketTemplates
                    (Id,Name,SourceSystem,Resort,PointOfSale,MatchKey,IsEnabled,LayoutJson)
                VALUES
                    (@Id,@Name,@SourceSystem,@Resort,@PointOfSale,@MatchKey,@Enabled,@Layout);
            """;
        await ExecuteAsync(sql, cancellationToken,
            new("@Id", id), new("@Name", request.Name!.Trim()), new("@SourceSystem", Db(request.SourceSystem)),
            new("@Resort", Db(request.Resort)), new("@PointOfSale", Db(request.PointOfSale)), new("@MatchKey", matchKey),
            new("@Enabled", request.IsEnabled), new("@Layout", JsonSerializer.Serialize(request.Layout, JsonOptions)));
        return id;
    }

    public Task DeleteTemplateAsync(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync("DELETE dbo.ConfigTicketTemplates WHERE Id=@Id", cancellationToken, new SqlParameter("@Id", id));

     public async Task<RuntimeConfigurationSnapshot> GetRuntimeSnapshotAsync(CancellationToken cancellationToken)
{
    var routes = await GetRoutesAsync(cancellationToken);
    var profiles = await GetProfilesAsync(cancellationToken);
    var templates = await GetTemplatesAsync(cancellationToken);

    var runtime = new List<RuntimeTicketProfile>();

    foreach (var profile in profiles.Where(x => x.IsEnabled))
    {
        var connection = await GetConnectionAsync(profile.ConnectionId, cancellationToken);

        if (connection is { IsEnabled: true })
        {
            var connectionString =
                protector.Unprotect(connection.ProtectedConnectionString);

            runtime.Add(
                new RuntimeTicketProfile(
                    profile,
                    connectionString));
        }
    }

    return new(
        routes.Where(x => x.IsEnabled).ToArray(),
        runtime,
        templates.Where(x => x.IsEnabled).ToArray());
}

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken, params SqlParameter[] parameters)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static TicketProfile ReadProfile(SqlDataReader r) => new(
        r.GetGuid(0), r.GetString(1), r.GetString(2), GetString(r, 3), r.GetGuid(4), r.GetBoolean(5), GetString(r, 6), GetString(r, 7),
        GetString(r, 8), GetString(r, 9), GetString(r, 10), GetString(r, 11), GetString(r, 12), GetString(r, 13), GetString(r, 14),
        JsonSerializer.Deserialize<Dictionary<string, string>>(r.GetString(15), JsonOptions) ?? new(), GetString(r, 16), GetString(r, 17), r.GetInt32(18), r.GetDateTimeOffset(19));

    private static void ValidateProfile(SaveTicketProfileRequest request)
    {
        Required(request.Name, "nombre");
        var source = Required(request.SourceSystem, "sistema origen");
        if (new[] { "MOCK", "LOCALSQL", "INSSIST_SPA", "INSSIST_KIDSCLUB" }.Contains(source, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Ese sistema origen está reservado por un resolver integrado.");
        if (request.ConnectionId == Guid.Empty) throw new ArgumentException("Selecciona una conexión.");
        if (request.MaxRows is < 1 or > 5000) throw new ArgumentException("MaxRows debe estar entre 1 y 5000.");
        if (!string.IsNullOrWhiteSpace(request.CustomQuerySql))
        {
            ConfiguredQueryValidator.ValidateAndNormalize(request.CustomQuerySql);
        }
        else
        {
            Required(request.BaseSchema, "esquema base"); Required(request.BaseTable, "tabla base");
            Required(request.ReservationColumn, "columna de reservación"); Required(request.CheckNumberColumn, "columna de cheque");
            if (request.FieldMappings is null || request.FieldMappings.Count == 0)
                throw new ArgumentException("Configura al menos un campo de salida.");
        }
        var detailParts = new[] { request.DetailSchema, request.DetailTable, request.BaseJoinColumn, request.DetailJoinColumn };
        if (detailParts.Any(x => !string.IsNullOrWhiteSpace(x)) && detailParts.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Para usar detalle indica esquema, tabla y ambas columnas de unión.");
    }

    private static string Required(string? value, string field) =>
        !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new ArgumentException($"El campo {field} es obligatorio.");
    private static string? GetString(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static object Db(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
}
