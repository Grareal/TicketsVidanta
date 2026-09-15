using TicketsVidanta.Shared.Processing;

namespace TicketsVidanta.Features.Tickets.ObtenerEstado;

public sealed class Handler(IProcessingRegistry registry)
{
    public async Task<Response?> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var record = await registry.GetAsync(request.CorrelationId, cancellationToken);
        return record is null
            ? null
            : new Response(record.CorrelationId, record.Status, record.StartedAt, record.CompletedAt, record.ErrorMessage);
    }
}
