using Microsoft.Extensions.Logging.Abstractions;
using TicketsVidanta.Shared.Resolvers;

namespace TicketsVidanta.Tests;

public sealed class CheckResolverSelectorTests
{
    [Fact]
    public void Select_ReturnsMockResolver_WhenSourceIsMock()
    {
        ICheckResolver[] resolvers = [new MockCheckResolver()];
        var selector = new CheckResolverSelector(resolvers, NullLogger<CheckResolverSelector>.Instance);

        var result = selector.Select(TestFactory.Context());

        Assert.True(result.IsSuccess);
        Assert.IsType<MockCheckResolver>(result.Resolver);
    }

    [Fact]
    public void Select_ReturnsControlledFailure_WhenResolverDoesNotExist()
    {
        var selector = new CheckResolverSelector([], NullLogger<CheckResolverSelector>.Instance);

        var result = selector.Select(TestFactory.Context(sourceSystem: "UNKNOWN"));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Resolver);
        Assert.Contains("No existe resolver", result.Error);
    }
}
