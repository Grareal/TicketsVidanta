# Matriz de sistemas origen

Esta matriz se completará durante Discovery con evidencia y responsables. Las filas TBD no representan sistemas existentes ni decisiones tomadas.

| Negocio | Sistema | Base de datos | Identificador cheque | Tabla detalle | Correlación | Resolver | Estado |
| ------- | ------- | ------------- | -------------------- | ------------- | ----------- | -------- | ------ |
| Desarrollo | LOCALSQL | TicketsVidanta | CheckNumber | CommerceChecks / CommerceCheckItems | Resort + ReservationId + CheckNumber + SourceSystem | SqlCheckResolver | Implementado localmente |
| SPA | INSSIST_SPA | Por RESORT | srp_Cheque | spares / spasrp / spapgo / hotche / spapar | pgo_CargoA + srp_Cheque | InssistSpaCheckResolver | Implementado; falta conexión real |
| Kids Club | INSSIST_KIDSCLUB | Por RESORT | hotche.folio | hotche / hotcom / H_Cajas_Descrip | cargar_a + folio + caja 56/57 | InssistKidsClubCheckResolver | Implementado; falta conexión real |
| TBD | TBD | TBD | TBD | TBD | TBD | TBD | Pendiente Discovery |

No registrar connection strings, credenciales ni datos productivos en esta tabla.
