# Integración FINANCIAL TRANSACTIONS / Inssist

## Conocimiento confirmado el 22 de septiembre de 2026

`TCADBOPE.dbo.FINANCIAL_TRANSACTIONS_P_DET_CLOUD` es la fuente maestra propuesta. Cada fila
incluye, entre otros, `RESORT`, `TRX_DATE`, `BUSINESS_DATE`, `TRX_NO`, `TC_GROUP`, `TRX_CODE`,
`CHEQUE_NUMBER`, `RESV_NAME_ID`, `ROOM`, `REFERENCE` y `REMARK`.

La decisión del origen tiene dos niveles:

1. `TC_GROUP + TRX_CODE` determina el tipo de resolver o sistema.
2. `RESORT` determina el servidor/base física de ese sistema.

Reglas conocidas actualmente:

| TC_GROUP | TRX_CODE | Origen lógico | Consulta |
|---|---:|---|---|
| `6OTROS` | `6` | `INSSIST_SPA` | `spares`, `spasrp`, `spapgo`, `hotche`, `spapar` |
| `6OTROS` | `4` | `INSSIST_KIDSCLUB` | `hotche`, `hotcom`, `H_Cajas_Descrip`; cajas 56 y 57 |

Otros grupos quedan deliberadamente sin implementar. La información preliminar indica que algunos
apuntarán a Simphony y otros a Merksyst; se debe agregar un resolver por contrato confirmado, sin
usar Inssist como fallback.

## Flujo implementado

```text
FINANCIAL_TRANSACTIONS_P_DET_CLOUD (solo lectura)
  -> regla TC_GROUP + TRX_CODE
  -> bandeja local MasterTransactions (idempotencia y reintentos)
  -> conexión elegida por SourceSystem + RESORT
  -> resolver SPA o Kids Club
  -> CheckDetail normalizado
  -> ticket SVG guardado en App_Data/generated-tickets
  -> [carga OHIP deshabilitada]
```

La ingestión corporativa está apagada por defecto. Cuando se habilita, consulta una ventana reciente,
inserta únicamente llaves nuevas en la bandeja local y deja que el worker existente haga claim/lease.
Esto evita intentar escribir estados de procesamiento en la tabla corporativa.

## Configuración pendiente

Los valores vacíos son intencionales. Las cadenas reales deben venir de secretos o variables de
entorno, nunca del repositorio:

```json
{
  "ConnectionStrings": {
    "FinancialTransactions": "",
    "InssistNuevoVallarta": ""
  },
  "FinancialTransactionSource": {
    "Enabled": false
  },
  "CommerceDatabases": {
    "Connections": {
      "INSSIST_SPA:NUEVO_VALLARTA": {
        "Provider": "SqlServer",
        "ConnectionStringName": "InssistNuevoVallarta"
      },
      "INSSIST_KIDSCLUB:NUEVO_VALLARTA": {
        "Provider": "SqlServer",
        "ConnectionStringName": "InssistNuevoVallarta"
      }
    }
  }
}
```

El texto después de `:` debe coincidir exactamente (ignorando mayúsculas/minúsculas) con el valor
de `RESORT` que entregue la tabla maestra. `NUEVO_VALLARTA` es un marcador legible: debe reemplazarse
por el código real observado. Para una plaza nueva se agregan dos entradas si comparte la misma base,
o conexiones distintas si SPA y Kids Club viven en servidores diferentes.

Variables de entorno equivalentes:

```text
ConnectionStrings__FinancialTransactions
ConnectionStrings__InssistNuevoVallarta
FinancialTransactionSource__Enabled=true
```

Antes de habilitar la ingestión se debe ejecutar `database/005-financial-transaction-routing.sql` en
la base local. La cuenta de la fuente corporativa solo necesita `SELECT`; las cuentas de Inssist
también deben ser de solo lectura.

## Correlaciones aplicadas

- `RESV_NAME_ID` -> `ReservationId` / `cargar_a` / `pgo_CargoA`.
- `CHEQUE_NUMBER` -> `CheckNumber` / `folio` / `srp_Cheque`.
- `ROOM` -> habitación informativa.
- SPA filtra simultáneamente por reserva y cheque.
- Kids Club filtra reserva, cheque y cajas `56`, `57`.

Todos los parámetros de datos se envían con `SqlCommand`; no se concatenan valores procedentes de
la transacción. Los nombres de tabla permanecen fijos en código.

## Consultas de investigación conservadas

```sql
SELECT TOP (1000)
    RESORT, TRX_DATE, BUSINESS_DATE, TRX_NO, TC_GROUP, TC_SUBGROUP,
    TRX_CODE, CHEQUE_NUMBER, RESV_NAME_ID, ROOM, REFERENCE, REMARK,
    INSERT_DATE, UPDATE_DATE
FROM TCADBOPE.dbo.FINANCIAL_TRANSACTIONS_P_DET_CLOUD
WHERE TC_GROUP = '6OTROS' AND ROOM = '3575'
ORDER BY TRX_DATE DESC;
```

La consulta SPA entregada originalmente correlaciona `spares -> spasrp -> spapgo -> hotche ->
spapar` y filtra `p.pgo_CargoA`. La implementación añade el filtro por `s.srp_Cheque` para no mezclar
varios cheques de la misma reserva.

La consulta Kids Club entregada originalmente correlaciona `hotche -> hotcom -> H_Cajas_Descrip`,
filtra las cajas 56/57 y `cargar_a`. La implementación añade el filtro por `hc.folio` por la misma
razón.

## Opera Cloud: bloqueo explícito

`OperaCloud:EnableUpload` está en `false` y `OperaCloud:UseMock` en `true`. Con `EnableUpload=false`,
el pipeline no llama ni a la búsqueda de reserva ni al POST de documento: termina después de guardar
el SVG local y audita el resultado sin `OperaDocumentId`.

No se debe activar la carga hasta confirmar en un ambiente OHIP no productivo: endpoint y contrato
del payload, formato de imagen aceptado (el renderer actual produce SVG), hotel por `RESORT`, límites,
autenticación, nombre del cheque y reglas de reintento. No se realizaron pruebas de subida como parte
de esta implementación.

## Pendientes de validación con datos reales

- Código exacto de cada `RESORT` y su servidor/base Inssist.
- Tipos SQL reales de `TRX_CODE`, `CHEQUE_NUMBER`, `RESV_NAME_ID`, `folio` y `srp_Cheque`.
- Si `CHEQUE_NUMBER` siempre coincide con `folio`/`srp_Cheque` o requiere normalización adicional.
- Moneda por plaza; temporalmente el detalle Inssist usa `MXN`.
- Posibles duplicados de `spapgo` para un cheque y granularidad de `spasrp`.
- Semántica de `hm.importe` (importe de línea frente a precio unitario) y cantidad en Kids Club.
- Zona horaria de `TRX_DATE`, `fec_che` y `hra_alta`.
- Ventana y volumen adecuados para la ingestión; los defaults son 7 días y 1000 filas.
- Diseño visual final y conversión a PNG/JPEG si OHIP no acepta SVG.
