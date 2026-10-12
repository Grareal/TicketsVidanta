using System.ComponentModel.DataAnnotations;

namespace TicketsVidanta.Shared.Configuration;

public sealed class ProcessingOptions
{
    public const string SectionName = "Processing";
    public bool EnableBackgroundProcessing { get; init; }
    public ProcessingMode Mode { get; init; } = ProcessingMode.Automatic;
    [Range(1, 86400)] public int IntervalSeconds { get; init; } = 60;
    [Range(1, 1000)] public int BatchSize { get; init; } = 10;
    [Range(1, 20)] public int MaxAttempts { get; init; } = 3;
    [Range(30, 86400)] public int ClaimLeaseSeconds { get; init; } = 300;
    public ControlledProcessingOptions Controlled { get; init; } = new();

    public MasterTransactionSelection? GetControlledSelection() =>
        Mode == ProcessingMode.Controlled ? Controlled.ToSelection() : null;
}

public enum ProcessingMode
{
    Automatic,
    Controlled
}

/// <summary>
/// Guardrail for an authorized, finite validation against Opera Cloud. In Controlled mode the
/// worker only claims Pending rows matching these filters and exits after its first batch.
/// </summary>
public sealed class ControlledProcessingOptions
{
    [Range(1, 100)] public int MaxDocuments { get; init; } = 1;
    public ControlledDocumentOptions[] Documents { get; init; } = [];
    public string[] SourceSystems { get; init; } = [];
    public string[] Resorts { get; init; } = [];
    public string[] TcGroups { get; init; } = [];
    public string[] TrxCodes { get; init; } = [];

    public bool HasSelector => Documents.Length > 0 || HasValue(SourceSystems) || HasValue(Resorts) ||
                               HasValue(TcGroups) || HasValue(TrxCodes);

    public MasterTransactionSelection ToSelection()
    {
        if (!HasSelector)
            throw new InvalidOperationException(
                "Processing:Controlled requiere Documents, SourceSystems, Resorts, TcGroups o TrxCodes.");

        var documents = Documents.Select(document => document.ToSelection()).ToArray();
        return new MasterTransactionSelection(
            MaxDocuments,
            documents,
            Normalize(SourceSystems),
            Normalize(Resorts),
            Normalize(TcGroups),
            Normalize(TrxCodes));
    }

    private static IReadOnlyList<string> Normalize(IReadOnlyList<string> values) => values
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static bool HasValue(IReadOnlyList<string> values) =>
        values.Any(value => !string.IsNullOrWhiteSpace(value));
}

public sealed class ControlledDocumentOptions
{
    [Required] public string? Resort { get; init; }
    [Required] public string? ReservationId { get; init; }
    [Required] public string? CheckNumber { get; init; }

    internal MasterTransactionDocument ToSelection()
    {
        if (string.IsNullOrWhiteSpace(Resort) || string.IsNullOrWhiteSpace(ReservationId) ||
            string.IsNullOrWhiteSpace(CheckNumber))
            throw new InvalidOperationException(
                "Cada Processing:Controlled:Documents debe incluir Resort, ReservationId y CheckNumber.");
        return new MasterTransactionDocument(Resort.Trim(), ReservationId.Trim(), CheckNumber.Trim());
    }
}

public sealed record MasterTransactionDocument(string Resort, string ReservationId, string CheckNumber);

public sealed record MasterTransactionSelection(
    int MaxDocuments,
    IReadOnlyList<MasterTransactionDocument> Documents,
    IReadOnlyList<string> SourceSystems,
    IReadOnlyList<string> Resorts,
    IReadOnlyList<string> TcGroups,
    IReadOnlyList<string> TrxCodes);
