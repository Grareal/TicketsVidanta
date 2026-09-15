using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.Processing;

public sealed record ProcessingRecord(
    ProcessingKey Key,
    Guid CorrelationId,
    ProcessingStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage);
