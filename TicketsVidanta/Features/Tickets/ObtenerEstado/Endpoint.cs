namespace TicketsVidanta.Features.Tickets.ObtenerEstado;

public static class Endpoint
{
    public static IEndpointRouteBuilder MapObtenerEstadoEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/tickets/{correlationId:guid}/status", async (
                Guid correlationId,
                Handler handler,
                CancellationToken cancellationToken) =>
            {
                var response = await handler.HandleAsync(new Request(correlationId), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(response);
            })
            .WithName("GetTicketProcessingStatus")
            .Produces<Response>()
            .Produces(StatusCodes.Status404NotFound);
        return endpoints;
    }
}
