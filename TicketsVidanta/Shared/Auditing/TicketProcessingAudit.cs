using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.Auditing;

public sealed record TicketProcessingAudit(
    Guid Id,
    Guid CorrelationId,
    string Resort,
    string ReservationId,
    string CheckNumber,
    string SourceSystem,
    ProcessingStatus Status,
    int AttemptCount,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? OperaDocumentId,
    string? FileName,
    string? ErrorMessage);
