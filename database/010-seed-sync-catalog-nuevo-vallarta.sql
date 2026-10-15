USE [TicketsVidanta];
GO

SET NOCOUNT ON;

-- Limpieza o actualización idempotente de entradas previas para este Resort y Conexión
MERGE etl.SyncTableCatalog AS tgt
USING (
    VALUES
        -- -------------------------------------------------------------------------
        -- TABLAS PARA RESOLVER INSSIST_SPA
        -- -------------------------------------------------------------------------
        -- 1. spapgo: Pagos de servicios de Spa (incremental por fecha de pago y folio/cheque)
        ('INSSIST_SPA', 'VINV', 'Inssist NuevoVallarta', 'dbo.spapgo', 'stg.spapgo',
         'pgo_Fecha', 'pgo_Cheque', CAST('2026-07-01' AS date), 5000, 15, 180, 1),

        -- 2. spasrp: Detalle de servicios de Spa consumidos (incremental por fecha de servicio)
        ('INSSIST_SPA', 'VINV', 'Inssist NuevoVallarta', 'dbo.spasrp', 'stg.spasrp',
         'srp_Fecha', 'srp_Cheque', CAST('2026-07-01' AS date), 5000, 15, 180, 1),

        -- 3. spares: Catálogo de reservaciones Spa (nombres de huésped y habitación, sync por clave)
        ('INSSIST_SPA', 'VINV', 'Inssist NuevoVallarta', 'dbo.spares', 'stg.spares',
         'NONE', 'res_Clave', CAST('2026-07-01' AS date), 5000, 0, 300, 1),

        -- 4. hotche: Encabezado de comanda/cheque (necesario en SPA para extraer la propina "tip")
        ('INSSIST_SPA', 'VINV', 'Inssist NuevoVallarta', 'dbo.hotche', 'stg.hotche',
         'fec_che', 'folio', CAST('2026-07-01' AS date), 5000, 15, 180, 1),

        -- -------------------------------------------------------------------------
        -- TABLAS PARA RESOLVER KIDS CLUB
        -- -------------------------------------------------------------------------
        -- 5. hotcom: Partidas/cargos de comanda de Kids Club (incremental por fecha y folio)
        ('INSSIST_KIDSCLUB', 'VINV', 'Inssist NuevoVallarta', 'dbo.hotcom', 'stg.hotcom',
         'fec_che', 'folio', CAST('2026-07-01' AS date), 5000, 15, 180, 1),

        -- 6. hotayb: Catálogo de artículos/servicios de comanda (descripción de cargos Kids Club)
        ('INSSIST_KIDSCLUB', 'VINV', 'Inssist NuevoVallarta', 'dbo.hotayb', 'stg.hotayb',
         'NONE', 'codigo', CAST('2026-07-01' AS date), 5000, 0, 600, 1)
) AS src (
    SourceSystem, Resort, ConnectionName, RemoteTable, LocalTable,
    DateColumn, KeyColumn, InitialExtractDate, BatchSize, OverlapMinutes, IntervalSeconds, IsEnabled
)
ON tgt.SourceSystem = src.SourceSystem
AND tgt.Resort = src.Resort
AND tgt.RemoteTable = src.RemoteTable
WHEN MATCHED THEN
    UPDATE SET
        tgt.ConnectionName    = src.ConnectionName,
        tgt.LocalTable        = src.LocalTable,
        tgt.DateColumn        = src.DateColumn,
        tgt.KeyColumn         = src.KeyColumn,
        tgt.InitialExtractDate= src.InitialExtractDate,
        tgt.BatchSize         = src.BatchSize,
        tgt.OverlapMinutes    = src.OverlapMinutes,
        tgt.IntervalSeconds   = src.IntervalSeconds,
        tgt.IsEnabled         = src.IsEnabled,
        tgt.UpdatedAtUtc      = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (
        SourceSystem, Resort, ConnectionName, RemoteTable, LocalTable,
        DateColumn, KeyColumn, InitialExtractDate, LastExtractedDate, LastExtractedKey,
        BatchSize, OverlapMinutes, IntervalSeconds, IsEnabled
    )
    VALUES (
        src.SourceSystem, src.Resort, src.ConnectionName, src.RemoteTable, src.LocalTable,
        src.DateColumn, src.KeyColumn, src.InitialExtractDate,
        CAST(src.InitialExtractDate AS datetime2(3)), '0',
        src.BatchSize, src.OverlapMinutes, src.IntervalSeconds, src.IsEnabled
    );
GO

-- Verificación de registros cargados
SELECT
    CatalogId,
    SourceSystem,
    Resort,
    ConnectionName,
    RemoteTable,
    LocalTable,
    DateColumn,
    KeyColumn,
    InitialExtractDate,
    LastExtractedDate,
    BatchSize,
    OverlapMinutes,
    IntervalSeconds,
    IsEnabled
FROM etl.SyncTableCatalog
ORDER BY SourceSystem, RemoteTable;
GO

