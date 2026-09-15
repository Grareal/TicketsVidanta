using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.TicketGeneration;

/// <summary>Impide generar documentos no aprobados fuera de Development.</summary>
public sealed class PendingTicketRenderer : ITicketRenderer
{
    public Task<GeneratedTicket> RenderAsync(
        CheckProcessingContext context,
        CheckDetail detail,
        CancellationToken cancellationToken)
    {
        // TODO [TICKET-RENDERING]:
        // Sustituir por el renderer aprobado después de definir diseño, librería,
        // dimensiones, fuentes, contenido, privacidad y límites de Opera Cloud.
        throw new NotImplementedException("El renderer productivo requiere completar Discovery.");
    }
}
