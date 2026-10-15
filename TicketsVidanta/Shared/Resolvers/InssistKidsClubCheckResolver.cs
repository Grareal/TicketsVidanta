using Microsoft.Data.SqlClient;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Database;
using TicketsVidanta.Shared.Database.Commerce;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.Resolvers;

public sealed class InssistKidsClubCheckResolver(ICommerceSqlConnectionFactory connectionFactory) : ICheckResolver
{
    public const string SourceSystemName = "INSSIST_KIDSCLUB";

    public bool CanHandle(CheckProcessingContext context) =>
        string.Equals(context.SourceSystem, SourceSystemName, StringComparison.OrdinalIgnoreCase);

    public async Task<CheckDetail?> ResolveAsync(CheckProcessingContext context, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT hc.fec_che, hc.hra_alta, hc.caja, cd.descrip, cd.c_desc,
                   hc.folio, hc.num_hab, hc.total, hm.platillo, hm.importe
            FROM hotche hc
            INNER JOIN hotcom hm
              ON hc.caja = hm.caja
             AND hc.turno = hm.turno
             AND hc.folio = hm.folio
            INNER JOIN H_Cajas_Descrip cd ON hc.caja = cd.clave
            WHERE hc.caja IN ('56', '57')
              AND hc.cargar_a = @ReservationId
              AND hc.folio = @CheckNumber
            ORDER BY hc.fec_che DESC;
            """;

        await using var connection = connectionFactory.Create(SourceSystemName, context.Resort);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@ReservationId", System.Data.SqlDbType.VarChar, 80)
        { Value = context.ReservationId.Trim() });
        command.Parameters.Add(new SqlParameter("@CheckNumber", System.Data.SqlDbType.VarChar, 80)
        { Value = context.CheckNumber.Trim() });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<CheckItem>();
        CheckReceiptDetails? receipt = null;
        decimal? total = null;
        while (await reader.ReadAsync(cancellationToken))
        {
            var amount = reader.IsDBNull(9) ? 0m : Convert.ToDecimal(reader.GetValue(9));
            items.Add(new CheckItem(Convert.ToString(reader.GetValue(8))?.Trim() ?? "Kids Club", 1m, amount));
            total ??= reader.IsDBNull(7) ? null : Convert.ToDecimal(reader.GetValue(7));
            receipt ??= new CheckReceiptDetails(
                Room: reader.IsDBNull(6) ? context.Room : Convert.ToString(reader.GetValue(6))?.Trim(),
                PointOfSale: reader.IsDBNull(3) ? null : Convert.ToString(reader.GetValue(3))?.Trim(),
                CheckNumber: reader.IsDBNull(5) ? context.CheckNumber : Convert.ToString(reader.GetValue(5))?.Trim(),
                BusinessDate: reader.IsDBNull(0) ? null : FlexibleDateParser.ParseNullable(reader.GetValue(0)),
                Time: ReadTime(reader, 1),
                Header: reader.IsDBNull(4) ? null : Convert.ToString(reader.GetValue(4))?.Trim());
        }

        return receipt is null ? null : new CheckDetail(items, total, "MXN", receipt);
    }

    private static TimeSpan? ReadTime(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null :
        reader.GetValue(ordinal) is TimeSpan value ? value : TimeSpan.TryParse(Convert.ToString(reader.GetValue(ordinal)), out var parsed) ? parsed : null;
}
