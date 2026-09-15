using System.Collections.Concurrent;

namespace TicketsVidanta.Shared.Auditing;

public sealed class InMemoryAuditService(ILogger<InMemoryAuditService> logger) : IAuditService
{
    private readonly ConcurrentQueue<TicketProcessingAudit> _audits = new();
    public IReadOnlyCollection<TicketProcessingAudit> Entries => _audits.ToArray();

    public Task WriteAsync(TicketProcessingAudit audit, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _audits.Enqueue(audit);
        logger.LogInformation("Audit recorded. CorrelationId={CorrelationId}, Status={Status}", audit.CorrelationId, audit.Status);
        return Task.CompletedTask;
    }
}
