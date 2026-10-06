using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using TicketsVidanta.Features.DatabaseConfiguration;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.Resolvers;

internal sealed partial class ConfigurableSqlCheckResolver(
    IRuntimeConfigurationCache cache,
    ILogger<ConfigurableSqlCheckResolver> logger)
    : ICheckResolver{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "GuestName", "Room", "PointOfSale", "CheckNumber", "BusinessDate", "Time", "Subtotal", "Tip", "Tax",
        "Total", "Currency", "Header", "Footer", "Server", "Table", "GuestCount", "Turn", "CopyNumber",
        "ItemDescription", "ItemQuantity", "ItemAmount", "RawTicketText"
    };
    private readonly ILogger<ConfigurableSqlCheckResolver> _logger = logger;


        public bool CanHandle(CheckProcessingContext context)
    {
        var profile = cache.FindProfile(
            context.SourceSystem,
            context.Resort);

        _logger.LogWarning(
            "CanHandle SourceSystem={SourceSystem}, Resort={Resort}, ProfileFound={ProfileFound}",
            context.SourceSystem,
            context.Resort,
            profile != null);

        return profile is not null;
    }


    public async Task<CheckDetail?> ResolveAsync(CheckProcessingContext context, CancellationToken cancellationToken)
    {
        var runtime = cache.FindProfile(context.SourceSystem, context.Resort);
                logger.LogWarning(
                    "Resolver search. SourceSystem={SourceSystem}, Resort={Resort}",
                    context.SourceSystem,
                    context.Resort);
       

        if (runtime is null) return null;
        var profile = runtime.Profile;
        var customQuery = !string.IsNullOrWhiteSpace(profile.CustomQuerySql);
        var sql = customQuery ? BuildCustomQuery(profile) : BuildStructuredQuery(profile);

        await using var connection = new SqlConnection(runtime.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        command.Parameters.AddWithValue("@MaxRows", profile.MaxRows);
        command.Parameters.AddWithValue("@ReservationId", context.ReservationId.Trim());
        command.Parameters.AddWithValue("@CheckNumber", context.CheckNumber.Trim());

        //No tiene adaptacion para que se retire la informacion del cheque 
           
 
        command.Parameters.AddWithValue("@Resort", context.Resort.Trim());

         
  
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<CheckItem>();
        Dictionary<string, object?>? first = null;
        var resultRoles = Enumerable.Range(0, reader.FieldCount)
            .Select(index => (Index: index, Role: reader.GetName(index)))
            .Where(x => AllowedRoles.Contains(x.Role))
            .ToArray();
        if (resultRoles.Length == 0)
            throw new InvalidOperationException("La consulta no devolvio alias de ticket reconocidos.");
        while (await reader.ReadAsync(cancellationToken))
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                    foreach (var (index, role) in resultRoles)
                        row[role] = reader.IsDBNull(index) ? null : reader.GetValue(index);

                    first ??= row;

                    if (row.TryGetValue("ItemDescription", out var description) && description is not null)
                        items.Add(new(
                            Convert.ToString(description)?.Trim() ?? "Concepto",
                            Decimal(row, "ItemQuantity") ?? 1m,
                            Decimal(row, "ItemAmount") ?? 0m));
                }

                if (first is null)
                {
                    _logger.LogWarning(
                        "No rows found. ReservationId={ReservationId}, CheckNumber={CheckNumber}, Resort={Resort}, SourceSystem={SourceSystem}",
                        context.ReservationId,
                        context.CheckNumber,
                        context.Resort,
                        context.SourceSystem);

                    return null;
                }

           context.RawTicketText= Text(first,"RawTicketText");


        var receipt = new CheckReceiptDetails(
            Text(first, "GuestName"), Text(first, "Room") ?? context.Room, Text(first, "PointOfSale"),
            Text(first, "CheckNumber") ?? context.CheckNumber, Date(first, "BusinessDate"), Time(first, "Time"),
            Decimal(first, "Subtotal"), Decimal(first, "Tip"), Decimal(first, "Tax"), Text(first, "Header"), Text(first, "Footer"),
            Text(first, "Server"), Text(first, "Table"), Text(first, "GuestCount"), Text(first, "Turn"), Text(first, "CopyNumber"));
        return new(items, Decimal(first, "Total"), Text(first, "Currency") ?? profile.CurrencyConstant, receipt);
    }

    private static string BuildCustomQuery(TicketProfile profile)
    {
        var query = ConfiguredQueryValidator.ValidateAndNormalize(profile.CustomQuerySql!);
        return $"SET ROWCOUNT @MaxRows;\n{query};\nSET ROWCOUNT 0;";
    }

    private static string BuildStructuredQuery(TicketProfile profile)
    {
        var select = profile.FieldMappings
            .Where(x => AllowedRoles.Contains(x.Key))
            .Select(x => $"{Column(x.Value)} AS {Quote(x.Key)}").ToArray();
        if (select.Length == 0) throw new InvalidOperationException("El perfil no contiene campos reconocidos.");
        if (string.IsNullOrWhiteSpace(profile.BaseSchema) || string.IsNullOrWhiteSpace(profile.BaseTable) ||
            string.IsNullOrWhiteSpace(profile.ReservationColumn) || string.IsNullOrWhiteSpace(profile.CheckNumberColumn))
            throw new InvalidOperationException("El perfil estructurado no tiene tabla o filtros completos.");

        var sql = $"SELECT TOP (@MaxRows) {string.Join(',', select)} FROM {Table(profile.BaseSchema, profile.BaseTable)} b";
        if (!string.IsNullOrWhiteSpace(profile.DetailTable))
            sql += $" LEFT JOIN {Table(profile.DetailSchema!, profile.DetailTable)} d ON b.{Quote(profile.BaseJoinColumn!)}=d.{Quote(profile.DetailJoinColumn!)}";
        sql += $" WHERE CONVERT(nvarchar(256),{Column(profile.ReservationColumn)})=@ReservationId" +
               $" AND CONVERT(nvarchar(256),{Column(profile.CheckNumberColumn)})=@CheckNumber";
        if (!string.IsNullOrWhiteSpace(profile.ResortColumn))
            sql += $" AND CONVERT(nvarchar(256),{Column(profile.ResortColumn)})=@Resort";
        return sql;
    }

    private static string Table(string schema, string table) => $"{Quote(Identifier(schema))}.{Quote(Identifier(table))}";
    private static string Column(string reference)
    {
        var parts = reference.Split('.', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || (parts[0] != "b" && parts[0] != "d")) throw new InvalidOperationException($"Referencia de columna inválida: {reference}");
        return $"{parts[0]}.{Quote(Identifier(parts[1]))}";
    }
    private static string Identifier(string value) => IdentifierRegex().IsMatch(value) ? value : throw new InvalidOperationException($"Identificador SQL inválido: {value}");
    private static string Quote(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
    private static string? Text(IReadOnlyDictionary<string, object?> row, string key) => row.TryGetValue(key, out var value) ? Convert.ToString(value)?.Trim() : null;
    private static decimal? Decimal(IReadOnlyDictionary<string, object?> row, string key) => row.TryGetValue(key, out var value) && value is not null ? Convert.ToDecimal(value, CultureInfo.InvariantCulture) : null;


    private static DateTime? Date(
    IReadOnlyDictionary<string, object?> row,
    string key)
{
    if (!row.TryGetValue(key, out var value) || value is null)
        return null;

    if (value is DateTime dt)
        return dt;

    if (value is DateTimeOffset dto)
        return dto.DateTime;

    if (DateTime.TryParse(
        Convert.ToString(value, CultureInfo.InvariantCulture),
        CultureInfo.InvariantCulture,
        DateTimeStyles.None,
        out var parsed))
    {
        return parsed;
    }

    return null;
}
    private static TimeSpan? Time(IReadOnlyDictionary<string, object?> row, string key) => row.TryGetValue(key, out var value) && value is not null && TimeSpan.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) ? parsed : null;
    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_@$#]{0,127}$", RegexOptions.CultureInvariant)] private static partial Regex IdentifierRegex();
}
