namespace TicketsVidanta.Features.VisualTest;

public static class Endpoint
{
    public static IEndpointRouteBuilder MapVisualTestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/visual-test", () => Results.Redirect("/visual-test/index.html"));

        endpoints.MapPost("/api/visual-test/images", async (
                HttpRequest request,
                IUploadedTicketImageStore imageStore,
                CancellationToken cancellationToken) =>
            {
                if (!request.HasFormContentType)
                    return Results.BadRequest(new { message = "Se esperaba un formulario con una imagen." });

                var form = await request.ReadFormAsync(cancellationToken);
                var checkNumber = form["checkNumber"].ToString();
                var image = form.Files.GetFile("image");

                if (string.IsNullOrWhiteSpace(checkNumber))
                    return Results.BadRequest(new { message = "Indica el nombre o número del cheque." });
                if (image is null)
                    return Results.BadRequest(new { message = "Selecciona una imagen." });
                if (image.Length is 0 or > 10 * 1024 * 1024)
                    return Results.BadRequest(new { message = "La imagen debe pesar entre 1 byte y 10 MB." });

                try
                {
                    await using var stream = image.OpenReadStream();
                    await imageStore.SaveAsync(checkNumber, stream, image.ContentType, cancellationToken);
                    return Results.Ok(new
                    {
                        message = "La imagen quedó asociada al cheque.",
                        checkNumber = checkNumber.Trim(),
                        fileName = image.FileName
                    });
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidDataException or IOException)
                {
                    return Results.BadRequest(new { message = exception.Message });
                }
            })
            .DisableAntiforgery();

        return endpoints;
    }
}
