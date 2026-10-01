# Pendientes del formato de ticket

El renderer productivo genera un SVG térmico configurable desde `/ticket-formats`. Consulte
`CONFIGURADOR-FORMATOS-TICKET.md`.

Antes de producción todavía se debe confirmar:

- ancho físico, DPI y márgenes de impresión;
- logotipo corporativo y licencia de tipografías;
- redondeo, moneda y semántica de propina/impuestos;
- textos legales aprobados por hotel y país;
- datos personales permitidos;
- peso y MIME types admitidos realmente por OHIP;
- necesidad de conversión SVG a PNG/JPEG;
- casos de varias páginas o cantidades extraordinarias de partidas;
- autenticación, autorización y auditoría del configurador.

`MockTicketRenderer` permanece solamente para pruebas. Con `TicketGeneration:UseMock=false`, el
pipeline usa el renderer SVG configurable.
