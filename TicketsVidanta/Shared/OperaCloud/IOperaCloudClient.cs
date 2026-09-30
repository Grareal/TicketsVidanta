namespace TicketsVidanta.Shared.OperaCloud;

public interface IOperaCloudClient
{
    Task<ReservationLookupResult> FindReservationAsync(string resort, string reservationId, Guid correlationId, CancellationToken cancellationToken);
    Task<DocumentUploadResult> UploadDocumentAsync(DocumentUploadRequest request, CancellationToken cancellationToken);
}
