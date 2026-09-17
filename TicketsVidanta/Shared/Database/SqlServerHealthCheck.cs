using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TicketsVidanta.Shared.Database;

public sealed class SqlServerHealthCheck(ISqlConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("SQL Server disponible.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("SQL Server no disponible.", exception);
        }
    }
}
