namespace TicketsVidanta.Shared.OperaCloud;

public sealed record DocumentUploadRequest(
    string Resort,
    string ReservationId,
    string CheckNumber,
    string FileName,
    string MimeType,
    ReadOnlyMemory<byte> Content,
    Guid CorrelationId);
