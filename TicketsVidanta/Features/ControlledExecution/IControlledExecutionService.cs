namespace TicketsVidanta.Features.ControlledExecution;

public interface IControlledExecutionService
{
    Task<ControlledBatchResult> ExecuteAsync(ControlledBatchRequest request, CancellationToken cancellationToken);
    Task<ReprocessResult> ReprocessAsync(ReprocessRequest request, CancellationToken cancellationToken);
}

