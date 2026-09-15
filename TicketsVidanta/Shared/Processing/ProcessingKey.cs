using TicketsVidanta.Features.Tickets.ProcesarCheque;

namespace TicketsVidanta.Shared.Processing;

/// <summary>Clave lógica provisional utilizada para idempotencia.</summary>
public readonly record struct ProcessingKey(
    string Resort,
    string ReservationId,
    string CheckNumber,
    string SourceSystem)
{
    public static ProcessingKey From(CheckProcessingContext context) =>
        new(context.Resort, context.ReservationId, context.CheckNumber, context.SourceSystem);

    // TODO [BUSINESS-RULE]:
    // Pendiente confirmar la llave definitiva de idempotencia.
    //
    // QUÉ DEBE COLOCARSE AQUÍ:
    // Los campos normalizados y reglas de comparación acordados con negocio y los sistemas origen.
    //
    // EJEMPLO ESPERADO:
    // Una especificación que confirme o reemplace Resort + ReservationId + CheckNumber + SourceSystem.
    //
    // NO IMPLEMENTAR HASTA:
    // Validar duplicados, reaperturas y reutilización de números de cheque con los responsables.
}
