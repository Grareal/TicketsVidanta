using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;

namespace TicketsVidanta.Shared.Database.Commerce;

public sealed class OptionsCommerceConnectionCatalog(IOptions<CommerceDatabaseOptions> options)
    : ICommerceConnectionCatalog
{
    public bool TryGet(string sourceSystem, string resort, out CommerceConnectionDescriptor? descriptor)
    {
        if (options.Value.Connections.TryGetValue($"{sourceSystem}:{resort}", out var definition))
        {
            descriptor = new CommerceConnectionDescriptor(definition.Provider, definition.ConnectionStringName);
            return true;
        }

        descriptor = null;
        return false;
    }
}
