using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TicketsVidanta.Features.Tickets.ProcesarCheque;
using TicketsVidanta.Shared.Auditing;
using TicketsVidanta.Shared.Configuration;
using TicketsVidanta.Shared.Naming;
using TicketsVidanta.Shared.OperaCloud;
using TicketsVidanta.Shared.Processing;
using TicketsVidanta.Shared.Resolvers;
using TicketsVidanta.Shared.Routing;
using TicketsVidanta.Shared.TicketGeneration;

namespace TicketsVidanta.Tests;

public sealed class ControlledExecutionTests
{
    [Fact]
    public async Task ProcessAsync_InSimulationMode_ReturnsPayloadWithoutPersistingOrUploading()
    {
        var registry = new InMemoryProcessingRegistry();
        var handler = CreateHandler([new MockCheckResolver()], registry);
        var request = new Request("TEST", "123456", "CHK-SIM-001", "100", "MOCK", Mode: TicketExecutionMode.Simulation);

        var result = await handler.ProcessAsync(request, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.SimulationPayload);
        Assert.Contains("Simulación completada", result.Message);

        // Verificar que NO se bloqueó la llave en el registro
        var context = TestFactory.Context("MOCK", "CHK-SIM-001");
        var alreadyProcessed = await registry.HasAlreadyBeenProcessedAsync(ProcessingKey.From(context), CancellationToken.None);
        Assert.False(alreadyProcessed);
    }

    [Fact]
    public async Task ProcessAsync_InDryRunMode_ArchivesLocallyWithoutOperaUpload()
    {
        var registry = new InMemoryProcessingRegistry();
        var handler = CreateHandler([new MockCheckResolver()], registry);
        var request = new Request("TEST", "123456", "CHK-DRY-001", "100", "MOCK", Mode: TicketExecutionMode.DryRun);

        var result = await handler.ProcessAsync(request, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.FileName);
        Assert.Null(result.OperaDocumentId);
        Assert.Contains("DryRun", result.Message);
    }

    private static Handler CreateHandler(IEnumerable<ICheckResolver> resolvers, IProcessingRegistry registry)
    {
        var selector = new CheckResolverSelector(resolvers, NullLogger<CheckResolverSelector>.Instance);
        var audit = new InMemoryAuditService(NullLogger<InMemoryAuditService>.Instance);
        return new Handler(
            registry,
            selector,
            new MockOperaCloudClient(NullLogger<MockOperaCloudClient>.Instance),
            new MockTicketRenderer(),
            new NullGeneratedTicketStore(),
            new TicketFileNameGenerator(),
            audit,
            new OptionsTransactionSourceRouter(Options.Create(new TransactionRoutingOptions())),
            Options.Create(new OperaCloudOptions { UseMock = true, EnableUpload = false }),
            NullLogger<Handler>.Instance);
    }
}

