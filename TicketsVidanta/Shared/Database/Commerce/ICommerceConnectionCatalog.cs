namespace TicketsVidanta.Shared.Database.Commerce;

public interface ICommerceConnectionCatalog
{
    bool TryGet(string sourceSystem, string resort, out CommerceConnectionDescriptor? descriptor);
}

public sealed record CommerceConnectionDescriptor(string Provider, string ConnectionStringName);
