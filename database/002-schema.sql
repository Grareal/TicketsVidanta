:setvar DatabaseName "TicketsVidanta"
USE [$(DatabaseName)];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.MasterTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MasterTransactions
    (
        Id              bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_MasterTransactions PRIMARY KEY,
        Resort          nvarchar(20) NOT NULL,
        ReservationId   nvarchar(80) NOT NULL,
        CheckNumber     nvarchar(80) NOT NULL,
        Room            nvarchar(20) NULL,
        Reference       nvarchar(100) NULL,
        SourceSystem    nvarchar(50) NOT NULL,
        Status          varchar(20) NOT NULL CONSTRAINT DF_MasterTransactions_Status DEFAULT ('Pending'),
        AttemptCount    int NOT NULL CONSTRAINT DF_MasterTransactions_AttemptCount DEFAULT (0),
        CorrelationId   uniqueidentifier NULL,
        ClaimedAtUtc    datetimeoffset(7) NULL,
        LeaseUntilUtc   datetimeoffset(7) NULL,
        CompletedAtUtc  datetimeoffset(7) NULL,
        LastError       nvarchar(2000) NULL,
        CreatedAtUtc    datetimeoffset(7) NOT NULL CONSTRAINT DF_MasterTransactions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc    datetimeoffset(7) NOT NULL CONSTRAINT DF_MasterTransactions_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        RowVersion      rowversion NOT NULL,
        CONSTRAINT CK_MasterTransactions_Status CHECK (Status IN ('Pending','Processing','Completed','Failed')),
        CONSTRAINT CK_MasterTransactions_AttemptCount CHECK (AttemptCount >= 0),
        CONSTRAINT UQ_MasterTransactions_BusinessKey UNIQUE (Resort, ReservationId, CheckNumber, SourceSystem)
    );
    CREATE INDEX IX_MasterTransactions_Claim
        ON dbo.MasterTransactions (Status, LeaseUntilUtc, AttemptCount, CreatedAtUtc)
        INCLUDE (Resort, ReservationId, CheckNumber, Room, Reference, SourceSystem);
END;
GO

IF OBJECT_ID(N'dbo.ProcessingRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcessingRecords
    (
        Id              bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProcessingRecords PRIMARY KEY,
        Resort          nvarchar(20) NOT NULL,
        ReservationId   nvarchar(80) NOT NULL,
        CheckNumber     nvarchar(80) NOT NULL,
        SourceSystem    nvarchar(50) NOT NULL,
        CorrelationId   uniqueidentifier NOT NULL,
        Status          varchar(30) NOT NULL,
        StartedAtUtc    datetimeoffset(7) NOT NULL,
        CompletedAtUtc  datetimeoffset(7) NULL,
        ErrorMessage    nvarchar(2000) NULL,
        CONSTRAINT UQ_ProcessingRecords_BusinessKey UNIQUE (Resort, ReservationId, CheckNumber, SourceSystem),
        CONSTRAINT UQ_ProcessingRecords_CorrelationId UNIQUE (CorrelationId),
        CONSTRAINT CK_ProcessingRecords_Status CHECK
            (Status IN ('Pending','Resolving','Resolved','GeneratingDocument','Uploading','Completed','Failed'))
    );
END;
GO

IF OBJECT_ID(N'dbo.TicketProcessingAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketProcessingAudit
    (
        Id                  uniqueidentifier NOT NULL CONSTRAINT PK_TicketProcessingAudit PRIMARY KEY,
        CorrelationId       uniqueidentifier NOT NULL,
        Resort              nvarchar(20) NOT NULL,
        ReservationId       nvarchar(80) NOT NULL,
        CheckNumber         nvarchar(80) NOT NULL,
        SourceSystem        nvarchar(50) NOT NULL,
        Status              varchar(30) NOT NULL,
        AttemptCount        int NOT NULL,
        StartedAtUtc        datetimeoffset(7) NOT NULL,
        CompletedAtUtc      datetimeoffset(7) NULL,
        OperaDocumentId     nvarchar(200) NULL,
        FileName            nvarchar(255) NULL,
        ErrorMessage        nvarchar(2000) NULL,
        CreatedAtUtc        datetimeoffset(7) NOT NULL CONSTRAINT DF_TicketProcessingAudit_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_TicketProcessingAudit_AttemptCount CHECK (AttemptCount > 0)
    );
    CREATE INDEX IX_TicketProcessingAudit_CorrelationId
        ON dbo.TicketProcessingAudit (CorrelationId, CreatedAtUtc DESC);
    CREATE INDEX IX_TicketProcessingAudit_BusinessKey
        ON dbo.TicketProcessingAudit (Resort, ReservationId, CheckNumber, SourceSystem, CreatedAtUtc DESC);
END;
GO

IF OBJECT_ID(N'dbo.CommerceChecks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommerceChecks
    (
        Id              bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommerceChecks PRIMARY KEY,
        Resort          nvarchar(20) NOT NULL,
        ReservationId   nvarchar(80) NOT NULL,
        CheckNumber     nvarchar(80) NOT NULL,
        SourceSystem    nvarchar(50) NOT NULL,
        Currency        char(3) NULL,
        Total           decimal(19,4) NULL,
        CreatedAtUtc    datetimeoffset(7) NOT NULL CONSTRAINT DF_CommerceChecks_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_CommerceChecks_BusinessKey UNIQUE (Resort, ReservationId, CheckNumber, SourceSystem)
    );
END;
GO

IF OBJECT_ID(N'dbo.CommerceCheckItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommerceCheckItems
    (
        Id              bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommerceCheckItems PRIMARY KEY,
        CheckId         bigint NOT NULL,
        LineNumber      int NOT NULL,
        Description     nvarchar(500) NOT NULL,
        Quantity        decimal(19,4) NOT NULL,
        Amount          decimal(19,4) NOT NULL,
        CONSTRAINT FK_CommerceCheckItems_Check FOREIGN KEY (CheckId)
            REFERENCES dbo.CommerceChecks(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_CommerceCheckItems_Line UNIQUE (CheckId, LineNumber),
        CONSTRAINT CK_CommerceCheckItems_LineNumber CHECK (LineNumber > 0)
    );
    CREATE INDEX IX_CommerceCheckItems_CheckId ON dbo.CommerceCheckItems (CheckId, LineNumber);
END;
GO
