namespace TicketsVidanta.Features.SyncEngine;

public interface ISyncCatalogRepository
{
    Task<IReadOnlyList<SyncTableCatalogEntry>> GetActiveEntriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SyncTableCatalogEntry>> GetAllEntriesAsync(CancellationToken cancellationToken);
    Task<SyncTableCatalogEntry?> GetEntryByIdAsync(int catalogId, CancellationToken cancellationToken);
    Task<int> SaveEntryAsync(SaveSyncCatalogRequest request, CancellationToken cancellationToken);
    Task DeleteEntryAsync(int catalogId, CancellationToken cancellationToken);
    Task UpdateWatermarkAsync(int catalogId, DateTime lastDate, string lastKey, CancellationToken cancellationToken);
    Task<long> StartBatchLogAsync(int catalogId, string sourceSystem, string resort, string tableName, CancellationToken cancellationToken);
    Task CompleteBatchLogAsync(long batchId, int rowsExtracted, int rowsUpserted, string status, string? error, CancellationToken cancellationToken);
    Task<IReadOnlyList<SyncBatchLogEntry>> GetRecentBatchLogsAsync(int top, CancellationToken cancellationToken);
}

