namespace TicketsVidanta.Features.SyncEngine;

public sealed record SyncTableCatalogEntry(
    int CatalogId,
    string SourceSystem,
    string Resort,
    string ConnectionName,
    string RemoteTable,
    string LocalTable,
    string DateColumn,
    string KeyColumn,
    DateOnly InitialExtractDate,
    DateTime LastExtractedDate,
    string LastExtractedKey,
    int BatchSize,
    int OverlapMinutes,
    int IntervalSeconds,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SaveSyncCatalogRequest(
    int? CatalogId,
    string? SourceSystem,
    string? Resort,
    string? ConnectionName,
    string? RemoteTable,
    string? LocalTable,
    string? DateColumn,
    string? KeyColumn,
    DateOnly? InitialExtractDate,
    int BatchSize = 5000,
    int OverlapMinutes = 15,
    int IntervalSeconds = 180,
    bool IsEnabled = true);

public sealed record SyncBatchResult(
    int CatalogId,
    string SourceSystem,
    string Resort,
    string TableName,
    int RowsExtracted,
    int RowsUpserted,
    DateTime NextWatermarkDate,
    string NextWatermarkKey,
    bool Succeeded,
    string? ErrorMessage);

public sealed record SyncBatchLogEntry(
    long BatchId,
    int CatalogId,
    string SourceSystem,
    string Resort,
    string TableName,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int RowsExtracted,
    int RowsUpserted,
    string Status,
    string? ErrorMessage);

public sealed class SyncEngineOptions
{
    public const string SectionName = "SyncEngine";
    public bool Enabled { get; init; } = false;
    public int DefaultIntervalSeconds { get; init; } = 180;
    public int MaxConcurrentSyncs { get; init; } = 2;
}

