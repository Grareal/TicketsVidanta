:setvar DatabaseName "TicketsVidanta"
USE [$(DatabaseName)];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Crear esquemas [stg] y [etl] si no existen
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'stg')
    EXEC('CREATE SCHEMA [stg];');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'etl')
    EXEC('CREATE SCHEMA [etl];');
GO

-- 2. Catálogo de sincronización y Watermarks
IF OBJECT_ID(N'etl.SyncTableCatalog', N'U') IS NULL
BEGIN
    CREATE TABLE etl.SyncTableCatalog
    (
        CatalogId           int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SyncTableCatalog PRIMARY KEY,
        SourceSystem        nvarchar(50)  NOT NULL,       -- 'INSSIST_AYB', 'INSSIST_SPA', 'SIMPHONY'
        Resort              nvarchar(20)  NOT NULL,       -- 'VINV', 'VIAC', 'VIRM'
        ConnectionName      nvarchar(100) NOT NULL,       -- Nombre de conexión en ConfigDatabaseConnections o appsettings
        RemoteTable         nvarchar(120) NOT NULL,       -- 'dbo.hotche', 'spares', etc.
        LocalTable          nvarchar(120) NOT NULL,       -- 'stg.hotche', 'stg.spares', etc.
        DateColumn          nvarchar(80)  NOT NULL,       -- 'fec_che', 'TRX_DATE', 'pgo_Fecha'
        KeyColumn           nvarchar(80)  NOT NULL,       -- 'folio', 'srp_Cheque', etc.
        InitialExtractDate  date          NOT NULL CONSTRAINT DF_SyncCatalog_InitDate DEFAULT ('2026-07-01'),
        LastExtractedDate   datetime2(3)  NOT NULL CONSTRAINT DF_SyncCatalog_LastDate DEFAULT ('2026-07-01'),
        LastExtractedKey    nvarchar(80)  NOT NULL CONSTRAINT DF_SyncCatalog_LastKey DEFAULT ('0'),
        BatchSize           int           NOT NULL CONSTRAINT DF_SyncCatalog_BatchSize DEFAULT (5000),
        OverlapMinutes      int           NOT NULL CONSTRAINT DF_SyncCatalog_Overlap DEFAULT (15),
        IntervalSeconds     int           NOT NULL CONSTRAINT DF_SyncCatalog_Interval DEFAULT (180),
        IsEnabled           bit           NOT NULL CONSTRAINT DF_SyncCatalog_IsEnabled DEFAULT (1),
        CreatedAtUtc        datetimeoffset(7) NOT NULL CONSTRAINT DF_SyncCatalog_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc        datetimeoffset(7) NOT NULL CONSTRAINT DF_SyncCatalog_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_SyncTableCatalog UNIQUE (SourceSystem, Resort, RemoteTable)
    );
END;
GO

IF OBJECT_ID(N'etl.SyncBatchLog', N'U') IS NULL
BEGIN
    CREATE TABLE etl.SyncBatchLog
    (
        BatchId             bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SyncBatchLog PRIMARY KEY,
        CatalogId           int NOT NULL,
        SourceSystem        nvarchar(50) NOT NULL,
        Resort              nvarchar(20) NOT NULL,
        TableName           nvarchar(120) NOT NULL,
        StartedAtUtc        datetimeoffset(7) NOT NULL CONSTRAINT DF_SyncBatchLog_Started DEFAULT (SYSUTCDATETIME()),
        CompletedAtUtc      datetimeoffset(7) NULL,
        RowsExtracted       int NOT NULL CONSTRAINT DF_SyncBatchLog_RowsExtracted DEFAULT (0),
        RowsUpserted        int NOT NULL CONSTRAINT DF_SyncBatchLog_RowsUpserted DEFAULT (0),
        Status              varchar(20) NOT NULL CONSTRAINT DF_SyncBatchLog_Status DEFAULT ('Running'), -- Running, Success, Failed
        ErrorMessage        nvarchar(max) NULL
    );
    CREATE INDEX IX_SyncBatchLog_Catalog ON etl.SyncBatchLog (CatalogId, StartedAtUtc DESC);
