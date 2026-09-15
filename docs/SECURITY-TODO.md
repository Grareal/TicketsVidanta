# Discovery de seguridad

<!-- TODO [SECURITY]: definir autenticación del API, Entra ID/Azure AD si aplica,
autorización, roles, service-to-service, scopes, segmentación de red, Key Vault,
managed identity, certificados, OAuth OHIP y rotación antes de producción. -->

- [ ] Clasificar ReservationId, habitación, cheque, auditoría y documento.
- [ ] Definir minimización, enmascarado y retención de logs.
- [ ] Proteger o retirar el endpoint manual fuera de Development.
- [ ] Definir CORS, rate limiting, límites de body y headers de seguridad.
- [ ] Definir TLS, egress a OHIP, ingress, firewall, proxy y DNS.
- [ ] Definir supply-chain scanning, parcheo y SBOM.
- [ ] Definir acceso operativo, segregación de funciones e incident response.
- [ ] Realizar threat model y revisión de seguridad previa al go-live.

No se implementó autenticación ficticia: una elección incorrecta generaría una falsa sensación de protección.
