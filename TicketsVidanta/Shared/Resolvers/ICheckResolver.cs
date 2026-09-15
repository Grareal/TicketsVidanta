using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.Resolvers;

public interface ICheckResolver
{
    bool CanHandle(CheckProcessingContext context);
    Task<CheckDetail?> ResolveAsync(CheckProcessingContext context, CancellationToken cancellationToken);
}
