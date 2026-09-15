# TicketsVidanta

Orquestador ETL interno para procesar cheques y, cuando las integraciones corporativas estén definidas, adjuntar su representación visual a una reserva de Opera Cloud. Este repositorio implementa lo conocido, encapsula lo que requerirá integración y conserva como pendiente todo dato que debe obtenerse durante Discovery.

## Estado actual

El proyecto productivo es una sola API ASP.NET Core sobre .NET 10, con nullable habilitado, inyección de dependencias nativa, logging estructurado, Options Pattern, `HttpClientFactory`, OpenAPI, `async/await` y `CancellationToken`. Existe un segundo proyecto únicamente para pruebas unitarias.

Hoy funciona de extremo a extremo un escenario de desarrollo:

```text
POST manual -> validar -> crear contexto/correlation ID -> idempotencia en memoria
-> seleccionar MockCheckResolver -> detalle simulado -> reserva simulada
-> PNG mínimo válido -> nombre sanitizado -> upload simulado -> auditoría en memoria
-> completed
```

No hay conexiones a infraestructura corporativa, secretos, endpoints OHIP, tablas ni reglas de negocio ficticias.

## Arquitectura

Se usa Vertical Slice porque cada caso de uso reúne endpoint, contrato, validación y handler en una carpeta. Esto reduce navegación y evita capas genéricas sin valor. Los componentes compartidos existen solo para capacidades usadas por más de un slice: resolvers, idempotencia, auditoría, acceso maestro, Opera Cloud, nombres y renderizado.

La solución contiene:

- `TicketsVidanta`: único proyecto productivo ASP.NET Core.
- `TicketsVidanta.Tests`: excepción permitida, solo pruebas.
- `docs`: decisiones y checklists para Discovery.

Las carpetas principales del proyecto son:

```text
TicketsVidanta/
  Features/Tickets/
    ProcesarCheque/       Endpoint, contratos, validador, contexto y pipeline
    ObtenerEstado/        Consulta por CorrelationId
  Shared/
    Auditing/             Auditoría en memoria y marcador SQL
    Configuration/        Options tipadas
    Database/Master/      Contrato maestro, mock y marcador SQL
    Database/Commerce/    Catálogo extensible de conexiones
    Exceptions/           Excepciones específicas disponibles
    Extensions/           Registro de dependencias
    Naming/               Nombre de archivo aislado
    OperaCloud/           Contrato, mock y cliente real pendiente
    Processing/           Idempotencia y BackgroundService
    Resolvers/            Resolver Pattern y guía de extensión
    TicketGeneration/     Contrato y PNG mock
```

## Flujo objetivo

La tabla maestra entregará pendientes; el worker los reclamará por lote; cada registro se normalizará como `CheckProcessingContext`; el selector localizará exactamente un resolver; el resolver consultará el detalle propio del comercio; se validará `ReservationId`; se generará el PNG; se cargará en Opera Cloud; se registrará auditoría y se actualizará el registro maestro.

`ReservationId` es la llave canónica actual para buscar la reserva. La idempotencia usa provisionalmente `Resort + ReservationId + CheckNumber + SourceSystem`; ambas decisiones deben validarse con negocio. `CorrelationId` es un `Guid` nuevo por intento y aparece en logs, auditoría y contratos externos.

## Componentes Mock

- `MockCheckResolver`: acepta únicamente `SourceSystem=MOCK`.
- `MockOperaCloudClient`: considera encontrada la reserva y devuelve un DocumentId con prefijo `MOCK-`.
- `MockTicketRenderer`: genera un PNG transparente válido de 1x1; no representa el diseño final.
- `MockMasterTransactionRepository`: entrega un registro de desarrollo una sola vez por proceso.
- `InMemoryProcessingRegistry`: bloquea duplicados durante la vida del proceso.
- `InMemoryAuditService`: conserva auditorías durante la vida del proceso y escribe logs.

Se registran de forma explícita y únicamente en Development en `DependencyInjectionExtensions`; no existe selección secreta ni acceso de red accidental. Fuera de Development se registran los marcadores reales pendientes, el endpoint manual y OpenAPI no se publican, y el worker continúa desactivado.

## Ejecutar

Requisitos: SDK .NET 10.0.400 o compatible.

```powershell
dotnet restore .\TicketsVidanta.slnx
dotnet run --project .\TicketsVidanta\TicketsVidanta.csproj --launch-profile http
```

El perfil HTTP usa `http://localhost:5137`. Compruebe:

```powershell
Invoke-WebRequest http://localhost:5137/health
Invoke-WebRequest http://localhost:5137/openapi/v1.json
```

