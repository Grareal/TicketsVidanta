namespace TicketsVidanta.Shared.Exceptions;

public sealed class TicketGenerationException(string message, Exception? innerException = null) : Exception(message, innerException);
