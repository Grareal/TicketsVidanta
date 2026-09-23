using Microsoft.Extensions.Options;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Database.Master;

namespace TicketsVidanta.Shared.Processing;

public sealed class TicketProcessingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ProcessingOptions> options,
    ILogger<TicketProcessingBackgroundService> logger) : BackgroundService
{
    private readonly SemaphoreSlim _singleExecution = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.EnableBackgroundProcessing)
        {
            logger.LogInformation("Background ticket processing is disabled by configuration.");
            return;
        }

        logger.LogInformation("Background ticket processing started.");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds));
        do
        {
            await ProcessBatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        if (!await _singleExecution.WaitAsync(0, cancellationToken)) return;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMasterTransactionRepository>();
            var processor = scope.ServiceProvider.GetRequiredService<ITicketProcessor>();
            var pending = await repository.GetPendingTransactionsAsync(options.Value.BatchSize, cancellationToken);

            foreach (var transaction in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var result = await processor.ProcessAsync(new Request(
                        transaction.Resort, transaction.ReservationId, transaction.CheckNumber,
                        transaction.Room, transaction.Reference, transaction.SourceSystem,
                        transaction.TcGroup, transaction.TrxCode), cancellationToken);

                    if (result.Succeeded || result.AlreadyProcessed)
                        await repository.MarkAsProcessedAsync(transaction.Id, result.CorrelationId, cancellationToken);
                    else
                        await repository.MarkAsFailedAsync(transaction.Id, result.CorrelationId, result.Message, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Unexpected transaction failure. TransactionId={TransactionId}", transaction.Id);
                }
            }
        }
        finally
        {
            _singleExecution.Release();
        }
    }
}
