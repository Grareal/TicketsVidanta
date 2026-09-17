using System.ComponentModel.DataAnnotations;

namespace TicketsVidanta.Shared.Configuration;

public sealed class ProcessingOptions
{
    public const string SectionName = "Processing";
    public bool EnableBackgroundProcessing { get; init; }
    [Range(1, 86400)] public int IntervalSeconds { get; init; } = 60;
    [Range(1, 1000)] public int BatchSize { get; init; } = 10;
    [Range(1, 20)] public int MaxAttempts { get; init; } = 3;
    [Range(30, 86400)] public int ClaimLeaseSeconds { get; init; } = 300;
}
