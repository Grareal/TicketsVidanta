using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using TicketsVidanta.Shared.Database;

namespace TicketsVidanta.Shared.Database.Master;

public sealed class SqlMasterTransactionInbox(
    ISqlConnectionFactory connectionFactory,
    ILogger<SqlMasterTransactionInbox>? logger = null) : IMasterTransactionInbox
{
    public async Task<int> AddMissingAsync(
        IReadOnlyList<FinancialTransactionCandidate> transactions,
        CancellationToken cancellationToken)
    {
        if (transactions.Count == 0) return 0;

        // Deduplicar intra-lote por BusinessKey (Resort, ReservationId, CheckNumber)
        // para prevenir errores 2627 (UQ_MasterTransactions_BusinessKey) cuando
        // FINANCIAL_TRANSACTIONS_P_DET_CLOUD contiene multiples lineas para un mismo cheque.
        var distinctTransactions = Deduplicate(transactions);

        if (transactions.Count != distinctTransactions.Count)
        {
            logger?.LogInformation(
                "[SqlMasterTransactionInbox] Lote recibido con {Total} transacciones. Deduplicadas a {Distinct} cheques unicos por (Resort, ReservationId, CheckNumber).",
                transactions.Count, distinctTransactions.Count);
        }

        var inserted = 0;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        // SQL Server accepts at most 2,100 parameters. Eight parameters per row leave room for
        // the statement itself and keep source ingestion from issuing one command per document.
        foreach (var batch in distinctTransactions.Chunk(200))
        {
            var values = new List<string>();
            await using var command = new SqlCommand { Connection = connection };
            var index = 0;
            foreach (var item in batch)
            {
                values.Add($"(@Resort{index},@ReservationId{index},@CheckNumber{index},@Room{index},@Reference{index},@SourceSystem{index},@TcGroup{index},@TrxCode{index})");
                command.Parameters.AddWithValue($"@Resort{index}", item.Resort.Trim());
                command.Parameters.AddWithValue($"@ReservationId{index}", item.ReservationId.Trim());
                command.Parameters.AddWithValue($"@CheckNumber{index}", item.CheckNumber.Trim());
                command.Parameters.AddWithValue($"@Room{index}", (object?)item.Room?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue($"@Reference{index}", (object?)item.Reference?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue($"@SourceSystem{index}", item.SourceSystem.Trim());
                command.Parameters.AddWithValue($"@TcGroup{index}", item.TcGroup.Trim());
                command.Parameters.AddWithValue($"@TrxCode{index}", item.TrxCode.Trim());
                index++;
            }
            command.CommandText = $"""
                ;WITH BatchSource AS
                (
                    SELECT source.Resort, source.ReservationId, source.CheckNumber, source.Room, source.Reference,
                           source.SourceSystem, source.TcGroup, source.TrxCode,
                           ROW_NUMBER() OVER (
                               PARTITION BY source.Resort, source.ReservationId, source.CheckNumber
                               ORDER BY (SELECT NULL)
                           ) AS RowNum
                    FROM (VALUES {string.Join(",", values)}) AS source
                        (Resort, ReservationId, CheckNumber, Room, Reference, SourceSystem, TcGroup, TrxCode)
                )
                INSERT dbo.MasterTransactions
                    (Resort, ReservationId, CheckNumber, Room, Reference, SourceSystem, TcGroup, TrxCode)
                SELECT Resort, ReservationId, CheckNumber, Room, Reference,
                       SourceSystem, TcGroup, TrxCode
                FROM BatchSource
                WHERE RowNum = 1
                  AND NOT EXISTS
                  (
                      SELECT 1 FROM dbo.MasterTransactions WITH (UPDLOCK, HOLDLOCK)
                      WHERE Resort = BatchSource.Resort
                        AND ReservationId = BatchSource.ReservationId
                        AND CheckNumber = BatchSource.CheckNumber
                  );
                """;
            inserted += await command.ExecuteNonQueryAsync(cancellationToken);
        }
        return inserted;
    }

    public static IReadOnlyList<FinancialTransactionCandidate> Deduplicate(
        IReadOnlyList<FinancialTransactionCandidate> transactions)
    {
        if (transactions.Count == 0) return [];

        return transactions
            .GroupBy(
                t => (
                    Resort: (t.Resort ?? string.Empty).Trim(),
                    ReservationId: (t.ReservationId ?? string.Empty).Trim(),
                    CheckNumber: (t.CheckNumber ?? string.Empty).Trim()
                ),
                (key, group) => group.First(),
                new BusinessKeyComparer())
            .ToList();
    }

    private sealed class BusinessKeyComparer : IEqualityComparer<(string Resort, string ReservationId, string CheckNumber)>
    {
        public bool Equals(
            (string Resort, string ReservationId, string CheckNumber) x,
            (string Resort, string ReservationId, string CheckNumber) y) =>
            string.Equals(x.Resort, y.Resort, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.ReservationId, y.ReservationId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.CheckNumber, y.CheckNumber, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Resort, string ReservationId, string CheckNumber) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Resort),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ReservationId),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.CheckNumber));
    }
}
