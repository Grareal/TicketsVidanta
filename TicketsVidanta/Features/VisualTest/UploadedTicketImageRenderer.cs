using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Models;
using TicketsVidanta.Shared.TicketGeneration;

namespace TicketsVidanta.Features.VisualTest;

public sealed class UploadedTicketImageRenderer(
    IUploadedTicketImageStore imageStore,
    MockTicketRenderer fallbackRenderer,
    ILogger<UploadedTicketImageRenderer> logger) : ITicketRenderer
{
    public async Task<GeneratedTicket> RenderAsync(
        CheckProcessingContext context,
        CheckDetail detail,
        CancellationToken cancellationToken)
    {
        var image = await imageStore.FindAsync(context.CheckNumber, cancellationToken);
        if (image is null)
            return await fallbackRenderer.RenderAsync(context, detail, cancellationToken);

        logger.LogInformation(
            "Using uploaded visual-test image. CorrelationId={CorrelationId}, CheckNumber={CheckNumber}",
            context.CorrelationId, context.CheckNumber);
        return new GeneratedTicket(image.Content, image.ContentType);
    }
}