END;
GO

-- 3. Tablas de Staging en TicketsVidanta

-- A) stg.hotche (Encabezado de consumo Inssist)
IF OBJECT_ID(N'stg.hotche', N'U') IS NULL
BEGIN
    CREATE TABLE stg.hotche
    (
        StgId               bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_stg_hotche PRIMARY KEY NONCLUSTERED,
        Resort              nvarchar(20) NOT NULL,
        caja                nvarchar(20) NOT NULL,
        turno               nvarchar(20) NOT NULL,
        folio               nvarchar(40) NOT NULL,
        fec_che             nvarchar(30) NULL,
        hra_alta            nvarchar(20) NULL,
        cargar_a            nvarchar(80) NOT NULL, -- Clave de reservación
        num_hab             nvarchar(30) NULL,
        notas               nvarchar(255) NULL,
        sub_total           decimal(19,4) NOT NULL CONSTRAINT DF_stg_hotche_subtotal DEFAULT (0),
        tip                 decimal(19,4) NOT NULL CONSTRAINT DF_stg_hotche_tip DEFAULT (0),
        impuesto            decimal(19,4) NOT NULL CONSTRAINT DF_stg_hotche_impuesto DEFAULT (0),
        total               decimal(19,4) NOT NULL CONSTRAINT DF_stg_hotche_total DEFAULT (0),
        -- Auditoría y control local
        ExtractDateUtc      datetimeoffset(7) NOT NULL CONSTRAINT DF_stg_hotche_Extract DEFAULT (SYSUTCDATETIME()),
        HashKey             varbinary(32) NULL,
        ProcessedFlag       bit NOT NULL CONSTRAINT DF_stg_hotche_Processed DEFAULT (0),
        CONSTRAINT UQ_stg_hotche_Key UNIQUE CLUSTERED (Resort, caja, turno, folio)
    );
    CREATE NONCLUSTERED INDEX IX_stg_hotche_Lookup 
        ON stg.hotche (cargar_a, folio, Resort)
        INCLUDE (total, tip, fec_che, num_hab, notas, caja, turno, sub_total, impuesto);
END;
GO

-- B) stg.hotcom (Partidas de comanda Inssist)
IF OBJECT_ID(N'stg.hotcom', N'U') IS NULL
BEGIN
    CREATE TABLE stg.hotcom
    (
        StgId               bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_stg_hotcom PRIMARY KEY NONCLUSTERED,
        Resort              nvarchar(20) NOT NULL,
        caja                nvarchar(20) NOT NULL,
        turno               nvarchar(20) NOT NULL,
        folio               nvarchar(40) NOT NULL,
        fec_che             nvarchar(30) NULL,
        platillo            nvarchar(60) NOT NULL,
        cantidad            decimal(19,4) NOT NULL CONSTRAINT DF_stg_hotcom_cantidad DEFAULT (1),
        importe             decimal(19,4) NOT NULL CONSTRAINT DF_stg_hotcom_importe DEFAULT (0),
        precio              decimal(19,4) NOT NULL CONSTRAINT DF_stg_hotcom_precio DEFAULT (0),
        ExtractDateUtc      datetimeoffset(7) NOT NULL CONSTRAINT DF_stg_hotcom_Extract DEFAULT (SYSUTCDATETIME()),
        HashKey             varbinary(32) NULL
    );
    CREATE CLUSTERED INDEX IX_stg_hotcom_Key ON stg.hotcom (Resort, caja, turno, folio, platillo, StgId);
END;
GO

