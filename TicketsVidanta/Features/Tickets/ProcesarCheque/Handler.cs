using TicketsVidanta.Shared.Auditing;
using TicketsVidanta.Shared.Models;
using TicketsVidanta.Shared.Naming;
using TicketsVidanta.Shared.OperaCloud;
using TicketsVidanta.Shared.Processing;
using TicketsVidanta.Shared.Resolvers;
using TicketsVidanta.Shared.TicketGeneration;

namespace TicketsVidanta.Features.Tickets.ProcesarCheque;

public sealed class Handler(
    IProcessingRegistry registry,
    ICheckResolverSelector resolverSelector,
    IOperaCloudClient operaCloudClient,
    ITicketRenderer ticketRenderer,
    ITicketFileNameGenerator fileNameGenerator,
    IAuditService auditService,
    ILogger<Handler> logger) : ITicketProcessor
{
    public async Task<Response> ProcessAsync(Request request, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid();
        var context = CreateContext(request, correlationId);
        var key = ProcessingKey.From(context);

        logger.LogInformation(
            "Starting processing. CorrelationId={CorrelationId}, ReservationId={ReservationId}, CheckNumber={CheckNumber}",
            correlationId, context.ReservationId, context.CheckNumber);

        if (!await registry.TryRegisterStartedAsync(key, correlationId, cancellationToken))
        {
            logger.LogWarning("Duplicate processing prevented. CorrelationId={CorrelationId}, ReservationId={ReservationId}, CheckNumber={CheckNumber}",
                correlationId, context.ReservationId, context.CheckNumber);
            return new Response(false, correlationId, ProcessingStatus.Pending,
                "El cheque ya fue registrado para procesamiento.", AlreadyProcessed: true);
        }

        string? fileName = null;
        try
        {
            context.ProcessingStatus = ProcessingStatus.Resolving;
            var selection = resolverSelector.Select(context);
            if (!selection.IsSuccess)
                return await FailAsync(context, key, selection.Error ?? "No fue posible seleccionar un resolver.", null, cancellationToken);

            var detail = await selection.Resolver!.ResolveAsync(context, cancellationToken);
            if (detail is null)
                return await FailAsync(context, key, "El resolver no encontró el detalle del cheque.", null, cancellationToken);

            context.Items = detail.Items;
            context.ProcessingStatus = ProcessingStatus.Resolved;
            logger.LogInformation("Check detail retrieved. CorrelationId={CorrelationId}, ItemCount={ItemCount}", correlationId, detail.Items.Count);

            var reservation = await operaCloudClient.FindReservationAsync(
                context.ReservationId, correlationId, cancellationToken);
            if (!reservation.Found)
                return await FailAsync(context, key, "ReservationId no fue localizado en Opera Cloud.", null, cancellationToken);

            context.ProcessingStatus = ProcessingStatus.GeneratingDocument;
            logger.LogInformation("Generating document. CorrelationId={CorrelationId}", correlationId);
            var generated = await ticketRenderer.RenderAsync(context, detail, cancellationToken);
            fileName = fileNameGenerator.Generate(context, DateTimeOffset.UtcNow);
            logger.LogInformation("Document generated. CorrelationId={CorrelationId}, FileName={FileName}", correlationId, fileName);

            context.ProcessingStatus = ProcessingStatus.Uploading;
            logger.LogInformation("Uploading to Opera Cloud. CorrelationId={CorrelationId}", correlationId);
            var upload = await operaCloudClient.UploadDocumentAsync(
                new DocumentUploadRequest(context.ReservationId, fileName, generated.MimeType, generated.Content, correlationId),
                cancellationToken);
            if (!upload.Succeeded)
                return await FailAsync(context, key, upload.Error ?? "Opera Cloud rechazó el documento.", fileName, cancellationToken);

            context.ProcessingStatus = ProcessingStatus.Completed;
            context.CompletedAt = DateTimeOffset.UtcNow;
            await registry.RegisterCompletedAsync(key, correlationId, cancellationToken);
            await auditService.WriteAsync(CreateAudit(context, fileName, upload.DocumentId, null), cancellationToken);
            logger.LogInformation("Processing completed. CorrelationId={CorrelationId}, OperaDocumentId={OperaDocumentId}",
                correlationId, upload.DocumentId);

            return new Response(true, correlationId, ProcessingStatus.Completed,
                "Cheque procesado correctamente con infraestructura Mock.", fileName, upload.DocumentId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Processing failed. CorrelationId={CorrelationId}, ReservationId={ReservationId}, CheckNumber={CheckNumber}",
                correlationId, context.ReservationId, context.CheckNumber);
            return await FailAsync(context, key, "El procesamiento terminó con un error controlado.", fileName, cancellationToken);
        }
    }

    private async Task<Response> FailAsync(
        CheckProcessingContext context,
        ProcessingKey key,
        string message,
        string? fileName,
        CancellationToken cancellationToken)
    {
        context.ProcessingStatus = ProcessingStatus.Failed;
        context.CompletedAt = DateTimeOffset.UtcNow;
        await registry.RegisterFailedAsync(key, context.CorrelationId, message, cancellationToken);
        await auditService.WriteAsync(CreateAudit(context, fileName, null, message), cancellationToken);
        logger.LogWarning(
            "Processing failed in a controlled manner. CorrelationId={CorrelationId}, ReservationId={ReservationId}, CheckNumber={CheckNumber}, Reason={Reason}",
            context.CorrelationId, context.ReservationId, context.CheckNumber, message);
        return new Response(false, context.CorrelationId, ProcessingStatus.Failed, message, fileName);
    }

    private static CheckProcessingContext CreateContext(Request request, Guid correlationId) => new()
    {
        Resort = request.Resort!.Trim(),
        ReservationId = request.ReservationId!.Trim(),
        CheckNumber = request.CheckNumber!.Trim(),
        Room = request.Room?.Trim(),
        Reference = request.Reference?.Trim(),
        // Para el endpoint de desarrollo, Reference="MOCK" permite usar el resolver Mock
        // del ejemplo solicitado. Los orígenes reales deberán informar SourceSystem explícitamente.
        SourceSystem = (request.SourceSystem ?? request.Reference ?? string.Empty).Trim(),
        CorrelationId = correlationId
    };

    private static TicketProcessingAudit CreateAudit(
        CheckProcessingContext context,
        string? fileName,
        string? documentId,
        string? error) => new(
            Guid.NewGuid(), context.CorrelationId, context.Resort, context.ReservationId,
            context.CheckNumber, context.SourceSystem, context.ProcessingStatus, 1,
            context.StartedAt, context.CompletedAt, documentId, fileName, error);
}
