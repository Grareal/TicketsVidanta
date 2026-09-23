namespace TicketsVidanta.Shared.OperaCloud;

/// <summary>Bloquea por construcción cualquier acceso a Opera Cloud.</summary>
public sealed class DisabledOperaCloudClient : IOperaCloudClient
{
    public Task<ReservationLookupResult> FindReservationAsync(
        string reservationId,
        Guid correlationId,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("La integración con Opera Cloud está deshabilitada.");

    public Task<DocumentUploadResult> UploadDocumentAsync(
        DocumentUploadRequest request,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("La carga a Opera Cloud está deshabilitada.");
}
