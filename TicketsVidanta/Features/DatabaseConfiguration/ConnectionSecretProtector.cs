using Microsoft.AspNetCore.DataProtection;

namespace TicketsVidanta.Features.DatabaseConfiguration;

public sealed class ConnectionSecretProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector(
        "TicketsVidanta.DatabaseConnections.v1");

    public string Protect(string value) => _protector.Protect(value);
    public string Unprotect(string value) => _protector.Unprotect(value);
}
