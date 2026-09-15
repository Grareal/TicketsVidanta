# Checklist maestro de Discovery

## Datos y negocio

- [ ] Confirmar tabla maestra, owner y ambiente no productivo.
- [ ] Confirmar significado definitivo de `RESV_NAME_ID` y si corresponde a `ReservationId`.
- [ ] Confirmar definición y unicidad de `CheckNumber`.
- [ ] Confirmar significado, formato y uso de `Reference`.
- [ ] Confirmar catálogo de resorts y normalización de identificadores.
- [ ] Identificar sistemas origen y responsables técnicos.
- [ ] Identificar bases de datos por comercio.
- [ ] Definir agrupación/correlación de transacciones.
- [ ] Definir reglas de selección y precedencia de resolvers.
- [ ] Confirmar llave definitiva de idempotencia y reaperturas.
- [ ] Confirmar nomenclatura del archivo.

## Opera Cloud y documento

- [ ] Confirmar endpoint OHIP para localizar una reserva.
- [ ] Confirmar endpoint OHIP para adjuntar documentos.
- [ ] Confirmar OAuth, scopes y credenciales por ambiente.
- [ ] Confirmar property/hotel ID y su mapeo desde Resort.
- [ ] Confirmar payload, MIME types y tamaño máximo.
- [ ] Confirmar respuesta y `DocumentId`.
- [ ] Definir diseño visual, accesibilidad y aprobación de marca.
- [ ] Definir estrategia de retry, timeout y circuit breaking.

## Operación, datos y seguridad

- [ ] Definir persistencia y retención de auditoría.
- [ ] Definir tratamiento y reintento de fallos de auditoría.
- [ ] Definir claim/lease y concurrencia multiinstancia.
- [ ] Definir manejo de registros poison/dead-letter.
- [ ] Definir autenticación y autorización del API.
- [ ] Definir despliegue, topología de red y DNS/proxy.
- [ ] Definir Key Vault u otro secret provider.
- [ ] Definir managed identity, certificados y rotación.
- [ ] Clasificar datos personales/sensibles y reglas de logging.
- [ ] Definir liveness, readiness, métricas y alertas.
- [ ] Definir ambientes, datos de prueba y criterio de aceptación.
- [ ] Definir ownership, soporte, runbook y recuperación ante desastres.

> TODO [DISCOVERY]: asignar responsable, fuente de verdad, fecha objetivo y evidencia a cada punto antes de sustituir mocks.
