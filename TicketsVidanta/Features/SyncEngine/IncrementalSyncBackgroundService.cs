using Microsoft.Extensions.Options;

namespace TicketsVidanta.Features.SyncEngine;

public sealed class IncrementalSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<SyncEngineOptions> options,
    ILogger<IncrementalSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Incremental Sync Engine is disabled by configuration (SyncEngine:Enabled=false).");
            return;
        }

        logger.LogInformation("Incremental Sync Engine started. DefaultInterval={Interval}s", options.Value.DefaultIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(10, options.Value.DefaultIntervalSeconds)));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var catalog = scope.ServiceProvider.GetRequiredService<ISyncCatalogRepository>();
                var syncService = scope.ServiceProvider.GetRequiredService<ITableSyncService>();
                var activeEntries = await catalog.GetActiveEntriesAsync(stoppingToken);

                if (activeEntries.Count == 0)
                {
                    logger.LogDebug("No hay tablas activas en etl.SyncTableCatalog para sincronizar.");
                    continue;
                }

                foreach (var entry in activeEntries)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    try
                    {
                        using var perTableCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        perTableCts.CancelAfter(TimeSpan.FromSeconds(60));

                        var result = await syncService.SyncTableAsync(entry, perTableCts.Token);
                        if (!result.Succeeded)
                            logger.LogWarning("Sincronización no completada para {RemoteTable} ({Resort}): {Error}. Continuando con la siguiente tabla del catálogo.", 
                                entry.RemoteTable, entry.Resort, result.ErrorMessage);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (OperationCanceledException)
                    {
                        logger.LogWarning("Tiempo de espera agotado (60s) al sincronizar {RemoteTable} ({Resort}). Continuando con la siguiente tabla del catálogo.", 
                            entry.RemoteTable, entry.Resort);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error en ciclo de sincronización para {RemoteTable} ({Resort}). Continuando con la siguiente tabla del catálogo.", 
                            entry.RemoteTable, entry.Resort);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inesperado en IncrementalSyncBackgroundService.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

