namespace TicketsVidanta.Features.VisualTest;
using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Exceptions;
using TicketsVidanta.Shared.OperaCloud;

public static class Endpoint
{
    public static IEndpointRouteBuilder MapVisualTestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/visual-test", () => Results.Redirect("/visual-test/index.html"));

        endpoints.MapPost("/api/visual-test/images", async (
                    HttpRequest request,
                    IOperaCloudClient operaCloudClient,
                    IOptions<OperaCloudOptions> operaOptions,
                    CancellationToken cancellationToken) =>
            {
                if (!request.HasFormContentType)
                    return Results.BadRequest(new { message = "Se esperaba un formulario con una imagen." });

                var form = await request.ReadFormAsync(cancellationToken);
                var resort = form["resort"].ToString();
                var checkNumber = form["checkNumber"].ToString();
                var image = form.Files.GetFile("image");

                if (string.IsNullOrWhiteSpace(resort)) resort = operaOptions.Value.DefaultResort;
                if (string.IsNullOrWhiteSpace(checkNumber))
                    return Results.BadRequest(new { message = "Indica el nombre o número del cheque." });
                if (image is null)
                    return Results.BadRequest(new { message = "Selecciona una imagen." });
                if (image.Length is 0 or > 10 * 1024 * 1024)
                    return Results.BadRequest(new { message = "La imagen debe pesar entre 1 byte y 10 MB." });
                if (image.ContentType is not ("image/png" or "image/jpeg" or "image/webp"))
                    return Results.BadRequest(new { message = "El archivo debe ser PNG, JPEG o WebP." });

                try
                {
                    
                    await using var stream = image.OpenReadStream();
                    using var memory = new MemoryStream();
                    await stream.CopyToAsync(memory, cancellationToken);
                    if (!FileSystemUploadedTicketImageStore.HasExpectedSignature(
                            memory.GetBuffer().AsSpan(0, (int)memory.Length), image.ContentType))
                        return Results.BadRequest(new { message = "El contenido del archivo no coincide con el formato indicado." });
                    var uploadRequest = new DocumentUploadRequest(
                        Resort: resort.Trim(),
                        ReservationId: string.Empty,
                        CheckNumber: checkNumber.Trim(),
                        FileName: image.FileName,
                        MimeType: image.ContentType,
                        Content: memory.ToArray(),
                        CorrelationId: Guid.NewGuid());
                    var result = await operaCloudClient.UploadDocumentAsync(
                        uploadRequest,
                        cancellationToken);
                    if (!result.Succeeded)
                    {
                        return Results.BadRequest(new
                        {
                            message = result.Error
                        });
                    }


                    return Results.Ok(new
                    {
                        message = "Imagen enviada a Opera.",
                        resort = resort.Trim(),
                        checkNumber = checkNumber.Trim(),
                        documentId = result.DocumentId
                    });

                }
                catch (Exception exception) when (exception is ArgumentException or InvalidDataException or
                    InvalidOperationException or IOException or OperaCloudException)
                {
                    return Results.BadRequest(new { message = exception.Message });
                }
            })
            .DisableAntiforgery();

        return endpoints;
    }
}
