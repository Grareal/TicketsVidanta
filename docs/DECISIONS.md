# Decisiones arquitectónicas

## ADR-001: un solo proyecto ASP.NET Core

**Estado:** aceptada. Un único artefacto productivo reduce complejidad inicial. El proyecto de tests es la única excepción.

## ADR-002: Vertical Slice Architecture

**Estado:** aceptada. Los cambios de cada caso de uso se mantienen juntos y `Shared` contiene solo capacidades transversales reales.

## ADR-003: Resolver Pattern

**Estado:** aceptada, extensible. Cada sistema confirmado implementará `ICheckResolver`; el selector exige una coincidencia única.

## ADR-004: ReservationId como llave canónica actual

**Estado:** provisional. Se usa para localizar la reserva en Opera Cloud, sujeto a validación del significado de `RESV_NAME_ID`, hotel y estados.

## ADR-005: mocks para infraestructura desconocida

**Estado:** aceptada durante Discovery. Permiten compilar y demostrar el pipeline sin simular contratos productivos.

## ADR-006: idempotencia obligatoria

**Estado:** aceptada; implementación productiva pendiente. En memoria protege una instancia. La llave y almacenamiento persistente pueden cambiar.

## ADR-007: CorrelationId para trazabilidad

**Estado:** aceptada. Cada intento obtiene un `Guid` propagado en contexto, logs, auditoría y futuros requests externos cuando sea posible.

## ADR-008: retries diferidos

**Estado:** provisional. Existe `MaxAttempts`, pero no retry automático hasta conocer semántica de base de datos y OHIP; evita duplicar documentos o insistir sobre errores funcionales.
