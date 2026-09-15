namespace TicketsVidanta.Shared.Database.Master;

/// <summary>Marcador explícito para la futura implementación SQL.</summary>
public sealed class SqlMasterTransactionRepository : IMasterTransactionRepository
{
    private static NotImplementedException DiscoveryRequired() => new(
        "El repositorio SQL requiere confirmar el contrato de la tabla maestra.");

    public Task<IReadOnlyList<MasterTransaction>> GetPendingTransactionsAsync(int batchSize, CancellationToken cancellationToken)
    {
        // TODO [DATABASE-DISCOVERY]:
        // Pendiente confirmar la tabla maestra.
        //
        // QUÉ DEBE COLOCARSE AQUÍ:
        // 1. nombre de servidor y base mediante configuración segura;
        // 2. esquema y tabla o stored procedure;
        // 3. columnas y llave primaria;
        // 4. significado/campo de ReservationId o RESV_NAME_ID;
        // 5. CheckNumber, Reference, Resort y sistema origen;
        // 6. criterio y orden para registros pendientes;
        // 7. estrategia de bloqueo/claim y aislamiento para consumidores concurrentes.
        //
        // EJEMPLO ESPERADO:
        // Consulta parametrizada que reclame como máximo batchSize registros de forma atómica.
        //
        // NO IMPLEMENTAR HASTA:
        // DBA y negocio confirmen el contrato completo y se disponga de un ambiente no productivo.
        throw DiscoveryRequired();
    }

    public Task MarkAsProcessedAsync(string transactionId, Guid correlationId, CancellationToken cancellationToken)
    {
        // TODO [DATABASE-DISCOVERY]: confirmar operación, columnas de estado, auditoría,
        // concurrencia y stored procedure para marcar una transacción como completada.
        throw DiscoveryRequired();
    }

    public Task MarkAsFailedAsync(string transactionId, Guid correlationId, string errorMessage, CancellationToken cancellationToken)
    {
        // TODO [DATABASE-DISCOVERY]: confirmar operación, estados, longitud/privacidad
        // del error, conteo de intentos y mecanismo para marcar una transacción fallida.
        throw DiscoveryRequired();
    }
}
