# Fuentes de base de datos configurables

## Objetivo

El módulo permite administrar, sin recompilar, la ruta completa:

```text
TC_GROUP + TRX_CODE
  -> SourceSystem
  -> perfil SourceSystem + RESORT (o perfil comodín)
  -> conexión SQL cifrada
  -> tabla principal + detalle opcional
  -> filtros ReservationId / CheckNumber / Resort
  -> campos normalizados del ticket
  -> renderer existente
  -> archivo local y, cuando esté habilitado, Opera Cloud
```

La vista está en `/db-config` cuando `Database:UseSqlPersistence=true` y
`DatabaseConfiguration:EnableAdminUi=true`. En Development ambos valores ya están habilitados.

## Preparación

Ejecute `database/006-configurable-database-sources.sql`, o vuelva a ejecutar el instalador idempotente:

```powershell
.\database\install-local.ps1 -ServerInstance '.\SQLEXPRESS'
```

El módulo crea tres tablas:

- `ConfigDatabaseConnections`: catálogo y cadena de conexión protegida.
- `ConfigTicketProfiles`: tablas, relación, filtros, proyección y límite de filas.
- `ConfigTransactionRoutes`: reglas dinámicas `TC_GROUP + TRX_CODE -> SourceSystem`.

## Uso de la vista

1. En **Conexiones**, capture una cadena que ya apunte al servidor y base deseados. Use una cuenta
   con `SELECT` únicamente. Presione **Probar** y después **Guardar cifrada**.
2. En **Perfiles de consulta**, elija la conexión y presione **Cargar tablas y campos**. Seleccione
   la tabla principal; opcionalmente, una tabla detalle y las columnas que forman la relación.
3. Asigne las columnas por las que se localizará el cheque. `ReservationId` y `CheckNumber` son
   obligatorias; `Resort` es opcional porque normalmente ya seleccionó la conexión física.
4. Mapee los campos de salida. Para partidas use `ItemDescription`, `ItemQuantity` e `ItemAmount`.
   Los demás roles completan encabezado, huésped, fecha, totales y leyendas. Defina `MaxRows` para
   limitar cuánto puede devolver cada cheque.
5. En **Rutas TC/TRX**, asocie el par contable al mismo `SourceSystem` del perfil.

Un perfil con resort exacto gana sobre un perfil del mismo sistema cuyo resort está vacío. Esto
permite una configuración común y excepciones por propiedad. `MOCK`, `LOCALSQL`, `INSSIST_SPA` e
`INSSIST_KIDSCLUB` están reservados para los resolvers implementados en código.

## Seguridad

Las cadenas se protegen antes del `INSERT` mediante ASP.NET Core Data Protection, con el propósito
aislado `TicketsVidanta.DatabaseConnections.v1`. Los endpoints de listado nunca devuelven la cadena,
usuario ni contraseña. Al editar, una cadena vacía conserva el secreto existente.

El cifrado depende del llavero de Data Protection. Se persiste en
`DatabaseConfiguration:KeyRingDirectory` y, en Windows, sus claves se protegen adicionalmente con
DPAPI para el usuario del proceso. En IIS, contenedores o múltiples nodos se debe configurar un
llavero compartido y protección corporativa equivalente (por ejemplo, certificado/Key Vault). Si se
pierde el llavero o cambia la identidad Windows sin migrarlo, las cadenas no podrán recuperarse y
deberán capturarse nuevamente.

La interfaz es administrativa y **no debe habilitarse en producción hasta agregar autenticación,
autorización por rol, antiforgery y auditoría de cambios**. El interruptor queda apagado por defecto.
La conectividad de los servidores origen debe restringirse por red y las credenciales deben ser de
solo lectura.

## Construcción segura de consultas

La vista no guarda SQL libre. El resolver genera una consulta parametrizada:

- tablas y columnas aceptan solamente identificadores SQL simples y se delimitan con corchetes;
- valores de reservación, cheque y resort usan parámetros;
- la relación está limitada a igualdad entre una columna principal y una de detalle;
- `TOP (@MaxRows)` impide lecturas sin límite, con máximo configurable de 5,000;
- no se ejecutan escrituras en la base origen.

Los metadatos se leen desde `sys.tables`, `sys.schemas`, `sys.columns` y `sys.types`. La cuenta
necesita visibilidad de metadatos sobre los objetos que se configurarán.

## Roles de salida

| Rol | Uso |
|---|---|
| `GuestName`, `Room`, `PointOfSale`, `CheckNumber` | Identificación del ticket |
| `BusinessDate`, `Time` | Fecha y hora |
| `Subtotal`, `Tip`, `Tax`, `Total`, `Currency` | Resumen monetario |
| `Header`, `Footer` | Encabezado y leyenda |
| `ItemDescription`, `ItemQuantity`, `ItemAmount` | Renglones del consumo |

Cuando no existe columna de moneda puede usarse la moneda fija del perfil. Cantidad toma `1` y
importe `0` cuando sus columnas no están mapeadas o contienen `NULL`.

## Operación y diagnóstico

Guardar o eliminar conexiones, perfiles o rutas recarga una instantánea en memoria; no requiere
reiniciar. Al arrancar, el servicio carga todas las entradas habilitadas. Si el script `006` no se ha
ejecutado, el servicio conserva las reglas estáticas, registra una advertencia y la vista responderá
con el error SQL correspondiente.

Una ruta dinámica también se incorpora a los predicados de ingesta maestra, por lo que no queda
fuera del `SELECT` por depender únicamente de `appsettings`. Las reglas dinámicas tienen prioridad
sobre una pareja duplicada en configuración estática.

Para revisar un problema:

1. pruebe la conexión desde la vista;
2. vuelva a cargar el esquema y confirme tablas/columnas;
3. confirme coincidencia exacta de `SourceSystem` entre ruta y perfil;
4. confirme el valor real de `RESORT` o deje el resort del perfil vacío;
5. revise permisos `SELECT`, timeout y logs por `CorrelationId`.
