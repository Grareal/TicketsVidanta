# Arquitectura

## Contexto

TicketsVidanta orquesta la obtención, normalización, representación y entrega de cheques. Los contratos corporativos aún no están confirmados, por lo que los bordes del sistema se expresan como interfaces y se ejercitan con mocks explícitos.

## Componentes

`ProcesarCheque.Handler` coordina el caso de uso. `IProcessingRegistry` protege la llave lógica; `ICheckResolverSelector` exige una coincidencia única; `IOperaCloudClient` valida reserva y carga; `ITicketRenderer` produce bytes; `ITicketFileNameGenerator` aísla nomenclatura; `IAuditService` registra el resultado. El worker reutiliza el mismo procesador, evitando dos pipelines divergentes.

Las dependencias apuntan desde el slice hacia contratos pequeños de `Shared`. No hay capas de dominio/aplicación/infraestructura artificiales ni bus interno.

## Concurrencia y errores

`TryRegisterStartedAsync` es atómico en memoria. En producción deberá equivaler a una restricción única o claim transaccional persistente. Un resolver ausente/ambiguo, detalle ausente, reserva ausente o upload rechazado produce respuesta controlada y auditoría `Failed`. Las excepciones inesperadas se registran sin secretos y se convierten en fallo controlado dentro del pipeline; el middleware ProblemDetails cubre errores fuera del caso de uso.

El worker usa un semáforo para evitar solapamiento dentro de una instancia y continúa después de fallar una transacción. La coordinación entre instancias requiere diseño de base de datos.

## Salud y operación

`/health` solo indica que el proceso ASP.NET Core responde. Las comprobaciones de tabla maestra, comercios, Opera Cloud y secret provider se agregarán después de definir dependencias, con distinción entre liveness y readiness.
