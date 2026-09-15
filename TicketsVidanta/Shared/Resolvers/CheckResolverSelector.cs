using TicketsVidanta.Features.Tickets.ProcesarCheque;

namespace TicketsVidanta.Shared.Resolvers;

public sealed class CheckResolverSelector(
    IEnumerable<ICheckResolver> resolvers,
    ILogger<CheckResolverSelector> logger) : ICheckResolverSelector
{
    public ResolverSelectionResult Select(CheckProcessingContext context)
    {
        var matches = resolvers.Where(x => x.CanHandle(context)).Take(2).ToArray();
        if (matches.Length == 0)
        {
            var message = $"No existe resolver para SourceSystem '{context.SourceSystem}'.";
            logger.LogWarning(
                "Resolver not found. CorrelationId={CorrelationId}, ReservationId={ReservationId}, CheckNumber={CheckNumber}, SourceSystem={SourceSystem}",
                context.CorrelationId, context.ReservationId, context.CheckNumber, context.SourceSystem);
            return ResolverSelectionResult.NotFound(message);
        }

        if (matches.Length > 1)
        {
            var message = $"Más de un resolver acepta SourceSystem '{context.SourceSystem}'.";
            logger.LogWarning("Ambiguous resolver selection. CorrelationId={CorrelationId}, SourceSystem={SourceSystem}",
                context.CorrelationId, context.SourceSystem);
            return ResolverSelectionResult.NotFound(message);
        }

        logger.LogInformation("Resolver selected. CorrelationId={CorrelationId}, Resolver={Resolver}",
            context.CorrelationId, matches[0].GetType().Name);
        return ResolverSelectionResult.Success(matches[0]);
    }
}
