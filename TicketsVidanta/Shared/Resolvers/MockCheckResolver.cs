using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Shared.Resolvers;

/// <summary>Resolver simulado que acepta exclusivamente el sistema lógico MOCK.</summary>
public sealed class MockCheckResolver : ICheckResolver
{
    public bool CanHandle(CheckProcessingContext context) =>
        string.Equals(context.SourceSystem, "MOCK", StringComparison.OrdinalIgnoreCase);

    public Task<CheckDetail?> ResolveAsync(CheckProcessingContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CheckDetail detail = new(
            [new CheckItem("Mock item - development only", 1, 0)],
            Total: 0,
            Currency: null);
        return Task.FromResult<CheckDetail?>(detail);
    }
}
