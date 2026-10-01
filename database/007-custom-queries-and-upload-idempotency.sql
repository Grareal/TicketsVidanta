:setvar DatabaseName "TicketsVidanta"
USE [$(DatabaseName)];
GO

IF COL_LENGTH(N'dbo.ConfigTicketProfiles', N'CustomQuerySql') IS NULL
    ALTER TABLE dbo.ConfigTicketProfiles ADD CustomQuerySql nvarchar(max) NULL;
GO

ALTER TABLE dbo.ConfigTicketProfiles ALTER COLUMN BaseSchema sysname NULL;
ALTER TABLE dbo.ConfigTicketProfiles ALTER COLUMN BaseTable sysname NULL;
ALTER TABLE dbo.ConfigTicketProfiles ALTER COLUMN ReservationColumn nvarchar(260) NULL;
ALTER TABLE dbo.ConfigTicketProfiles ALTER COLUMN CheckNumberColumn nvarchar(260) NULL;
GO

-- La identidad de carga no incluye SourceSystem: un cheque repetido por conceptos o
-- impuestos se conserva una sola vez. Se prioriza un registro ya completado.
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.ProcessingRecords') AND name=N'UQ_ProcessingRecords_BusinessKey')
    ALTER TABLE dbo.ProcessingRecords DROP CONSTRAINT UQ_ProcessingRecords_BusinessKey;
GO
;WITH Ranked AS
(
    SELECT Id, ROW_NUMBER() OVER
    (
        PARTITION BY Resort, ReservationId, CheckNumber
        ORDER BY CASE WHEN Status='Completed' THEN 0 ELSE 1 END, StartedAtUtc DESC, Id DESC
    ) AS RowNumber
    FROM dbo.ProcessingRecords
)
DELETE FROM Ranked WHERE RowNumber > 1;
GO
ALTER TABLE dbo.ProcessingRecords ADD CONSTRAINT UQ_ProcessingRecords_BusinessKey
    UNIQUE (Resort, ReservationId, CheckNumber);
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.MasterTransactions') AND name=N'UQ_MasterTransactions_BusinessKey')
    ALTER TABLE dbo.MasterTransactions DROP CONSTRAINT UQ_MasterTransactions_BusinessKey;
GO
;WITH Ranked AS
(
    SELECT Id, ROW_NUMBER() OVER
    (
        PARTITION BY Resort, ReservationId, CheckNumber
        ORDER BY CASE WHEN Status='Completed' THEN 0 ELSE 1 END, CreatedAtUtc, Id
    ) AS RowNumber
    FROM dbo.MasterTransactions
)
DELETE FROM Ranked WHERE RowNumber > 1;
GO
ALTER TABLE dbo.MasterTransactions ADD CONSTRAINT UQ_MasterTransactions_BusinessKey
    UNIQUE (Resort, ReservationId, CheckNumber);
GO
