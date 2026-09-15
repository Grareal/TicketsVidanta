namespace TicketsVidanta.Shared.OperaCloud;

public interface IOperaCloudClient
{
    Task<ReservationLookupResult> FindReservationAsync(string reservationId, Guid correlationId, CancellationToken cancellationToken);
    Task<DocumentUploadResult> UploadDocumentAsync(DocumentUploadRequest request, CancellationToken cancellationToken);
}
