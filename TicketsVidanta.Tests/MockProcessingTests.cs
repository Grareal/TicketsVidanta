using Microsoft.Extensions.Logging.Abstractions;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Auditing;
using TicketsVidanta.Shared.Naming;
using TicketsVidanta.Shared.OperaCloud;
using TicketsVidanta.Shared.Processing;
using TicketsVidanta.Shared.Resolvers;
using TicketsVidanta.Shared.TicketGeneration;

namespace TicketsVidanta.Tests;

public sealed class MockProcessingTests
{
    [Fact]
    public async Task ProcessAsync_CompletesFullMockPipeline()
    {
        var handler = CreateHandler([new MockCheckResolver()]);
        var request = new Request("TEST", "123456", "CHK-001", "100", "MOCK");

        var result = await handler.ProcessAsync(request, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.FileName);
        Assert.StartsWith("MOCK-", result.OperaDocumentId);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsFailed_WhenResolverDoesNotExist()
    {
        var handler = CreateHandler([]);
        var request = new Request("TEST", "123456", "CHK-001", "100", "UNKNOWN");

        var result = await handler.ProcessAsync(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("No existe resolver", result.Message);
    }

    private static Handler CreateHandler(IEnumerable<ICheckResolver> resolvers)
    {
        var selector = new CheckResolverSelector(resolvers, NullLogger<CheckResolverSelector>.Instance);
        var audit = new InMemoryAuditService(NullLogger<InMemoryAuditService>.Instance);
        return new Handler(
            new InMemoryProcessingRegistry(),
            selector,
            new MockOperaCloudClient(NullLogger<MockOperaCloudClient>.Instance),
            new MockTicketRenderer(),
            new TicketFileNameGenerator(),
            audit,
            NullLogger<Handler>.Instance);
    }
}
