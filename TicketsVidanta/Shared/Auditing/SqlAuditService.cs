namespace TicketsVidanta.Shared.Auditing;

public sealed class SqlAuditService : IAuditService
{
    public Task WriteAsync(TicketProcessingAudit audit, CancellationToken cancellationToken)
    {
        // TODO [DATABASE-DISCOVERY]:
        // Pendiente definir persistencia corporativa de auditoría.
        //
        // QUÉ DEBE COLOCARSE AQUÍ:
        // Proveedor, connection string por secret provider, esquema, tabla, columnas,
        // índices, retención, permisos y operación idempotente de escritura.
        //
        // EJEMPLO ESPERADO:
        // Implementación parametrizada que persista TicketProcessingAudit sin datos sensibles innecesarios.
        //
        // NO IMPLEMENTAR HASTA:
        // Aprobar el modelo físico y la política de conservación con DBA y seguridad.
        throw new NotImplementedException("La auditoría SQL requiere completar el discovery de base de datos.");
    }
}
