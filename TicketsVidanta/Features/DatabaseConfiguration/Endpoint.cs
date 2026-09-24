using Microsoft.Data.SqlClient;

namespace TicketsVidanta.Features.DatabaseConfiguration;

public static class Endpoint
{
    public static IEndpointRouteBuilder MapDatabaseConfigurationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/db-config", () => Results.Redirect("/db-config/index.html"));
        var api = endpoints.MapGroup("/api/admin/db-config");

        api.MapGet("/connections", (IConfigurationRepository repository, CancellationToken ct) => repository.GetConnectionsAsync(ct));
        api.MapPost("/connections", async (SaveDatabaseConnectionRequest request, IConfigurationRepository repository,
            IRuntimeConfigurationCache cache, CancellationToken ct) =>
            await Execute(async () =>
            {
                var id = await repository.SaveConnectionAsync(request, ct); await cache.ReloadAsync(ct); return Results.Ok(new { id });
            }));
        api.MapDelete("/connections/{id:guid}", async (Guid id, IConfigurationRepository repository,
            IRuntimeConfigurationCache cache, CancellationToken ct) => await Execute(async () =>
            {
                await repository.DeleteConnectionAsync(id, ct); await cache.ReloadAsync(ct); return Results.NoContent();
            }));
        api.MapPost("/connections/test", async (TestDatabaseConnectionRequest request, DatabaseMetadataService metadata, CancellationToken ct) =>
            await Execute(async () => Results.Ok(new { message = await metadata.TestAsync(request.Id, request.ConnectionString, ct) })));
        api.MapGet("/connections/{id:guid}/schema", async (Guid id, DatabaseMetadataService metadata, CancellationToken ct) =>
            await Execute(async () => Results.Ok(await metadata.GetSchemaAsync(id, ct))));

        api.MapGet("/profiles", (IConfigurationRepository repository, CancellationToken ct) => repository.GetProfilesAsync(ct));
        api.MapPost("/profiles", async (SaveTicketProfileRequest request, IConfigurationRepository repository,
            IRuntimeConfigurationCache cache, CancellationToken ct) => await Execute(async () =>
            {
                var id = await repository.SaveProfileAsync(request, ct); await cache.ReloadAsync(ct); return Results.Ok(new { id });
            }));
        api.MapDelete("/profiles/{id:guid}", async (Guid id, IConfigurationRepository repository,
            IRuntimeConfigurationCache cache, CancellationToken ct) => await Execute(async () =>
            {
                await repository.DeleteProfileAsync(id, ct); await cache.ReloadAsync(ct); return Results.NoContent();
            }));

        api.MapGet("/routes", (IConfigurationRepository repository, CancellationToken ct) => repository.GetRoutesAsync(ct));
        api.MapPost("/routes", async (SaveTransactionRouteRequest request, IConfigurationRepository repository,
            IRuntimeConfigurationCache cache, CancellationToken ct) => await Execute(async () =>
            {
                var id = await repository.SaveRouteAsync(request, ct); await cache.ReloadAsync(ct); return Results.Ok(new { id });
            }));
        api.MapDelete("/routes/{id:guid}", async (Guid id, IConfigurationRepository repository,
            IRuntimeConfigurationCache cache, CancellationToken ct) => await Execute(async () =>
            {
                await repository.DeleteRouteAsync(id, ct); await cache.ReloadAsync(ct); return Results.NoContent();
            }));

        return endpoints;
    }

    private static async Task<IResult> Execute(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ArgumentException exception) { return Results.BadRequest(new { message = exception.Message }); }
        catch (KeyNotFoundException exception) { return Results.NotFound(new { message = exception.Message }); }
        catch (SqlException exception) { return Results.BadRequest(new { message = "SQL Server rechazó la operación.", detail = exception.Message }); }
        catch (System.Security.Cryptography.CryptographicException) { return Results.Problem("No fue posible descifrar la conexión. Verifica el llavero de Data Protection."); }
    }
}
