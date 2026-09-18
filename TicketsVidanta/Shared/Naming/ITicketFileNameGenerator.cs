using TicketsVidanta.Features.Tickets.ProcesarCheque;

namespace TicketsVidanta.Shared.Naming;

public interface ITicketFileNameGenerator
{
    string Generate(CheckProcessingContext context, DateTimeOffset timestamp, string mimeType = "image/png");
}
