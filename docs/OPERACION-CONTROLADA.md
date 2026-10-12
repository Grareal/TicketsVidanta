# Operación controlada y consumo de base de datos

## Lectura de transacciones financieras

La lectura de `FINANCIAL_TRANSACTIONS_P_DET_CLOUD` está desactivada por defecto. Al habilitarla,
la configuración base consulta como máximo 100 filas cada 300 segundos (antes eran 1,000 cada 60
segundos): 50 veces menos filas candidatas por hora. El alta en `MasterTransactions` se hace ahora
por bloques de hasta 200 filas, en lugar de una sentencia por fila.

La ventana `LookbackDays` sigue en 7 para no descartar transacciones tardías. Es una ventana de
seguridad, no un cursor; por tanto se volverán a leer candidatos recientes y la llave única local
evitará duplicados. Cuando el propietario de la fuente confirme el SLA de llegada tardía, reduzca
esa ventana (por ejemplo, a 1 día) y solicite un índice en la fuente equivalente a:

```sql
CREATE INDEX IX_FinancialTransactions_TicketIngestion
ON dbo.FINANCIAL_TRANSACTIONS_P_DET_CLOUD (TC_GROUP, TRX_CODE, TRX_DATE)
INCLUDE (RESORT, BUSINESS_DATE, TRX_NO, CHEQUE_NUMBER, RESV_NAME_ID, ROOM, REFERENCE, REMARK);
```

El orden y las columnas reales deben validarse con el DBA antes de crear ese índice. La consulta ya
no aplica `CONVERT` sobre `TRX_CODE`, por lo que SQL Server puede aprovechar un índice compatible.

Las consultas de detalle de SPA y Kids Club también dejaron de envolver columnas de filtro y unión
con `LTRIM`, `RTRIM` o `CONVERT`. En SQL Server la igualdad normal ya ignora el relleno final de
campos `char`; los filtros pasan parámetros tipados para favorecer seeks. Antes de desplegar,
compruebe el plan de ejecución con un cheque real en el ambiente de prueba y, si falta, solicite al
DBA índices sobre las llaves de cheque, reservación y unión de esas tablas.

## Prueba acotada de carga a Opera Cloud

Use `Processing:Mode=Controlled` para ejecutar un único lote y detener el worker. Ese modo reclama
solamente filas `Pending`; no recupera leases ni reintenta fallos. Requiere al menos un selector y
el máximo está limitado a 100 documentos. Para una prueba de exactamente un documento, configúrelo
mediante secretos o variables de entorno antes de iniciar la aplicación:

```json
"Processing": {
  "EnableBackgroundProcessing": true,
  "Mode": "Controlled",
  "Controlled": {
    "MaxDocuments": 1,
    "Documents": [
      {
        "Resort": "VINV",
        "ReservationId": "123456",
        "CheckNumber": "CHK-001"
      }
    ]
  }
}
```

Como alternativa, para probar un tipo de documento, filtre por `SourceSystems`, `TcGroups`,
`TrxCodes` o `Resorts` y deje `MaxDocuments` en el número aprobado. Los filtros se combinan; si
incluye `Documents`, solo se tomarán esos documentos exactos.

Para una carga real, además de ese bloque, se requiere `OperaCloud:EnableUpload=true` y las
credenciales/configuración OHIP válidas. Revise primero que el ticket deseado esté `Pending` y que
el selector no incluya otros registros. Tras la prueba, restaure `Mode` a `Automatic` (o deshabilite
el worker); no deje Opera Cloud habilitado con una configuración de selección amplia.
