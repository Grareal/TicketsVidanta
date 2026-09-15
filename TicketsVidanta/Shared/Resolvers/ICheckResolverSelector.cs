using TicketsVidanta.Features.Tickets.ProcesarCheque;

namespace TicketsVidanta.Shared.Resolvers;

public interface ICheckResolverSelector
{
    ResolverSelectionResult Select(CheckProcessingContext context);
}
