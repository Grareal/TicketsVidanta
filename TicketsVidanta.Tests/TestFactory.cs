using TicketsVidanta.Features.Tickets.ProcesarCheque;

namespace TicketsVidanta.Tests;

internal static class TestFactory
{
    public static CheckProcessingContext Context(
        string sourceSystem = "MOCK",
        string checkNumber = "CHK-001") => new()
    {
        Resort = "TEST",
        ReservationId = "123456",
        CheckNumber = checkNumber,
        Room = "100",
        Reference = sourceSystem,
        SourceSystem = sourceSystem,
        CorrelationId = Guid.NewGuid()
    };
}
