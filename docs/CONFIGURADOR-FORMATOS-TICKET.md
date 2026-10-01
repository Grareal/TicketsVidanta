# Configurador de formatos de ticket

## Objetivo

El formato visual es independiente de la consulta y de la carga a Opera:

```text
consulta del POS -> CheckDetail normalizado
                -> plantilla por SourceSystem + Resort + PointOfSale
                -> SVG térmico
                -> archivo local
                -> adjunto OHIP (cuando se habilite)
```

La administración está disponible en `/ticket-formats` cuando se cumplen estas dos condiciones:

```json
"Database": { "UseSqlPersistence": true },
"DatabaseConfiguration": { "EnableAdminUi": true }
```

Antes de abrirla ejecute el instalador para aplicar `008-configurable-ticket-templates.sql`:

```powershell
.\database\install-local.ps1 -ServerInstance '.\SQLEXPRESS'
```

## Selección de plantilla

Cada plantilla puede dejar vacíos sus tres criterios. Vacío significa comodín. La coincidencia más
específica gana:

1. `SourceSystem + Resort + PointOfSale`;
2. `SourceSystem + Resort`;
3. `SourceSystem`;
4. plantilla completamente genérica;
5. formato térmico integrado, si no hay registros aplicables.

| Plantilla | SourceSystem | Resort | POS | Resultado |
|---|---|---|---|---|
| General | vacío | vacío | vacío | respaldo para todos |
| AyB Acapulco | `INSSIST_AYB` | `ACAPULCO` | vacío | todos los POS de AyB Acapulco |
| Yull Be Go | `INSSIST_AYB` | `ACAPULCO` | `P5H` | solamente ese POS |

Solo puede existir una plantilla habilitada para una misma combinación. Una plantilla inactiva se
conserva, pero no participa en la generación.

## Uso del editor

1. Abra `http://localhost:5137/ticket-formats`.
2. Asigne nombre y, si corresponde, sistema, resort y POS.
3. Configure ancho del papel, margen, fuente, tamaños y colores.
4. Capture razón social, RFC, régimen, dirección y encabezado.
5. Cambie etiquetas de columnas, folio, fecha, huésped, habitación, firma, propina y total.
6. Capture leyendas de impuestos, propina y textos legales bilingües.
7. Active u oculte secciones mediante las casillas.
8. Pulse **Actualizar vista previa**. Se usan datos ficticios y el alto se ajusta automáticamente.
9. Pulse **Guardar plantilla**. Los siguientes tickets aplicables usarán el nuevo formato sin reiniciar.

El diseño se persiste como JSON validado en `ConfigTicketTemplates.LayoutJson`. Los colores aceptan
únicamente `#RRGGBB`; tamaños, márgenes, longitud de textos y formato de fecha tienen límites.

## Campos disponibles desde las consultas

| Alias | Sección visual |
|---|---|
| `Header`, `Footer` | encabezado/pie procedente del POS |
| `GuestName`, `Room` | huésped y habitación |
| `PointOfSale` | selecciona plantilla específica y se imprime |
| `CheckNumber`, `BusinessDate`, `Time` | folio, fecha y hora |
| `Server`, `Table`, `GuestCount`, `Turn`, `CopyNumber` | mesero, mesa, personas, turno y copia |
| `Subtotal`, `Tip`, `Tax`, `Total`, `Currency` | resumen monetario |
| `ItemDescription`, `ItemQuantity`, `ItemAmount` | partidas |

Ejemplo parcial:

```sql
SELECT
    hc.folio AS CheckNumber,
    hc.fec_che AS BusinessDate,
    hc.hra_alta AS Time,
    hc.caja AS PointOfSale,
    hc.turno AS Turn,
    hc.num_hab AS Room,
    hm.platillo AS ItemDescription,
    CAST(1 AS decimal(19,4)) AS ItemQuantity,
    hm.importe AS ItemAmount,
    hc.sub_total AS Subtotal,
    hc.impuesto AS Tax,
    hc.tip AS Tip,
    hc.total AS Total
FROM dbo.hotche hc
INNER JOIN dbo.hotcom hm
  ON hm.caja=hc.caja AND hm.turno=hc.turno AND hm.folio=hc.folio
WHERE LTRIM(RTRIM(hc.cargar_a))=@ReservationId
  AND CONVERT(nvarchar(80),hc.folio)=@CheckNumber
```

Si la consulta devuelve `Header`, este tiene prioridad sobre el título estático. `Footer` se imprime
antes del pie estático. Así se combinan datos del POS con textos corporativos administrados.

## Formato generado

El renderer produce `image/svg+xml`, con texto nítido, poco peso y altura variable. El formato
integrado, inspirado en el comprobante proporcionado, incluye:

- encabezado fiscal centrado;
- folio, copia, fecha, hora y punto de venta;
- mesero, mesa, personas y turno;
- tabla de cantidad, descripción e importe;
- leyendas de propina e impuestos;
- subtotal, impuestos, propina, total y total en letra;
- líneas de propina, habitación, nombre y firma;
- términos legales en español e inglés.

La fotografía original no se usa como fondo: contiene perspectiva, ruido, datos manuscritos y una
firma. La plantilla reproduce la estructura con datos nuevos y texto vectorial.

## Pruebas antes de Opera

Mantenga `OperaCloud:EnableUpload=false`, procese cheques y revise los SVG en
`App_Data/generated-tickets`. Valide descripciones largas, ausencia de impuestos/propina, importes
grandes, caracteres especiales, textos extensos y selección de perfil exacto/comodín.

Active Opera únicamente después de comprobar que el ambiente OHIP acepta `image/svg+xml`. Si exige
PNG o JPEG debe agregarse conversión rasterizada; no basta cambiar el nombre o MIME type.

## Seguridad

El configurador comparte la superficie administrativa de `/db-config`. No debe exponerse en
producción sin autenticación, autorización, antiforgery y auditoría. No coloque datos de huéspedes,
firmas ni reservaciones reales en plantillas o en el repositorio.
