using Microsoft.Data.SqlClient;
using TicketsVidanta.Shared.Database;

namespace TicketsVidanta.Shared.Database.Master;

public sealed class SqlMasterTransactionInbox(ISqlConnectionFactory connectionFactory) : IMasterTransactionInbox
{
    public async Task<int> AddMissingAsync(
        IReadOnlyList<FinancialTransactionCandidate> transactions,
        CancellationToken cancellationToken)
    {
        if (transactions.Count == 0) return 0;

        var inserted = 0;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        // SQL Server accepts at most 2,100 parameters. Eight parameters per row leave room for
        // the statement itself and keep source ingestion from issuing one command per document.
        foreach (var batch in transactions.Chunk(200))
        {
            var values = new List<string>();
            await using var command = new SqlCommand { Connection = connection };
            var index = 0;
            foreach (var item in batch)
            {
                values.Add($"(@Resort{index},@ReservationId{index},@CheckNumber{index},@Room{index},@Reference{index},@SourceSystem{index},@TcGroup{index},@TrxCode{index})");
                command.Parameters.AddWithValue($"@Resort{index}", item.Resort);
                command.Parameters.AddWithValue($"@ReservationId{index}", item.ReservationId);
                command.Parameters.AddWithValue($"@CheckNumber{index}", item.CheckNumber);
                command.Parameters.AddWithValue($"@Room{index}", (object?)item.Room ?? DBNull.Value);
                command.Parameters.AddWithValue($"@Reference{index}", (object?)item.Reference ?? DBNull.Value);
                command.Parameters.AddWithValue($"@SourceSystem{index}", item.SourceSystem);
                command.Parameters.AddWithValue($"@TcGroup{index}", item.TcGroup);
                command.Parameters.AddWithValue($"@TrxCode{index}", item.TrxCode);
                index++;
            }
            command.CommandText = $"""
                INSERT dbo.MasterTransactions
                    (Resort, ReservationId, CheckNumber, Room, Reference, SourceSystem, TcGroup, TrxCode)
                SELECT source.Resort, source.ReservationId, source.CheckNumber, source.Room, source.Reference,
                       source.SourceSystem, source.TcGroup, source.TrxCode
                FROM (VALUES {string.Join(",", values)}) AS source
                    (Resort, ReservationId, CheckNumber, Room, Reference, SourceSystem, TcGroup, TrxCode)
                WHERE NOT EXISTS
                (
                    SELECT 1 FROM dbo.MasterTransactions WITH (UPDLOCK, HOLDLOCK)
                    WHERE Resort=source.Resort AND ReservationId=source.ReservationId
                      AND CheckNumber=source.CheckNumber
                );
                """;
            inserted += await command.ExecuteNonQueryAsync(cancellationToken);
        }
        return inserted;
    }
}
