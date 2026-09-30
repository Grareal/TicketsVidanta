using Microsoft.Data.SqlClient;

namespace TicketsVidanta.Features.DatabaseConfiguration;

internal sealed class AdvancedQueryTestService(
    IConfigurationRepository repository,
    ConnectionSecretProtector protector)
{
    private static readonly string[] RecommendedRoles =
    [
        "CheckNumber", "ItemDescription", "ItemQuantity", "ItemAmount", "Total"
    ];

    public async Task<AdvancedQueryPreview> ExecuteAsync(
        TestAdvancedQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ConnectionId == Guid.Empty) throw new ArgumentException("Selecciona una conexión.");
        var sql = ReadOnlySqlTemplateValidator.Validate(request.QueryTemplate ?? string.Empty);
        var stored = await repository.GetConnectionAsync(request.ConnectionId, cancellationToken)
            ?? throw new KeyNotFoundException("La conexión no existe.");
        if (!stored.IsEnabled) throw new ArgumentException("La conexión está deshabilitada.");

        await using var connection = new SqlConnection(protector.Unprotect(stored.ProtectedConnectionString));
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
        command.Parameters.AddWithValue("@MaxRows", Math.Clamp(request.MaxRows, 1, 25));
        command.Parameters.AddWithValue("@ReservationId", Required(request.ReservationId, "RESV_NAME_ID"));
        command.Parameters.AddWithValue("@CheckNumber", Required(request.CheckNumber, "CHEQUE_NUMBER"));
        command.Parameters.AddWithValue("@Resort", Required(request.Resort, "RESORT"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        var rows = new List<IReadOnlyList<object?>>();
        while (rows.Count < 25 && await reader.ReadAsync(cancellationToken))
        {
            var row = new object?[reader.FieldCount];
            for (var index = 0; index < reader.FieldCount; index++)
                row[index] = reader.IsDBNull(index) ? null : reader.GetValue(index);
            rows.Add(row);
        }

        var missing = RecommendedRoles
            .Where(role => !columns.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        return new AdvancedQueryPreview(columns, rows, missing);
    }

    private static string Required(string? value, string field) =>
        !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException($"Indica un valor de prueba para {field}.");
}
