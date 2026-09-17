using Microsoft.Data.SqlClient;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Database;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.Resolvers;

public sealed class SqlCheckResolver(ISqlConnectionFactory connectionFactory) : ICheckResolver
{
    public bool CanHandle(CheckProcessingContext context) =>
        string.Equals(context.SourceSystem, "LOCALSQL", StringComparison.OrdinalIgnoreCase);

    public async Task<CheckDetail?> ResolveAsync(
        CheckProcessingContext context,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.Total, c.Currency, i.Description, i.Quantity, i.Amount
            FROM dbo.CommerceChecks c
            LEFT JOIN dbo.CommerceCheckItems i ON i.CheckId = c.Id
            WHERE c.Resort=@Resort AND c.ReservationId=@ReservationId
              AND c.CheckNumber=@CheckNumber AND c.SourceSystem=@SourceSystem
            ORDER BY i.LineNumber;
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Resort", context.Resort);
        command.Parameters.AddWithValue("@ReservationId", context.ReservationId);
        command.Parameters.AddWithValue("@CheckNumber", context.CheckNumber);
        command.Parameters.AddWithValue("@SourceSystem", context.SourceSystem);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        decimal? total = null;
        string? currency = null;
        var items = new List<CheckItem>();
        var found = false;
        while (await reader.ReadAsync(cancellationToken))
        {
            found = true;
            total = reader.IsDBNull(0) ? null : reader.GetDecimal(0);
            currency = reader.IsDBNull(1) ? null : reader.GetString(1);
            if (!reader.IsDBNull(2))
                items.Add(new CheckItem(reader.GetString(2), reader.GetDecimal(3), reader.GetDecimal(4)));
        }

        return found ? new CheckDetail(items, total, currency) : null;
    }
}
