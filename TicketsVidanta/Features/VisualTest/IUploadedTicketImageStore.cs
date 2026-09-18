namespace TicketsVidanta.Features.VisualTest;

public interface IUploadedTicketImageStore
{
    Task SaveAsync(string checkNumber, Stream content, string contentType, CancellationToken cancellationToken);
    Task<UploadedTicketImage?> FindAsync(string checkNumber, CancellationToken cancellationToken);
}

public sealed record UploadedTicketImage(ReadOnlyMemory<byte> Content, string ContentType);
