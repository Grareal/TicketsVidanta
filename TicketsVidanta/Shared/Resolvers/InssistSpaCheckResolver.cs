using Microsoft.Data.SqlClient;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Database.Commerce;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.Resolvers;

public sealed class InssistSpaCheckResolver(ICommerceSqlConnectionFactory connectionFactory) : ICheckResolver
{
    public const string SourceSystemName = "INSSIST_SPA";

    public bool CanHandle(CheckProcessingContext context) =>
        string.Equals(context.SourceSystem, SourceSystemName, StringComparison.OrdinalIgnoreCase);

    public async Task<CheckDetail?> ResolveAsync(CheckProcessingContext context, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                RTRIM(r.cli_Nombre) + ' ' + RTRIM(r.cli_Apellido) AS Huesped,
                r.res_NumeroHabitacion,
                s.srp_Cheque,
                h.fec_che,
                h.hra_alta,
                h.sub_total,
                h.tip,
                h.impuesto,
                h.total,
                s.srp_Cantidad,
                RTRIM(s.srp_Nombre) AS Servicio,
                s.srp_Importe,
                sp.par_PuntoVenta,
                sp.par_EncabezadoTickets,
                sp.par_LeyendaTickets
            FROM spares r
            INNER JOIN spasrp s ON s.srp_ClaveReservacion = r.res_Clave
            INNER JOIN spapgo p ON p.pgo_Cheque = s.srp_Cheque
            LEFT JOIN hotche h
              ON h.folio = s.srp_Cheque
             AND LTRIM(RTRIM(h.cargar_a)) = LTRIM(RTRIM(p.pgo_CargoA))
            LEFT JOIN spapar sp ON sp.par_PuntoVenta = h.caja
            WHERE LTRIM(RTRIM(p.pgo_CargoA)) = @ReservationId
              AND LTRIM(RTRIM(CONVERT(nvarchar(80), s.srp_Cheque))) = @CheckNumber
            ORDER BY s.srp_Fecha, s.srp_HoraIni;
            """;

        await using var connection = connectionFactory.Create(SourceSystemName, context.Resort);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ReservationId", context.ReservationId.Trim());
        command.Parameters.AddWithValue("@CheckNumber", context.CheckNumber.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<CheckItem>();
        CheckReceiptDetails? receipt = null;
        decimal? total = null;
        while (await reader.ReadAsync(cancellationToken))
        {
            var quantity = GetDecimal(reader, 9) ?? 1m;
            var amount = GetDecimal(reader, 11) ?? 0m;
            items.Add(new CheckItem(GetString(reader, 10) ?? "Servicio SPA", quantity, amount));
            total ??= GetDecimal(reader, 8);
            receipt ??= new CheckReceiptDetails(
                GetString(reader, 0), GetString(reader, 1), GetString(reader, 12), GetString(reader, 2),
                GetDateTime(reader, 3), GetTimeSpan(reader, 4), GetDecimal(reader, 5),
                GetDecimal(reader, 6), GetDecimal(reader, 7), GetString(reader, 13), GetString(reader, 14));
        }

        return receipt is null ? null : new CheckDetail(items, total, "MXN", receipt);
    }

    private static string? GetString(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal))?.Trim();
    private static decimal? GetDecimal(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal));
    private static DateTime? GetDateTime(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal));
    private static TimeSpan? GetTimeSpan(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null :
        reader.GetValue(ordinal) is TimeSpan value ? value : TimeSpan.TryParse(Convert.ToString(reader.GetValue(ordinal)), out var parsed) ? parsed : null;
}
