namespace TicketsVidanta.Shared.OperaCloud;

public interface IOperaCloudTokenProvider
{
    Task<string> GetAccessTokenAsync(Guid requestId, CancellationToken cancellationToken);
}
