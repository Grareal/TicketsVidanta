using Microsoft.Extensions.Options;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Routing;

namespace TicketsVidanta.Tests;

public sealed class TransactionSourceRouterTests
{
    private readonly OptionsTransactionSourceRouter _router = new(Options.Create(
        new TransactionRoutingOptions
        {
            Rules =
            [
                new() { TcGroup = "6OTROS", TrxCode = "6", SourceSystem = "INSSIST_SPA" },
                new() { TcGroup = "6OTROS", TrxCode = "4", SourceSystem = "INSSIST_KIDSCLUB" }
            ]
        }));

    [Theory]
    [InlineData("6OTROS", "6", "INSSIST_SPA")]
    [InlineData(" 6otros ", "4", "INSSIST_KIDSCLUB")]
    public void Resolve_MapsKnownTransaction(string group, string code, string expected) =>
        Assert.Equal(expected, _router.Resolve(group, code));

    [Fact]
    public void Resolve_DoesNotGuessUnknownTransaction() =>
        Assert.Null(_router.Resolve("2", "99"));
}
