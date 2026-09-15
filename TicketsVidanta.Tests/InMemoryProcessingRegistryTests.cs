using TicketsVidanta.Shared.Processing;

namespace TicketsVidanta.Tests;

public sealed class InMemoryProcessingRegistryTests
{
    [Fact]
    public async Task TryRegisterStarted_AllowsOnlyOneConcurrentRegistration()
    {
        var registry = new InMemoryProcessingRegistry();
        var key = ProcessingKey.From(TestFactory.Context());

        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => registry.TryRegisterStartedAsync(key, Guid.NewGuid(), CancellationToken.None)));

        Assert.Single(results, x => x);
    }

    [Fact]
    public async Task CompletedRecord_IsReportedAsAlreadyProcessed()
    {
        var registry = new InMemoryProcessingRegistry();
        var context = TestFactory.Context();
        var key = ProcessingKey.From(context);
        await registry.TryRegisterStartedAsync(key, context.CorrelationId, CancellationToken.None);
        await registry.RegisterCompletedAsync(key, context.CorrelationId, CancellationToken.None);

        Assert.True(await registry.HasAlreadyBeenProcessedAsync(key, CancellationToken.None));
    }
}
