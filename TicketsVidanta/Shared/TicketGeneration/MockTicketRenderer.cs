using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.TicketGeneration;

/// <summary>Genera un PNG transparente de 1x1 válido exclusivamente para probar el pipeline.</summary>
public sealed class MockTicketRenderer : ITicketRenderer
{
    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M/wHwAF/gL+Xw3LAAAAAElFTkSuQmCC");

    public Task<GeneratedTicket> RenderAsync(
        CheckProcessingContext context,
        CheckDetail detail,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // TODO [TICKET-RENDERING]:
        // Pendiente definir dimensiones, resolución, DPI, logo, tipografías, colores,
        // columnas, impuestos, propinas, descuentos, moneda, fechas, múltiples páginas
        // y longitud máxima. Sustituir este mock después de aprobar el diseño.
        return Task.FromResult(new GeneratedTicket(MinimalPng, "image/png"));
    }
}
