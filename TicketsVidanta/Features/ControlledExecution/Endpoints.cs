namespace TicketsVidanta.Features.ControlledExecution;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapControlledExecutionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/admin/tickets");

        api.MapPost("/controlled-run", async (ControlledBatchRequest request, IControlledExecutionService service, CancellationToken ct) =>
        {
            var result = await service.ExecuteAsync(request, ct);
            return Results.Ok(result);
        });

        api.MapPost("/reprocess", async (ReprocessRequest request, IControlledExecutionService service, CancellationToken ct) =>
        {
            var result = await service.ReprocessAsync(request, ct);
            return Results.Ok(result);
        });

        return endpoints;
    }
}

