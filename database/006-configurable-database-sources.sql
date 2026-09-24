:setvar DatabaseName "TicketsVidanta"
USE [$(DatabaseName)];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.ConfigDatabaseConnections', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfigDatabaseConnections
    (
        Id                        uniqueidentifier NOT NULL CONSTRAINT PK_ConfigDatabaseConnections PRIMARY KEY,
        Name                      nvarchar(120) NOT NULL,
        Provider                  varchar(30) NOT NULL CONSTRAINT DF_ConfigDatabaseConnections_Provider DEFAULT ('SqlServer'),
        ServerName                nvarchar(256) NULL,
        DatabaseName              nvarchar(256) NULL,
        ProtectedConnectionString nvarchar(max) NOT NULL,
        IsEnabled                 bit NOT NULL CONSTRAINT DF_ConfigDatabaseConnections_IsEnabled DEFAULT (1),
        CreatedAtUtc              datetimeoffset(7) NOT NULL CONSTRAINT DF_ConfigDatabaseConnections_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc              datetimeoffset(7) NOT NULL CONSTRAINT DF_ConfigDatabaseConnections_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_ConfigDatabaseConnections_Name UNIQUE (Name),
        CONSTRAINT CK_ConfigDatabaseConnections_Provider CHECK (Provider IN ('SqlServer'))
    );
END;
GO

IF COL_LENGTH(N'dbo.ConfigDatabaseConnections', N'ServerName') IS NULL
    ALTER TABLE dbo.ConfigDatabaseConnections ADD ServerName nvarchar(256) NULL;
IF COL_LENGTH(N'dbo.ConfigDatabaseConnections', N'DatabaseName') IS NULL
    ALTER TABLE dbo.ConfigDatabaseConnections ADD DatabaseName nvarchar(256) NULL;
GO

IF OBJECT_ID(N'dbo.ConfigTicketProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfigTicketProfiles
    (
        Id                  uniqueidentifier NOT NULL CONSTRAINT PK_ConfigTicketProfiles PRIMARY KEY,
        Name                nvarchar(120) NOT NULL,
        SourceSystem        nvarchar(80) NOT NULL,
        Resort              nvarchar(40) NULL,
        ConnectionId        uniqueidentifier NOT NULL,
        IsEnabled           bit NOT NULL CONSTRAINT DF_ConfigTicketProfiles_IsEnabled DEFAULT (1),
        BaseSchema          sysname NOT NULL,
        BaseTable           sysname NOT NULL,
        DetailSchema        sysname NULL,
        DetailTable         sysname NULL,
        BaseJoinColumn      sysname NULL,
        DetailJoinColumn    sysname NULL,
        ReservationColumn   nvarchar(260) NOT NULL,
        CheckNumberColumn   nvarchar(260) NOT NULL,
        ResortColumn        nvarchar(260) NULL,
        FieldMappingsJson   nvarchar(max) NOT NULL,
        CurrencyConstant    nvarchar(10) NULL,
        MaxRows             int NOT NULL CONSTRAINT DF_ConfigTicketProfiles_MaxRows DEFAULT (250),
        CreatedAtUtc        datetimeoffset(7) NOT NULL CONSTRAINT DF_ConfigTicketProfiles_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc        datetimeoffset(7) NOT NULL CONSTRAINT DF_ConfigTicketProfiles_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_ConfigTicketProfiles_Connection FOREIGN KEY (ConnectionId) REFERENCES dbo.ConfigDatabaseConnections(Id),
        CONSTRAINT UQ_ConfigTicketProfiles_Name UNIQUE (Name),
        CONSTRAINT CK_ConfigTicketProfiles_MaxRows CHECK (MaxRows BETWEEN 1 AND 5000),
        CONSTRAINT CK_ConfigTicketProfiles_MappingsJson CHECK (ISJSON(FieldMappingsJson) = 1)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ConfigTicketProfiles') AND name=N'UX_ConfigTicketProfiles_Route')
    CREATE UNIQUE INDEX UX_ConfigTicketProfiles_Route
        ON dbo.ConfigTicketProfiles (SourceSystem, Resort) WHERE IsEnabled = 1;
GO

IF OBJECT_ID(N'dbo.ConfigTransactionRoutes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfigTransactionRoutes
    (
        Id              uniqueidentifier NOT NULL CONSTRAINT PK_ConfigTransactionRoutes PRIMARY KEY,
        TcGroup         nvarchar(80) NOT NULL,
        TrxCode         nvarchar(80) NOT NULL,
        SourceSystem    nvarchar(80) NOT NULL,
        IsEnabled       bit NOT NULL CONSTRAINT DF_ConfigTransactionRoutes_IsEnabled DEFAULT (1),
        CreatedAtUtc    datetimeoffset(7) NOT NULL CONSTRAINT DF_ConfigTransactionRoutes_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc    datetimeoffset(7) NOT NULL CONSTRAINT DF_ConfigTransactionRoutes_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ConfigTransactionRoutes') AND name=N'UX_ConfigTransactionRoutes_Rule')
    CREATE UNIQUE INDEX UX_ConfigTransactionRoutes_Rule
        ON dbo.ConfigTransactionRoutes (TcGroup, TrxCode) WHERE IsEnabled = 1;
GO
