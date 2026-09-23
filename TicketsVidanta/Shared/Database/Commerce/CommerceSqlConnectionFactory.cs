using Microsoft.Data.SqlClient;

namespace TicketsVidanta.Shared.Database.Commerce;

public sealed class CommerceSqlConnectionFactory(
    ICommerceConnectionCatalog catalog,
    IConfiguration configuration) : ICommerceSqlConnectionFactory
{
    public SqlConnection Create(string sourceSystem, string resort)
    {
        if (!catalog.TryGet(sourceSystem, resort, out var descriptor) || descriptor is null)
            throw new InvalidOperationException(
                $"No existe una conexión de comercio para '{sourceSystem}' en el resort '{resort}'.");

        if (!string.Equals(descriptor.Provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"El proveedor '{descriptor.Provider}' no está soportado.");

        var connectionString = configuration.GetConnectionString(descriptor.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"La cadena '{descriptor.ConnectionStringName}' no está configurada.");

        return new SqlConnection(connectionString);
    }
}
