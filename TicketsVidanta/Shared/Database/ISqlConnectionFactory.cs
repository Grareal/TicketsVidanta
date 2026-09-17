using Microsoft.Data.SqlClient;

namespace TicketsVidanta.Shared.Database;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}
