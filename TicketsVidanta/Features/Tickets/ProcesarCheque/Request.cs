namespace TicketsVidanta.Features.Tickets.ProcesarCheque;

public enum TicketExecutionMode
{
    Live,
    DryRun,
    Simulation
}

public sealed record Request(
    string? Resort,
    string? ReservationId,
    string? CheckNumber,
    string? Room,
    string? Reference,
    string? SourceSystem = null,
    string? TcGroup = null,
    string? TrxCode = null,
    TicketExecutionMode Mode = TicketExecutionMode.Live);

