using TicketsVidanta.Shared.Models;

namespace TicketsVidanta.Features.Tickets.ProcesarCheque;

/// <summary>Estado normalizado que acompaña al cheque durante todo el pipeline.</summary>
public sealed class CheckProcessingContext
{
    /// <summary>Identificador del resort informado por el origen.</summary>
    public required string Resort { get; init; }
    /// <summary>Llave canónica actual para localizar la reserva en Opera Cloud.</summary>
    public required string ReservationId { get; init; }
    /// <summary>Número de cheque informado por el sistema origen.</summary>
    public required string CheckNumber { get; init; }
    /// <summary>Habitación, si fue proporcionada por el origen.</summary>
    public string? Room { get; init; }
    /// <summary>Referencia opaca disponible para correlación futura.</summary>
    public string? Reference { get; init; }
    /// <summary>Partidas normalizadas del cheque; se llena después de resolverlo.</summary>
    public IReadOnlyList<CheckItem> Items { get; set; } = [];
    /// <summary>Identificador técnico único usado en logs, auditoría y llamadas externas.</summary>
    public required Guid CorrelationId { get; init; }
    /// <summary>Nombre lógico del sistema origen, sin asumir un catálogo definitivo.</summary>
    public required string SourceSystem { get; init; }
    /// <summary>Grupo contable recibido de FINANCIAL_TRANSACTIONS_P_DET_CLOUD.</summary>
    public string? TcGroup { get; init; }
    /// <summary>Código de transacción que distingue el tipo de ticket dentro del grupo.</summary>
    public string? TrxCode { get; init; }
    /// <summary>Etapa actual del procesamiento.</summary>
    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Pending;
    /// <summary>Instante UTC en que comenzó el intento.</summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Instante UTC en que terminó el intento, si ya terminó.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
