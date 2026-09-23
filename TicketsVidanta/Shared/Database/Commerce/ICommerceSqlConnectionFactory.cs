using Microsoft.Data.SqlClient;

namespace TicketsVidanta.Shared.Database.Commerce;

public interface ICommerceSqlConnectionFactory
{
    SqlConnection Create(string sourceSystem, string resort);
}
