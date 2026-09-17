# TicketsVidanta

Orquestador ETL interno para procesar cheques y, cuando las integraciones corporativas estén definidas, adjuntar su representación visual a una reserva de Opera Cloud. Este repositorio implementa lo conocido, encapsula lo que requerirá integración y conserva como pendiente todo dato que debe obtenerse durante Discovery.

## Estado actual

El proyecto productivo es una sola API ASP.NET Core sobre .NET 10, con nullable habilitado, inyección de dependencias nativa, logging estructurado, Options Pattern, `HttpClientFactory`, OpenAPI, `async/await` y `CancellationToken`. Existe un segundo proyecto únicamente para pruebas unitarias.

Hoy funciona de extremo a extremo un escenario de desarrollo:

```text
tabla SQL o POST manual -> validar -> correlation ID -> idempotencia SQL
-> seleccionar SqlCheckResolver o MockCheckResolver -> detalle normalizado
-> reserva Mock/OHIP -> PNG -> nombre sanitizado -> upload Mock/OHIP -> auditoría SQL
-> completed
```

El ambiente `Development` usa SQL Server Express para cola maestra, claim/lease, idempotencia,
consulta de estado, detalle de cheque local y auditoría. Opera Cloud y el renderer continúan en
modo Mock hasta proporcionar credenciales OHIP y aprobar el diseño visual.

## Base de datos local

La instalación local predeterminada usa `DESKTOP-CRK4HOF\SQLEXPRESS` mediante autenticación
integrada y la base `TicketsVidanta`. Los scripts son idempotentes y aceptan el nombre de la base
como variable, por lo que pueden ejecutarse posteriormente en otra instancia:

```powershell
.\database\install-local.ps1 -ServerInstance '.\SQLEXPRESS' -IncludeDevelopmentSeed
```

Se crean las tablas `MasterTransactions`, `ProcessingRecords`, `TicketProcessingAudit`,
`CommerceChecks` y `CommerceCheckItems`, además de llaves, constraints e índices. El seed agrega
un cheque `LOCALSQL` y una transacción pendiente que el worker procesa automáticamente.

Para migrar a otro servidor, ejecute el mismo script indicando la instancia, establezca
`Database:UseSqlPersistence=true` y cambie `ConnectionStrings:TicketsVidanta`. En un ambiente real
coloque la cadena en variables de entorno, Secret Manager o Key Vault; no la agregue al repositorio.

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
    Auditing/             Auditoría SQL o en memoria
    Configuration/        Options tipadas
    Database/Master/      Cola maestra SQL con claim/lease
    Database/Commerce/    Catálogo extensible de conexiones
    Exceptions/           Excepciones específicas disponibles
    Extensions/           Registro de dependencias
    Naming/               Nombre de archivo aislado
    OperaCloud/           Mock y cliente OHIP real con OAuth
    Processing/           Idempotencia SQL/memoria y BackgroundService
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
- `InMemoryProcessingRegistry` e `InMemoryAuditService`: alternativas cuando
  `Database:UseSqlPersistence=false`; Development usa las implementaciones SQL.

Los mocks de OHIP y render se seleccionan explícitamente mediante `UseMock`; no existe acceso de
red accidental. El endpoint manual y OpenAPI solo se publican en Development.

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

## Tabla maestra

`SqlMasterTransactionRepository` reclama lotes atómicamente mediante `UPDLOCK`, `READPAST` y
`ROWLOCK`, asigna un lease recuperable y limita intentos. Para conectarlo a una tabla corporativa,
mantenga el contrato de `IMasterTransactionRepository` y adapte únicamente el repositorio o las
vistas/stored procedures del servidor destino.

## Activar Opera Cloud

`OperaCloudClient` implementa OAuth `client_credentials` y `password`, consulta de reserva y carga
Base64 mediante `/med/config/v1/fileAttachments`. Complete los valores vacíos de `OperaCloud`
mediante secretos, cambie `OperaCloud:UseMock` a `false` y pruebe primero en un ambiente OHIP no
productivo. La aplicación valida al arrancar que la configuración obligatoria esté completa.

## Auditoría SQL

`SqlAuditService` persiste resultados parametrizados y permite búsquedas por correlación y llave de
negocio. Antes de producción todavía deben acordarse retención, purga, privacidad y permisos.

## Secretos

`appsettings.json` contiene valores públicos vacíos, no credenciales. En desarrollo pueden usarse variables de entorno o Secret Manager; en ambientes corporativos, Azure Key Vault u otro proveedor aprobado. Deben definirse ownership, rotación, managed identity/certificados y separación por ambiente antes del despliegue.

## BackgroundService

Está registrado y en Development queda habilitado. Reclama registros pendientes de SQL al arrancar
y después en el intervalo configurado. Para deshabilitarlo temporalmente:

```powershell
$env:Processing__EnableBackgroundProcessing = 'false'
dotnet run --project .\TicketsVidanta\TicketsVidanta.csproj --launch-profile http
```

`IntervalSeconds`, `BatchSize`, `MaxAttempts` y `ClaimLeaseSeconds` son Options validadas. El claim
es atómico entre instancias, los fallos se reabren hasta `MaxAttempts` y un lease vencido permite
recuperar trabajo abandonado tras una caída.

## Pruebas

```powershell
dotnet test .\TicketsVidanta.slnx
```

Las pruebas cubren nombre/sanitización, selección de resolver, resolver ausente, idempotencia,
reintentos, pipeline Mock y contratos HTTP principales de OHIP. Las integraciones deben probarse
contra ambientes no productivos; no use producción como fixture.

## Pendiente y seguridad

Los checklists están en `docs/DISCOVERY-TODO.md`. Especialmente:

<!-- TODO [SECURITY]: definir autenticación API, autorización, roles, service-to-service,
Entra ID si aplica, Key Vault, managed identity, certificados y OAuth OHIP antes de exponer endpoints. -->

El endpoint manual es **DEVELOPMENT ONLY**. Falta definir rate limits, clasificación de datos, red, observabilidad, retención, hardening, escaneo de dependencias y procedimiento de incidentes. Consulte `docs/SECURITY-TODO.md`.
