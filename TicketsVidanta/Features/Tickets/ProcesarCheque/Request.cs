namespace TicketsVidanta.Features.Tickets.ProcesarCheque;

public sealed record Request(
    string? Resort,
    string? ReservationId,
    string? CheckNumber,
    string? Room,
    string? Reference,
    string? SourceSystem = null,
    string? TcGroup = null,
    string? TrxCode = null);
