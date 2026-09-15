namespace TicketsVidanta.Shared.TicketGeneration;

public sealed record GeneratedTicket(ReadOnlyMemory<byte> Content, string MimeType);
