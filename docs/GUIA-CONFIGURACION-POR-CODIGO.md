# Guia para configurar hoteles, codigos, bases y consultas

## Que queda configurable

El flujo ya no depende de una consulta de ticket fija:

```text
tabla de transacciones
  -> TC_GROUP + TRX_CODE
  -> SourceSystem
  -> perfil de ese SourceSystem + RESORT
  -> conexion (servidor y base)
  -> SELECT personalizado
  -> ticket normalizado
  -> una sola carga a Opera por hotel + reservacion + cheque
```

La tabla maestra y todas sus columnas se definen en `FinancialTransactionSource` de
`appsettings.json`. La cadena nombrada en `ConnectionStringName` define servidor y base. Cada
destino de ticket se administra en `http://localhost:5137/db-config`: alli se guarda otra conexion,
un perfil y una regla por codigo contable.

## 1. Instalar o actualizar la base local

Ejecute desde la raiz:

```powershell
.\database\install-local.ps1 -ServerInstance '.\SQLEXPRESS'
```

El script `007-custom-queries-and-upload-idempotency.sql` agrega las consultas personalizadas y
cambia las llaves unicas. Si ya habia duplicados conserva primero el cheque completado y elimina
los otros registros locales de control; no borra datos de Opera ni de las bases origen.

## 2. Configurar la tabla maestra de Transaction Sale

No escriba contrasenas reales en Git. Use Secret Manager, variables de entorno o un proveedor de
secretos. En `appsettings.json` quedan visibles todos los espacios editables:

```json
"ConnectionStrings": {
  "FinancialTransactions": "Server=SERVIDOR;Database=BASE;User ID=LECTURA;Password=SECRETO;Encrypt=True;TrustServerCertificate=True"
},
"FinancialTransactionSource": {
  "Enabled": true,
  "ConnectionStringName": "FinancialTransactions",
  "Schema": "dbo",
  "Table": "FINANCIAL_TRANSACTIONS_P_DET_CLOUD",
  "ResortColumn": "RESORT",
  "TransactionDateColumn": "TRX_DATE",
  "BusinessDateColumn": "BUSINESS_DATE",
  "TransactionNumberColumn": "TRX_NO",
  "TcGroupColumn": "TC_GROUP",
  "TrxCodeColumn": "TRX_CODE",
  "CheckNumberColumn": "CHEQUE_NUMBER",
  "ReservationIdColumn": "RESV_NAME_ID",
  "RoomColumn": "ROOM",
  "ReferenceColumn": "REFERENCE",
  "RemarkColumn": "REMARK"
}
```

`ReservationIdColumn` es el dato que termina usandose como `@ReservationId`; para Inssist
corresponde a `cargar_a` (por ejemplo `468830 23`). `CheckNumberColumn` corresponde a `folio`
(por ejemplo `221650`). Si su Transaction Sale usa otros nombres, cambie solamente estos valores.

## 3. Crear la conexion del hotel

1. Arranque la API y abra `/db-config`.
2. En **Conexiones**, escriba un nombre como `INSSIST ACAPULCO`.
3. Capture una cadena que ya incluya `Server` y `Database`.
4. Use una cuenta SQL con permiso `SELECT` solamente.
5. Pulse **Probar** y luego **Guardar cifrada**.

Puede crear una conexion distinta por hotel, POS o sistema. La cadena se cifra y no vuelve a
mostrarse en la API.

## 4. Crear el perfil y su consulta

En **Perfiles de consulta**:

1. Nombre: `AyB Acapulco`.
2. Sistema origen: un codigo logico unico, por ejemplo `INSSIST_AYB_ACA`.
3. Resort: el valor exacto que llega en la columna configurada como `ResortColumn`. Vacio sirve
   como perfil comodin.
4. Conexion: `INSSIST ACAPULCO`.
5. Maximo de renglones: por ejemplo `250`.
6. Moneda fija: `MXN`.
7. Pegue el contenido de `config/examples/acapulco-ticket-query.sql` en **Consulta personalizada**.
8. Guarde el perfil.

La plantilla reproduce la busqueda manual:

```text
hotche.cargar_a = @ReservationId
hotche.folio = @CheckNumber
hotche (caja + turno + folio) -> hotcom
hotcom.platillo -> hotayb.codigo
```

