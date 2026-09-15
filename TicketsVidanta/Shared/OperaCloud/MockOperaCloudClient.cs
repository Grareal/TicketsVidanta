namespace TicketsVidanta.Shared.OperaCloud;

/// <summary>Cliente simulado para desarrollo; no realiza llamadas de red.</summary>
public sealed class MockOperaCloudClient(ILogger<MockOperaCloudClient> logger) : IOperaCloudClient
{
    public Task<ReservationLookupResult> FindReservationAsync(
        string reservationId,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Mock reservation lookup. CorrelationId={CorrelationId}, ReservationId={ReservationId}",
            correlationId, reservationId);
        return Task.FromResult(new ReservationLookupResult(true, reservationId));
    }

    public Task<DocumentUploadResult> UploadDocumentAsync(DocumentUploadRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var documentId = $"MOCK-{Guid.NewGuid():N}";
        logger.LogInformation("Mock document upload. CorrelationId={CorrelationId}, FileName={FileName}",
            request.CorrelationId, request.FileName);
        return Task.FromResult(new DocumentUploadResult(true, documentId, null));
    }
}
