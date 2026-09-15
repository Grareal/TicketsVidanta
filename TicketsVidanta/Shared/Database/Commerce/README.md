# Bases de datos de comercios

`CommerceDatabaseOptions.Connections` es un catálogo extensible indexado por un identificador lógico de sistema. Cada entrada solo indica proveedor y nombre lógico de la cadena; la cadena real debe llegar desde variables de entorno, Key Vault u otro proveedor corporativo.

Para agregar una conexión, confirme primero el sistema y su contrato en `docs/SOURCE-SYSTEM-MATRIX.md`; configure una entrada sin secretos, implemente un repositorio específico del resolver y registre sus dependencias. No concentre consultas de sistemas distintos en una clase gigante.

> TODO [DATABASE-DISCOVERY]: por cada comercio faltan servidor/base, proveedor, esquema, tablas o stored procedures, columnas, permisos de solo lectura, timeout, reglas de correlación y responsable técnico.
