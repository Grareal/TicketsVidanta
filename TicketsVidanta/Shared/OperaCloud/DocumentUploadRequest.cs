namespace TicketsVidanta.Shared.OperaCloud;

public sealed record DocumentUploadRequest(
    string ReservationId,
    string CheckNumber,
    string FileName,
    string MimeType,
    ReadOnlyMemory<byte> Content,
    Guid CorrelationId);
