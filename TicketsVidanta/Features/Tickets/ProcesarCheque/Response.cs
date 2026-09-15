using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Features.Tickets.ProcesarCheque;

public sealed record Response(
    bool Succeeded,
    Guid CorrelationId,
    ProcessingStatus Status,
    string Message,
    string? FileName = null,
    string? OperaDocumentId = null,
    bool AlreadyProcessed = false);
