using System.Text;
using TicketsVidanta.Features.Tickets.ProcesarCheque;

namespace TicketsVidanta.Shared.Naming;

public sealed class TicketFileNameGenerator : ITicketFileNameGenerator
{
    public string Generate(CheckProcessingContext context, DateTimeOffset timestamp)
    {
        // TODO [BUSINESS-RULE]:
        // Confirmar nomenclatura definitiva requerida por Opera Cloud y negocio.
        var raw = $"VID-{context.Resort}-{context.ReservationId}-{context.CheckNumber}-{timestamp.UtcDateTime:yyyyMMddHHmmss}-{Guid.NewGuid():N}.png";
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var result = new StringBuilder(raw.Length);
        foreach (var character in raw)
            result.Append(invalid.Contains(character) || char.IsControl(character) ? '-' : character);
        return result.ToString();
    }
}
