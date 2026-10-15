USE [TicketsVidanta];
GO

SET NOCOUNT ON;

-- 1. Recrear stg.spapgo con clave real compuesta de origen y surrogate key
IF OBJECT_ID(N'stg.spapgo', N'U') IS NOT NULL
BEGIN
    DROP TABLE stg.spapgo;
END;
GO

CREATE TABLE stg.spapgo
(
    StgId               bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_stg_spapgo PRIMARY KEY NONCLUSTERED,
    Resort              nvarchar(20) NOT NULL,
    pgo_PuntoVenta      nvarchar(40) NOT NULL CONSTRAINT DF_stg_spapgo_pv DEFAULT (''),
    pgo_Cheque          nvarchar(80) NOT NULL,
    pgo_Secuencia       nvarchar(20) NOT NULL CONSTRAINT DF_stg_spapgo_sec DEFAULT ('01'),
    pgo_CargoA          nvarchar(80) NOT NULL,
    pgo_FormaPago       nvarchar(20) NULL,
    pgo_Monto           decimal(19,4) NOT NULL CONSTRAINT DF_stg_spapgo_monto DEFAULT (0),
    pgo_Fecha           nvarchar(30) NULL,
    pgo_Hora            nvarchar(20) NULL,
    pgo_Cancelado       nvarchar(10) NULL CONSTRAINT DF_stg_spapgo_canc DEFAULT ('0'),
    pgo_ClaveReservacion nvarchar(80) NULL,
    pgo_Observaciones   nvarchar(255) NULL,
    ExtractDateUtc      datetimeoffset(7) NOT NULL CONSTRAINT DF_stg_spapgo_Extract DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_stg_spapgo_Key UNIQUE CLUSTERED (Resort, pgo_PuntoVenta, pgo_Cheque, pgo_Secuencia)
);
GO

CREATE NONCLUSTERED INDEX IX_stg_spapgo_Lookup
    ON stg.spapgo (pgo_CargoA, pgo_Cheque, Resort)
    INCLUDE (pgo_ClaveReservacion, pgo_Fecha, pgo_Monto, pgo_Cancelado, pgo_FormaPago);
GO

-- 2. Actualizar vista dbo.spapgo para reflejar todas las columnas de origen manteniendo compatibilidad total
CREATE OR ALTER VIEW dbo.spapgo AS
SELECT
    pgo_PuntoVenta,
    pgo_Cheque,
    pgo_Secuencia,
    pgo_CargoA,
    pgo_FormaPago,
    pgo_Monto,
    pgo_Monto AS pgo_Importe, -- Alias para máxima compatibilidad con queries legacy
    pgo_Fecha,
    pgo_Hora,
    pgo_Cancelado,
    pgo_ClaveReservacion,
    pgo_Observaciones,
    Resort
FROM stg.spapgo;
GO
