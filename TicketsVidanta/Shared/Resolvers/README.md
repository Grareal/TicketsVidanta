# Cómo agregar un resolver

Un resolver traduce el detalle propio de un sistema origen al modelo normalizado `CheckDetail`.

1. Cree una clase pequeña que implemente `ICheckResolver`.
2. Haga que `CanHandle` use únicamente una regla de selección confirmada durante Discovery.
3. Obtenga su acceso de datos mediante una interfaz específica; no coloque SQL ni secretos en el resolver.
4. Implemente `ResolveAsync` con `CancellationToken` y devuelva `null` cuando el cheque confirmado no exista.
5. Registre la clase como `ICheckResolver` en `DependencyInjectionExtensions.AddTicketResolvers`.
6. Agregue pruebas para selección única, cheque encontrado, cheque ausente y errores controlados.
7. Actualice `docs/SOURCE-SYSTEM-MATRIX.md` y la documentación de base de datos.

No use un resolver genérico como fallback: ocultaría errores de configuración. Si ninguno coincide, el selector devuelve un resultado controlado, registra un warning y el pipeline audita el fallo.

> TODO [DISCOVERY]: confirmar el catálogo de sistemas origen y las reglas de selección antes de registrar resolvers reales.
