# Matriz de sistemas origen

Esta matriz se completará durante Discovery con evidencia y responsables. Las filas TBD no representan sistemas existentes ni decisiones tomadas.

| Negocio | Sistema | Base de datos | Identificador cheque | Tabla detalle | Correlación | Resolver | Estado |
| ------- | ------- | ------------- | -------------------- | ------------- | ----------- | -------- | ------ |
| Desarrollo | LOCALSQL | TicketsVidanta | CheckNumber | CommerceChecks / CommerceCheckItems | Resort + ReservationId + CheckNumber + SourceSystem | SqlCheckResolver | Implementado localmente |
| TBD | TBD | TBD | TBD | TBD | TBD | TBD | Pendiente Discovery |

No registrar connection strings, credenciales ni datos productivos en esta tabla.