.NET 10 expone el documento OpenAPI estándar en `/openapi/v1.json`; no se agregó una UI Swagger de terceros. Puede importar ese documento en Swagger Editor, Postman o su cliente OpenAPI preferido.

El endpoint manual solo se mapea en `Development`. No debe exponerse sin autenticación:

```powershell
$body = @{
  resort = 'TEST'
  reservationId = '123456'
  checkNumber = 'CHK-001'
  room = '100'
  reference = 'MOCK'
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri http://localhost:5137/api/tickets/process `
  -ContentType 'application/json' `
  -Body $body
```

Consulte después `GET /api/tickets/{correlationId}/status`. Repetir la misma llave en el mismo proceso devuelve `409 Conflict` por idempotencia.

## Agregar un resolver

Siga [la guía de resolvers](TicketsVidanta/Shared/Resolvers/README.md). Primero confirme la fila del sistema en `docs/SOURCE-SYSTEM-MATRIX.md`; cree su repositorio específico; implemente `ICheckResolver`; registre una sola implementación coincidente; pruebe selección, ausencia y error. Nunca use el mock como fallback para un sistema real.

## Agregar una base de comercio

Siga [la guía de conexiones](TicketsVidanta/Shared/Database/Commerce/README.md). `CommerceDatabases:Connections` admite un número abierto de entradas; contiene solo proveedor y nombre lógico de secret. Una conexión real se resuelve mediante configuración segura y se consume desde un repositorio específico. No agregue connection strings a `appsettings*.json`.

## Implementar la tabla maestra

Complete `docs/DATABASES-TODO.md`, sustituya `MockMasterTransactionRepository` por `SqlMasterTransactionRepository`, implemente queries parametrizadas y una operación atómica de claim. Debe evitar que varias instancias reclamen el mismo registro y mapear estados sin inventarlos.

## Implementar Opera Cloud

Complete `docs/OPERA-CLOUD-TODO.md` con documentación OHIP oficial y pruebas en un ambiente autorizado. Después implemente los dos métodos de `OperaCloudClient`, configure su `HttpClient`, autenticación y DTOs confirmados, y cambie el registro de `IOperaCloudClient`. No aplique retry a 4xx funcionales; la política para 429, `Retry-After`, timeout y 5xx permanece pendiente.

## Implementar auditoría SQL

Defina con DBA esquema, tabla, llaves, índices, longitudes, retención, privacidad y permisos. Implemente `SqlAuditService` con parámetros, garantice que un fallo de auditoría tenga un tratamiento operativo acordado y sustituya el registro DI. No copie mensajes que contengan tokens o datos sensibles.

## Secretos

`appsettings.json` contiene valores públicos vacíos, no credenciales. En desarrollo pueden usarse variables de entorno o Secret Manager; en ambientes corporativos, Azure Key Vault u otro proveedor aprobado. Deben definirse ownership, rotación, managed identity/certificados y separación por ambiente antes del despliegue.

## BackgroundService

Está registrado pero termina sin procesar cuando `Processing:EnableBackgroundProcessing` es `false`, valor predeterminado. Para probarlo con mocks:

```powershell
$env:Processing__EnableBackgroundProcessing = 'true'
dotnet run --project .\TicketsVidanta\TicketsVidanta.csproj --launch-profile http
```

`IntervalSeconds`, `BatchSize` y `MaxAttempts` son Options validadas. `MaxAttempts` prepara la configuración, pero no ejecuta retries todavía. Antes de habilitar el worker con SQL real deben definirse claim, lease, concurrencia entre instancias y recuperación tras caídas.

## Pruebas

```powershell
dotnet test .\TicketsVidanta.slnx
```

Las pruebas cubren nombre/sanitización, selección de resolver, resolver ausente, registro idempotente concurrente y pipeline mock exitoso. Al incorporar integraciones, agregue pruebas unitarias de mapeo y contract/integration tests contra ambientes no productivos; no use producción como fixture.

## Pendiente y seguridad

Los checklists están en `docs/DISCOVERY-TODO.md`. Especialmente:

<!-- TODO [SECURITY]: definir autenticación API, autorización, roles, service-to-service,
Entra ID si aplica, Key Vault, managed identity, certificados y OAuth OHIP antes de exponer endpoints. -->

El endpoint manual es **DEVELOPMENT ONLY**. Falta definir rate limits, clasificación de datos, red, observabilidad, retención, hardening, escaneo de dependencias y procedimiento de incidentes. Consulte `docs/SECURITY-TODO.md`.
