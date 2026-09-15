namespace TicketsVidanta.Shared.OperaCloud;

public sealed record DocumentUploadResult(bool Succeeded, string? DocumentId, string? Error);
