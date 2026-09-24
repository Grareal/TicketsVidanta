using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using TicketsVidanta.Features.DatabaseConfiguration;

namespace TicketsVidanta.Tests;

public sealed class ConnectionSecretProtectorTests
{
    [Fact]
    public void Protect_DoesNotPersistPlaintext_AndCanRoundTrip()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("TicketsVidanta.Tests");
        using var provider = services.BuildServiceProvider();
        var protector = new ConnectionSecretProtector(provider.GetRequiredService<IDataProtectionProvider>());
        const string connectionString = "Server=db;Database=tickets;User ID=reader;Password=secret-value";

        var protectedValue = protector.Protect(connectionString);

        Assert.DoesNotContain("secret-value", protectedValue, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", protectedValue, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(connectionString, protector.Unprotect(protectedValue));
    }
}