La plantilla usa `hotayb.codigo` como descripcion segura porque el TXT no contiene encabezados de
columnas. Cuando confirme el nombre real (por ejemplo, la columna de descripcion larga), reemplace:

```sql
CONVERT(nvarchar(80), ayb.codigo) AS ItemDescription
```

por algo como:

```sql
LTRIM(RTRIM(ayb.COLUMNA_REAL_DE_DESCRIPCION)) AS ItemDescription
```

No copie literalmente `COLUMNA_REAL_DE_DESCRIPCION`: debe ser el nombre observado en su base.

## 5. Asociar el codigo de Transaction Sale

En **Rutas TC/TRX**, capture el `TC_GROUP` y `TRX_CODE` reales de la fila contable y asigne
`INSSIST_AYB_ACA`. Puede repetir el mismo `SourceSystem` en varios codigos (alimento, bebida,
impuesto) para que todos resuelvan el mismo cheque. Para otro hotel o POS, cree otra conexion,
perfil y ruta; no hay que recompilar.

Una combinacion `TC_GROUP + TRX_CODE` solo puede tener una ruta activa. Un perfil exacto por resort
tiene prioridad sobre el perfil comodin.

## 6. Contrato de una consulta personalizada

Solo se admite una sentencia `SELECT` (tambien puede iniciar con `WITH`). No use comentarios,
punto y coma interno, `INSERT`, `UPDATE`, `DELETE`, procedimientos ni tablas temporales. Estan
disponibles estos parametros, siempre parametrizados por `SqlCommand`:

| Parametro | Valor |
|---|---|
| `@ReservationId` | reservacion / `cargar_a` |
| `@CheckNumber` | cheque / `folio` |
| `@Resort` | codigo de hotel |
| `@MaxRows` | limite configurado; lo aplica el programa |

El SELECT debe devolver uno o mas alias reconocidos:

| Alias | Uso |
|---|---|
| `ItemDescription`, `ItemQuantity`, `ItemAmount` | partidas del ticket |
| `GuestName`, `Room`, `PointOfSale`, `CheckNumber` | identificacion |
| `BusinessDate`, `Time` | fecha y hora |
| `Subtotal`, `Tip`, `Tax`, `Total`, `Currency` | importes |
| `Header`, `Footer` | textos del formato |
| `Server`, `Table`, `GuestCount`, `Turn`, `CopyNumber` | mesero, mesa, personas, turno y copia |

Los nombres fisicos de servidor, base, tablas y columnas pueden variar libremente; lo estable es
este conjunto de alias de salida.

## 7. Por que el cheque se sube una sola vez

La llave de carga es `Resort + ReservationId + CheckNumber`. `SourceSystem`, `TC_GROUP`,
`TRX_CODE`, alimento, bebida e impuesto no crean cargas adicionales. Hay dos barreras:

- `MasterTransactions` solo acepta una fila local por esa llave;
- `ProcessingRecords` solo permite un procesamiento/carga por esa llave, incluso con solicitudes
  concurrentes.

Un intento fallido si puede reintentarse. Un intento completado no vuelve a cargarse.

## 8. Activar Opera solo despues de probar

Por seguridad `OperaCloud:EnableUpload` queda en `false`. Primero procese un cheque y revise el SVG
en `App_Data/generated-tickets`. Cuando conexion, consulta, contenido y credenciales OHIP esten
validados en un ambiente no productivo, configure OHIP segun `docs/OHIP-CONFIGURATION.md` y cambie
`EnableUpload` a `true`.

## Diagnostico rapido

- Sin filas: ejecute manualmente el SELECT con `468830 23` y `221650`; confirme espacios y tipos.
- Sin ruta: compare exactamente `TC_GROUP` y `TRX_CODE` de Transaction Sale.
- Sin perfil: compare `SourceSystem` de la ruta y el `RESORT` recibido.
- Producto sin nombre: configure la columna descriptiva real de `hotayb`.
- SQL rechazado: la consulta debe ser un solo SELECT sin comentarios.
- Cheque marcado duplicado: consulte `MasterTransactions` y `ProcessingRecords`; es la proteccion
  intencional de una sola carga.
