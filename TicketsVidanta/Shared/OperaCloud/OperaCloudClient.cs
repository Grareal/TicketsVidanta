namespace TicketsVidanta.Shared.OperaCloud;

/// <summary>Marcador para la futura integración real. Solo debe registrarse de forma explícita.</summary>
public sealed class OperaCloudClient(HttpClient httpClient) : IOperaCloudClient
{
    public Task<ReservationLookupResult> FindReservationAsync(
        string reservationId,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        _ = httpClient;
        // TODO [OHIP-DISCOVERY]:
        // Pendiente confirmar endpoint OHIP, método HTTP, URL, headers, OAuth, scope,
        // property/hotel ID, semántica exacta de ReservationId, respuesta y códigos HTTP.
        //
        // QUÉ DEBE COLOCARSE AQUÍ:
        // Una llamada tipada y autenticada, con DTOs basados en documentación oficial validada.
        //
        // EJEMPLO ESPERADO:
        // Construir la solicitud con el hotel autorizado, propagar CorrelationId y mapear not-found.
        //
        // NO IMPLEMENTAR HASTA:
        // Confirmar contrato, ambiente, credenciales no productivas y pruebas de conectividad.
        throw new NotImplementedException("La consulta OHIP requiere completar Discovery.");
    }

    public Task<DocumentUploadResult> UploadDocumentAsync(DocumentUploadRequest request, CancellationToken cancellationToken)
    {
        _ = httpClient;
        // TODO [OHIP-DISCOVERY]:
        // Pendiente confirmar endpoint de documentos, método HTTP, URL, headers, OAuth,
        // scope, property/hotel ID, payload, codificación, MIME type, tamaño máximo,
        // respuesta, DocumentId y códigos HTTP.
        //
        // QUÉ DEBE COLOCARSE AQUÍ:
        // Carga parametrizada del PNG y mapeo explícito del resultado OHIP.
        //
        // EJEMPLO ESPERADO:
        // Enviar el documento para la reserva validada y devolver el identificador asignado.
        //
        // NO IMPLEMENTAR HASTA:
        // Validar el contrato de documentos en un ambiente autorizado.
        //
        // TODO [RETRY-POLICY]:
        // Definir con OHIP qué timeouts, 429, Retry-After y 5xx admiten reintento.
        // Los errores funcionales 4xx no deben reintentarse automáticamente.
        throw new NotImplementedException("La carga de documentos OHIP requiere completar Discovery.");
    }
}
