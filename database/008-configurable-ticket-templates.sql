:setvar DatabaseName "TicketsVidanta"
USE [$(DatabaseName)];
GO

IF OBJECT_ID(N'dbo.ConfigTicketTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfigTicketTemplates
    (
        Id            uniqueidentifier NOT NULL CONSTRAINT PK_ConfigTicketTemplates PRIMARY KEY,
        Name          nvarchar(120) NOT NULL,
        SourceSystem  nvarchar(80) NULL,
        Resort        nvarchar(40) NULL,
        PointOfSale   nvarchar(120) NULL,
        MatchKey      nvarchar(250) NOT NULL,
        IsEnabled     bit NOT NULL CONSTRAINT DF_ConfigTicketTemplates_IsEnabled DEFAULT (1),
        LayoutJson    nvarchar(max) NOT NULL,
        CreatedAtUtc  datetimeoffset(7) NOT NULL CONSTRAINT DF_ConfigTicketTemplates_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc  datetimeoffset(7) NOT NULL CONSTRAINT DF_ConfigTicketTemplates_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_ConfigTicketTemplates_Name UNIQUE (Name),
        CONSTRAINT CK_ConfigTicketTemplates_LayoutJson CHECK (ISJSON(LayoutJson) = 1)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id=OBJECT_ID(N'dbo.ConfigTicketTemplates')
      AND name=N'UX_ConfigTicketTemplates_EnabledMatch'
)
    CREATE UNIQUE INDEX UX_ConfigTicketTemplates_EnabledMatch
        ON dbo.ConfigTicketTemplates (MatchKey) WHERE IsEnabled = 1;
GO
