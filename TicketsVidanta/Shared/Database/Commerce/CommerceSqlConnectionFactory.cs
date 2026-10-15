using Microsoft.Data.SqlClient;
using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Shared.Database.Commerce;

public sealed class CommerceSqlConnectionFactory(
    ICommerceConnectionCatalog catalog,
    IConfiguration configuration,
    ISqlConnectionFactory localConnectionFactory,
    Microsoft.Extensions.Options.IOptions<CommerceDatabaseOptions> options) : ICommerceSqlConnectionFactory
{
    public SqlConnection Create(string sourceSystem, string resort)
    {
        var key = $"{sourceSystem}:{resort}";
        var useLocal = options.Value.UseLocalIntegration;
        if (options.Value.LocalIntegrationOverrides.TryGetValue(key, out var overrideVal))
            useLocal = overrideVal;

        if (catalog.TryGet(sourceSystem, resort, out var descriptor) && descriptor is not null)
        {
            if (options.Value.Connections.TryGetValue(key, out var def) && def.UseLocalIntegration)
                useLocal = true;

            if (string.Equals(descriptor.ConnectionStringName, "TicketsVidanta", StringComparison.OrdinalIgnoreCase))
                useLocal = true;
        }

        if (useLocal)
        {
            return localConnectionFactory.CreateConnection();
        }

        if (descriptor is null)
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
