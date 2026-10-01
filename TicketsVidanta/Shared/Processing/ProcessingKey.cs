using TicketsVidanta.Features.Tickets.ProcesarCheque;

namespace TicketsVidanta.Shared.Processing;

/// <summary>
/// Identidad de una carga a Opera. SourceSystem se conserva para resolver y auditar,
/// pero no distingue cargas: varias filas contables del mismo cheque se cargan una sola vez.
/// </summary>
public readonly struct ProcessingKey : IEquatable<ProcessingKey>
{
    public ProcessingKey(string resort, string reservationId, string checkNumber, string sourceSystem)
    {
        Resort = resort.Trim();
        ReservationId = reservationId.Trim();
        CheckNumber = checkNumber.Trim();
        SourceSystem = sourceSystem.Trim();
    }

    public string Resort { get; }
    public string ReservationId { get; }
    public string CheckNumber { get; }
    public string SourceSystem { get; }

    public static ProcessingKey From(CheckProcessingContext context) =>
        new(context.Resort, context.ReservationId, context.CheckNumber, context.SourceSystem);

    public bool Equals(ProcessingKey other) =>
        Same(Resort, other.Resort) && Same(ReservationId, other.ReservationId) && Same(CheckNumber, other.CheckNumber);

    public override bool Equals(object? obj) => obj is ProcessingKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(
        StringComparer.OrdinalIgnoreCase.GetHashCode(Resort),
        StringComparer.OrdinalIgnoreCase.GetHashCode(ReservationId),
        StringComparer.OrdinalIgnoreCase.GetHashCode(CheckNumber));

    public static bool operator ==(ProcessingKey left, ProcessingKey right) => left.Equals(right);
    public static bool operator !=(ProcessingKey left, ProcessingKey right) => !left.Equals(right);

    private static bool Same(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
