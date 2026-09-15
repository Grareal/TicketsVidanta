namespace TicketsVidanta.Shared.Models;

/// <summary>Etapa actual del procesamiento de un cheque.</summary>
public enum ProcessingStatus
{
    Pending,
    Resolving,
    Resolved,
    GeneratingDocument,
    Uploading,
    Completed,
    Failed
}
