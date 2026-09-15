namespace TicketsVidanta.Shared.OperaCloud;

public sealed record DocumentUploadRequest(
    string ReservationId,
    string FileName,
    string MimeType,
    ReadOnlyMemory<byte> Content,
    Guid CorrelationId);
