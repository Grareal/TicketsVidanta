using Microsoft.Data.SqlClient;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Routing;

namespace TicketsVidanta.Features.DatabaseConfiguration;

internal interface IRuntimeConfigurationCache
{
    RuntimeTicketProfile? FindProfile(string sourceSystem, string resort);
    string? Resolve(string? tcGroup, string? trxCode);
    IReadOnlyList<TransactionRoute> GetRoutes();
    Task ReloadAsync(CancellationToken cancellationToken);
}

internal sealed class RuntimeConfigurationCache(
    IConfigurationRepository repository,
    ILogger<RuntimeConfigurationCache> logger) : IRuntimeConfigurationCache, IHostedService
{
    private RuntimeConfigurationSnapshot _snapshot = RuntimeConfigurationSnapshot.Empty;

    public RuntimeTicketProfile? FindProfile(string sourceSystem, string resort)
    {
        var snapshot = Volatile.Read(ref _snapshot);
        return snapshot.Profiles.FirstOrDefault(x =>
                   string.Equals(x.Profile.SourceSystem, sourceSystem, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.Profile.Resort, resort, StringComparison.OrdinalIgnoreCase))
               ?? snapshot.Profiles.FirstOrDefault(x =>
                   string.Equals(x.Profile.SourceSystem, sourceSystem, StringComparison.OrdinalIgnoreCase) &&
                   string.IsNullOrWhiteSpace(x.Profile.Resort));
    }

    public string? Resolve(string? tcGroup, string? trxCode)
    {
        if (string.IsNullOrWhiteSpace(tcGroup) || string.IsNullOrWhiteSpace(trxCode)) return null;
        var matches = Volatile.Read(ref _snapshot).Routes.Where(x =>
            string.Equals(x.TcGroup.Trim(), tcGroup.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.TrxCode.Trim(), trxCode.Trim(), StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        return matches.Length == 1 ? matches[0].SourceSystem : null;
    }

    public IReadOnlyList<TransactionRoute> GetRoutes() => Volatile.Read(ref _snapshot).Routes;

    public async Task ReloadAsync(CancellationToken cancellationToken)
    {
        var snapshot = await repository.GetRuntimeSnapshotAsync(cancellationToken);
        Volatile.Write(ref _snapshot, snapshot);
        logger.LogInformation("Configuración dinámica recargada. Routes={Routes}, Profiles={Profiles}", snapshot.Routes.Count, snapshot.Profiles.Count);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try { await ReloadAsync(cancellationToken); }
        catch (SqlException exception) { logger.LogWarning(exception, "No se pudo cargar configuración dinámica. Ejecute el script 006."); }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class ConfigurableTransactionSourceRouter(
    IRuntimeConfigurationCache cache,
    OptionsTransactionSourceRouter optionsRouter,
    Microsoft.Extensions.Options.IOptions<TransactionRoutingOptions> options) : ITransactionSourceRouter, ITransactionRouteCatalog
{
    public string? Resolve(string? tcGroup, string? trxCode) =>
        cache.Resolve(tcGroup, trxCode) ?? optionsRouter.Resolve(tcGroup, trxCode);

    public IReadOnlyList<TransactionRoutingRule> GetRules()
    {
        var dynamicRules = cache.GetRoutes().Select(x => new TransactionRoutingRule
        {
            TcGroup = x.TcGroup,
            TrxCode = x.TrxCode,
            SourceSystem = x.SourceSystem
        });
        return dynamicRules.Concat(options.Value.Rules)
            .GroupBy(x => $"{x.TcGroup.Trim()}\u001f{x.TrxCode.Trim()}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First()).ToArray();
    }
}
