namespace TicketsVidanta.Shared.Database.Master;

/// <summary>Vista conceptual mínima de una transacción pendiente.</summary>
public sealed record MasterTransaction(
    string Id,
    string Resort,
    string ReservationId,
    string CheckNumber,
    string? Room,
    string? Reference,
    string SourceSystem,
    string? TcGroup = null,
    string? TrxCode = null);
