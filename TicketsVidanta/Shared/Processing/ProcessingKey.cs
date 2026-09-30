using TicketsVidanta.Features.Tickets.ProcesarCheque;

namespace TicketsVidanta.Shared.Processing;

/// <summary>
/// Llave de negocio definitiva: un cheque real es único dentro de un resort.
/// ReservationId y SourceSystem se conservan como metadatos, pero no participan en la igualdad.
/// </summary>
public readonly struct ProcessingKey : IEquatable<ProcessingKey>
{
    public ProcessingKey(string resort, string reservationId, string checkNumber, string sourceSystem)
    {
        Resort = resort;
        ReservationId = reservationId;
        CheckNumber = checkNumber;
        SourceSystem = sourceSystem;
    }

    public string Resort { get; }
    public string ReservationId { get; }
    public string CheckNumber { get; }
    public string SourceSystem { get; }

    public static ProcessingKey From(CheckProcessingContext context) =>
        new(context.Resort.Trim(), context.ReservationId.Trim(), context.CheckNumber.Trim(), context.SourceSystem.Trim());

    public bool Equals(ProcessingKey other) =>
        StringComparer.OrdinalIgnoreCase.Equals(Resort, other.Resort) &&
        StringComparer.OrdinalIgnoreCase.Equals(CheckNumber, other.CheckNumber);

    public override bool Equals(object? obj) => obj is ProcessingKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(
        StringComparer.OrdinalIgnoreCase.GetHashCode(Resort),
        StringComparer.OrdinalIgnoreCase.GetHashCode(CheckNumber));

    public static bool operator ==(ProcessingKey left, ProcessingKey right) => left.Equals(right);
    public static bool operator !=(ProcessingKey left, ProcessingKey right) => !left.Equals(right);
}
