namespace TicketsVidanta.Shared.Auditing;

public interface IAuditService
{
    Task WriteAsync(TicketProcessingAudit audit, CancellationToken cancellationToken);
}
