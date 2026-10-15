namespace TicketsVidanta.Features.SyncEngine;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapSyncEngineEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/admin/sync");

        api.MapGet("/catalog", (ISyncCatalogRepository repo, CancellationToken ct) => repo.GetAllEntriesAsync(ct));

        api.MapPost("/catalog", async (SaveSyncCatalogRequest request, ISyncCatalogRepository repo, CancellationToken ct) =>
        {
            var id = await repo.SaveEntryAsync(request, ct);
            return Results.Ok(new { id });
        });

        api.MapDelete("/catalog/{id:int}", async (int id, ISyncCatalogRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteEntryAsync(id, ct);
            return Results.NoContent();
        });

        api.MapPost("/run/{id:int}", async (int id, ISyncCatalogRepository repo, ITableSyncService syncService, CancellationToken ct) =>
        {
            var entry = await repo.GetEntryByIdAsync(id, ct);
            if (entry is null) return Results.NotFound(new { message = $"No se encontró la entrada de catálogo {id}" });

            var result = await syncService.SyncTableAsync(entry, ct);
            return Results.Ok(result);
        });

      


        api.MapGet("/logs", (ISyncCatalogRepository repo, int? top, CancellationToken ct) =>
            repo.GetRecentBatchLogsAsync(top ?? 50, ct));

        return endpoints;
    }
}

