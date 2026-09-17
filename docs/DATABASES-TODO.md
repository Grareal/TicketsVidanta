# Discovery de bases de datos

## Implementación local disponible

Existe un esquema de referencia ejecutable en `database/` con cola maestra, claim/lease atómico,
reintentos, idempotencia persistente, auditoría y un origen normalizado `LOCALSQL`. Sirve para
desarrollo y como contrato inicial de migración; los puntos siguientes continúan pendientes para
mapearlo a infraestructura corporativa y políticas operativas reales.

## Tabla maestra

<!-- TODO [DATABASE-DISCOVERY]: confirmar servidor lógico, motor/proveedor, base, esquema,
tabla o stored procedures, columnas, PK, estados, criterio/orden de pendientes,
ReservationId/RESV_NAME_ID, CheckNumber, Reference, Resort, sistema origen,
marcas de tiempo, intentos, longitudes, nulabilidad y volumen esperado. -->

También se requieren permisos mínimos, cifrado, timeout, índices, aislamiento, operación atómica de claim/lease, recuperación de claims abandonados, actualización completed/failed y estrategia de migraciones.

## Comercios

Por cada fila confirmada en `SOURCE-SYSTEM-MATRIX.md`, registrar owner, ambiente, proveedor, nombre lógico del secret, esquema, tabla/SP, llave del cheque, mapeo a `CheckDetail`, correlación, zona horaria, volumen, SLA y permisos read-only.

No colocar aquí secretos ni datos productivos. La implementación se ubicará en un repositorio específico consumido por su resolver.

## Auditoría

<!-- TODO [DATABASE-DISCOVERY]: confirmar repositorio de auditoría, modelo físico, PK,
restricción por CorrelationId/intento, índices, retención, purga, privacidad, cifrado,
permisos y comportamiento cuando la escritura falla. -->
