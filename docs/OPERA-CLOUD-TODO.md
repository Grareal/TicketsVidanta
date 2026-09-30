# Discovery de Opera Cloud / OHIP

## Implementación disponible

El cliente permite obtener una reserva por ID, autenticarse con Resource Owner o Client Credentials
y cargar la imagen del cheque mediante
`/csh/v1/hotels/{HotelId}/check/{checkNumber}`. Consulte `OHIP-CONFIGURATION.md`.

Permanece pendiente validar con las credenciales y versión del ambiente autorizado: permisos,
límites, códigos funcionales específicos, política de retry y aceptación del PNG final.

<!-- TODO [OHIP-DISCOVERY]: validar exclusivamente con documentación oficial y un
ambiente autorizado los endpoints, métodos HTTP, rutas, headers, OAuth, token URL,
grant, scope, property/hotel ID, mapeo de ReservationId, DTOs, códigos y límites. -->

Para reserva: confirmar si el identificador canónico basta, cómo se acota por hotel, respuesta not-found, estados de reserva admitidos y tratamiento de múltiples coincidencias.

Para imagen: confirmar versión del contrato CSH, payload Base64, MIME aceptado, tamaño máximo,
resolución, respuesta, identificador, duplicados y forma de consultar resultado.

Para resiliencia: confirmar timeout, idempotency headers si existen, 429, `Retry-After`, 5xx reintentables, límites por hotel/cliente y observabilidad permitida. No reintentar 4xx funcionales automáticamente.

Para autenticación: confirmar emisión/caché/renovación de token, scopes, credenciales separadas por ambiente, Key Vault, rotación, certificados o managed identity donde aplique. Nunca registrar token ni secreto.