-- C) stg.hotayb (Catálogo de alimentos y bebidas)
IF OBJECT_ID(N'stg.hotayb', N'U') IS NULL
BEGIN
    CREATE TABLE stg.hotayb
    (
        Resort              nvarchar(20) NOT NULL,
        codigo              nvarchar(60) NOT NULL,
        descrip             nvarchar(150) NOT NULL,
        c_linea             nvarchar(40) NULL,
        ExtractDateUtc      datetimeoffset(7) NOT NULL CONSTRAINT DF_stg_hotayb_Extract DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_stg_hotayb PRIMARY KEY CLUSTERED (Resort, codigo)
    );
END;
GO

-- D) stg.spares (Reservaciones SPA)
IF OBJECT_ID(N'stg.spares', N'U') IS NULL
BEGIN
    CREATE TABLE stg.spares
    (
        Resort              nvarchar(20) NOT NULL,
        res_Clave           nvarchar(80) NOT NULL,
        cli_Nombre          nvarchar(120) NULL,
        cli_Apellido        nvarchar(120) NULL,
        res_NumeroHabitacion nvarchar(40) NULL,
        ExtractDateUtc      datetimeoffset(7) NOT NULL CONSTRAINT DF_stg_spares_Extract DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_stg_spares PRIMARY KEY CLUSTERED (Resort, res_Clave)
    );
END;
GO

-- E) stg.spasrp (Servicios de SPA por reservación/cheque)
IF OBJECT_ID(N'stg.spasrp', N'U') IS NULL
BEGIN
    CREATE TABLE stg.spasrp
    (
        StgId               bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_stg_spasrp PRIMARY KEY NONCLUSTERED,
        Resort              nvarchar(20) NOT NULL,
        srp_ClaveReservacion nvarchar(80) NOT NULL,
        srp_Cheque          nvarchar(80) NOT NULL,
        srp_Cantidad        decimal(19,4) NOT NULL CONSTRAINT DF_stg_spasrp_cant DEFAULT (1),
        srp_Nombre          nvarchar(150) NULL,
        srp_Importe         decimal(19,4) NOT NULL CONSTRAINT DF_stg_spasrp_imp DEFAULT (0),
        srp_Fecha           nvarchar(30) NULL,
        srp_HoraIni         time(0) NULL,
        ExtractDateUtc      datetimeoffset(7) NOT NULL CONSTRAINT DF_stg_spasrp_Extract DEFAULT (SYSUTCDATETIME())
    );
    CREATE CLUSTERED INDEX IX_stg_spasrp_Key ON stg.spasrp (Resort, srp_Cheque, srp_ClaveReservacion, StgId);
END;
GO

-- F) stg.spapgo (Pagos de SPA - Modelo Histórico Staging con PK Técnica IDENTITY)
IF OBJECT_ID(N'stg.spapgo', N'U') IS NULL
BEGIN
    CREATE TABLE stg.spapgo
    (
        StgId               bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_stg_spapgo PRIMARY KEY NONCLUSTERED,
        Resort              nvarchar(20) NOT NULL,
        pgo_PuntoVenta      nvarchar(40) NULL,
        pgo_Cheque          nvarchar(80) NOT NULL,
        pgo_Secuencia       nvarchar(20) NULL,
        pgo_CargoA          nvarchar(80) NOT NULL,
        pgo_FormaPago       nvarchar(20) NULL,
        pgo_Monto           decimal(19,4) NOT NULL CONSTRAINT DF_stg_spapgo_monto DEFAULT (0),
        pgo_Fecha           nvarchar(30) NULL,
        pgo_Hora            nvarchar(20) NULL,
        pgo_Cancelado       nvarchar(10) NULL,
        pgo_ClaveReservacion nvarchar(80) NULL,
        pgo_Usuario         nvarchar(60) NULL,
        pgo_Observaciones   nvarchar(255) NULL,
        ExtractDateUtc      datetimeoffset(7) NOT NULL CONSTRAINT DF_stg_spapgo_Extract DEFAULT (SYSUTCDATETIME())
    );
    CREATE CLUSTERED INDEX IX_stg_spapgo_Key 
        ON stg.spapgo (Resort, pgo_Fecha, pgo_Cheque, StgId);
    CREATE NONCLUSTERED INDEX IX_stg_spapgo_Lookup
        ON stg.spapgo (pgo_CargoA, pgo_Cheque, Resort)
        INCLUDE (pgo_ClaveReservacion, pgo_Fecha, pgo_Monto, pgo_Cancelado, pgo_FormaPago);
