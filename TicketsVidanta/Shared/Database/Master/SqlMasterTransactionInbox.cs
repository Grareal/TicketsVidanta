using Microsoft.Data.SqlClient;
using TicketsVidanta.Shared.Database;

namespace TicketsVidanta.Shared.Database.Master;

public sealed class SqlMasterTransactionInbox(ISqlConnectionFactory connectionFactory) : IMasterTransactionInbox
{
    public async Task<int> AddMissingAsync(
        IReadOnlyList<FinancialTransactionCandidate> transactions,
        CancellationToken cancellationToken)
    {
        var inserted = 0;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        foreach (var item in transactions)
        {
            const string sql = """
                INSERT dbo.MasterTransactions
                    (Resort, ReservationId, CheckNumber, Room, Reference, SourceSystem, TcGroup, TrxCode)
                SELECT @Resort, @ReservationId, @CheckNumber, @Room, @Reference, @SourceSystem, @TcGroup, @TrxCode
                WHERE NOT EXISTS
                (
                    SELECT 1 FROM dbo.MasterTransactions WITH (UPDLOCK, HOLDLOCK)
                    WHERE Resort=@Resort AND ReservationId=@ReservationId
                      AND CheckNumber=@CheckNumber AND SourceSystem=@SourceSystem
                );
                """;
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Resort", item.Resort);
            command.Parameters.AddWithValue("@ReservationId", item.ReservationId);
            command.Parameters.AddWithValue("@CheckNumber", item.CheckNumber);
            command.Parameters.AddWithValue("@Room", (object?)item.Room ?? DBNull.Value);
            command.Parameters.AddWithValue("@Reference", (object?)item.Reference ?? DBNull.Value);
            command.Parameters.AddWithValue("@SourceSystem", item.SourceSystem);
            command.Parameters.AddWithValue("@TcGroup", item.TcGroup);
            command.Parameters.AddWithValue("@TrxCode", item.TrxCode);
            inserted += await command.ExecuteNonQueryAsync(cancellationToken);
        }
        return inserted;
    }
}
