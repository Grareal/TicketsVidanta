namespace TicketsVidanta.Features.Tickets.ProcesarCheque;

public static class Endpoint
{
    public static IEndpointRouteBuilder MapProcesarChequeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        var environment = endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment()) return endpoints;

        endpoints.MapPost("/api/tickets/process", async (
                Request request,
                Validator validator,
                ITicketProcessor processor,
                CancellationToken cancellationToken) =>
            {
                var errors = validator.Validate(request);
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                var response = await processor.ProcessAsync(request, cancellationToken);
                return response.Succeeded
                    ? Results.Ok(response)
                    : Results.Json(response, statusCode: response.AlreadyProcessed ? StatusCodes.Status409Conflict : StatusCodes.Status422UnprocessableEntity);
            })
            .WithName("ProcessMockCheck")
            .WithSummary("DEVELOPMENT ONLY: ejecuta el pipeline de cheque con infraestructura Mock")
            .Produces<Response>()
            .ProducesValidationProblem()
            .Produces<Response>(StatusCodes.Status409Conflict)
            .Produces<Response>(StatusCodes.Status422UnprocessableEntity);

        return endpoints;
    }
}