END;
GO

-- G) stg.spapar (Parámetros y leyendas de tickets de SPA)
IF OBJECT_ID(N'stg.spapar', N'U') IS NULL
BEGIN
    CREATE TABLE stg.spapar
    (
        Resort              nvarchar(20) NOT NULL,
        par_PuntoVenta      nvarchar(40) NOT NULL,
        par_EncabezadoTickets nvarchar(255) NULL,
        par_LeyendaTickets  nvarchar(255) NULL,
        ExtractDateUtc      datetimeoffset(7) NOT NULL CONSTRAINT DF_stg_spapar_Extract DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_stg_spapar PRIMARY KEY CLUSTERED (Resort, par_PuntoVenta)
    );
END;
GO

-- H) stg.H_Cajas_Descrip (Descripciones de cajas Kids Club)
IF OBJECT_ID(N'stg.H_Cajas_Descrip', N'U') IS NULL
BEGIN
    CREATE TABLE stg.H_Cajas_Descrip
    (
        Resort              nvarchar(20) NOT NULL,
        clave               nvarchar(30) NOT NULL,
        descrip             nvarchar(120) NULL,
        c_desc              nvarchar(120) NULL,
        ExtractDateUtc      datetimeoffset(7) NOT NULL CONSTRAINT DF_stg_hcajas_Extract DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_stg_H_Cajas_Descrip PRIMARY KEY CLUSTERED (Resort, clave)
    );
END;
GO

-- 4. Vistas de compatibilidad en esquema [dbo]
-- Permite que cualquier consulta especializada existente escrita contra 'dbo.hotche' o 'hotche'
-- corra sin tocar una sola línea de SQL al consultar TicketsVidanta local.

CREATE OR ALTER VIEW dbo.hotche AS
SELECT caja, turno, folio, fec_che, hra_alta, cargar_a, num_hab, notas, sub_total, tip, impuesto, total, Resort
FROM stg.hotche;
GO

CREATE OR ALTER VIEW dbo.hotcom AS
SELECT caja, turno, folio, fec_che, platillo, cantidad, importe, precio, Resort
FROM stg.hotcom;
GO

CREATE OR ALTER VIEW dbo.hotayb AS
SELECT codigo, descrip, c_linea, Resort
FROM stg.hotayb;
GO

CREATE OR ALTER VIEW dbo.spares AS
SELECT res_Clave, cli_Nombre, cli_Apellido, res_NumeroHabitacion, Resort
FROM stg.spares;
GO

CREATE OR ALTER VIEW dbo.spasrp AS
SELECT srp_ClaveReservacion, srp_Cheque, srp_Cantidad, srp_Nombre, srp_Importe, srp_Fecha, srp_HoraIni, Resort
FROM stg.spasrp;
GO

CREATE OR ALTER VIEW dbo.spapgo AS
SELECT
    pgo_PuntoVenta,
    pgo_Cheque,
    pgo_Secuencia,
    pgo_CargoA,
    pgo_FormaPago,
    pgo_Monto,
    pgo_Monto AS pgo_Importe,
    pgo_Fecha,
    pgo_Hora,
    pgo_Cancelado,
    pgo_ClaveReservacion,
    pgo_Usuario,
    pgo_Observaciones,
    Resort
FROM stg.spapgo;
GO

CREATE OR ALTER VIEW dbo.spapar AS
SELECT par_PuntoVenta, par_EncabezadoTickets, par_LeyendaTickets, Resort
FROM stg.spapar;
GO

CREATE OR ALTER VIEW dbo.H_Cajas_Descrip AS
SELECT clave, descrip, c_desc, Resort
FROM stg.H_Cajas_Descrip;
GO

