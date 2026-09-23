:setvar DatabaseName "TicketsVidanta"
USE [$(DatabaseName)];
GO

IF COL_LENGTH(N'dbo.MasterTransactions', N'TcGroup') IS NULL
    ALTER TABLE dbo.MasterTransactions ADD TcGroup nvarchar(80) NULL;
GO
IF COL_LENGTH(N'dbo.MasterTransactions', N'TrxCode') IS NULL
    ALTER TABLE dbo.MasterTransactions ADD TrxCode nvarchar(80) NULL;
GO
