namespace TicketsVidanta.Shared.Exceptions;

public sealed class OperaCloudException(string message, Exception? innerException = null) : Exception(message, innerException);
