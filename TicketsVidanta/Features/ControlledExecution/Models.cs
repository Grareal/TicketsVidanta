using TicketsVidanta.Features.Tickets.ProcesarCheque;

namespace TicketsVidanta.Features.ControlledExecution;

public sealed record ControlledBatchRequest(
    string? CheckNumber = null,
    string? ReservationId = null,
    int? Top = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string? SourceSystem = null,
    string? Resort = null,
    TicketExecutionMode Mode = TicketExecutionMode.DryRun);

public sealed record ControlledBatchResult(
    int TotalCandidates,
    int SuccessCount,
    int FailedCount,
    IReadOnlyList<Response> Results);

public sealed record ReprocessRequest(
    string? CheckNumber = null,
    string? ReservationId = null,
    string? Resort = null,
    string? SourceSystem = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null);

public sealed record ReprocessResult(
    int ReenqueuedCount,
    string Message);

