namespace TicketsVidanta.Features.SyncEngine;

public interface ITableSyncService
{
    Task<SyncBatchResult> SyncTableAsync(SyncTableCatalogEntry entry, CancellationToken cancellationToken);
}

