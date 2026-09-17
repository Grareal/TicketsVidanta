using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Shared.Database;

public sealed class SqlConnectionFactory(
    IConfiguration configuration,
    IOptions<DatabaseOptions> options) : ISqlConnectionFactory
{
    public SqlConnection CreateConnection()
    {
        var connectionString = configuration.GetConnectionString(options.Value.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"No se configuró ConnectionStrings:{options.Value.ConnectionStringName}.");

        return new SqlConnection(connectionString);
    }
}
