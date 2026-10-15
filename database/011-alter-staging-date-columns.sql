USE [TicketsVidanta];
GO

SET NOCOUNT ON;

PRINT 'Ajustando columnas de fecha a nvarchar(30) en tablas de staging para soportar formatos heterogéneos (YYYYMMDD)...';

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_stg_hotche_Lookup' AND object_id = OBJECT_ID('stg.hotche'))
    DROP INDEX IX_stg_hotche_Lookup ON stg.hotche;

ALTER TABLE stg.hotche ALTER COLUMN fec_che nvarchar(30) NULL;
ALTER TABLE stg.hotcom ALTER COLUMN fec_che nvarchar(30) NULL;
ALTER TABLE stg.spasrp ALTER COLUMN srp_Fecha nvarchar(30) NULL;
ALTER TABLE stg.spapgo ALTER COLUMN pgo_Fecha nvarchar(30) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_stg_hotche_Lookup' AND object_id = OBJECT_ID('stg.hotche'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_stg_hotche_Lookup 
        ON stg.hotche (cargar_a, folio, Resort)
        INCLUDE (total, tip, fec_che, num_hab, notas, caja, turno, sub_total, impuesto);
END;

PRINT 'Ajuste completado exitosamente.';
GO

