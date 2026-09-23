using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Database.Master;

namespace TicketsVidanta.Shared.Processing;

public sealed class FinancialTransactionIngestionService(
    IFinancialTransactionReader reader,
    IMasterTransactionInbox inbox,
    IOptions<FinancialTransactionSourceOptions> options,
    ILogger<FinancialTransactionIngestionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds));
        do
        {
            try
            {
                var candidates = await reader.ReadAsync(stoppingToken);
                var inserted = await inbox.AddMissingAsync(candidates, stoppingToken);
                logger.LogInformation("Financial transactions ingested. Read={Read}, Inserted={Inserted}", candidates.Count, inserted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Financial transaction ingestion failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
