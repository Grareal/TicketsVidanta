:setvar DatabaseName "TicketsVidanta"
USE [$(DatabaseName)];
GO

IF COL_LENGTH(N'dbo.ConfigTicketProfiles', N'QueryTemplate') IS NULL
    ALTER TABLE dbo.ConfigTicketProfiles ADD QueryTemplate nvarchar(max) NULL;
GO

IF EXISTS
(
    SELECT 1 FROM dbo.MasterTransactions
    GROUP BY Resort, CheckNumber HAVING COUNT(*) > 1
)
    THROW 51000, 'Hay duplicados Resort + CheckNumber en MasterTransactions. Consolídalos antes de aplicar la llave de negocio.', 1;
GO
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name=N'UQ_MasterTransactions_BusinessKey')
    ALTER TABLE dbo.MasterTransactions DROP CONSTRAINT UQ_MasterTransactions_BusinessKey;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.MasterTransactions') AND name=N'UQ_MasterTransactions_ResortCheck')
    CREATE UNIQUE INDEX UQ_MasterTransactions_ResortCheck ON dbo.MasterTransactions(Resort, CheckNumber);
GO

IF EXISTS
(
    SELECT 1 FROM dbo.ProcessingRecords
    GROUP BY Resort, CheckNumber HAVING COUNT(*) > 1
)
    THROW 51001, 'Hay duplicados Resort + CheckNumber en ProcessingRecords. Consolídalos antes de aplicar la llave de negocio.', 1;
GO
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name=N'UQ_ProcessingRecords_BusinessKey')
    ALTER TABLE dbo.ProcessingRecords DROP CONSTRAINT UQ_ProcessingRecords_BusinessKey;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProcessingRecords') AND name=N'UQ_ProcessingRecords_ResortCheck')
    CREATE UNIQUE INDEX UQ_ProcessingRecords_ResortCheck ON dbo.ProcessingRecords(Resort, CheckNumber);
GO
