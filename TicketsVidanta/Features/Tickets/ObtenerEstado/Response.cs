using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Features.Tickets.ObtenerEstado;

public sealed record Response(
    Guid CorrelationId,
    ProcessingStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage);
