-- ============================================================================
-- 013-update-transaction-routes.sql
-- Actualiza ConfigTransactionRoutes con los codigos TRX_CODE reales observados
-- en FINANCIAL_TRANSACTIONS_P_DET_CLOUD (TCADBOPE) para SPA y KIDSCLUB.
-- ============================================================================

-- 1. Desactivar codigos semilla/ficticios obsoletos
UPDATE dbo.ConfigTransactionRoutes
SET IsEnabled = 0,
    UpdatedAtUtc = SYSDATETIMEOFFSET()
WHERE TrxCode IN ('4SPA001', '4SPA002', '4SPA004', '6KIDS01', '6KIDS02');

-- 2. Insertar / Actualizar codigos reales de transacciones
MERGE dbo.ConfigTransactionRoutes AS Target
USING (VALUES
    ('4SPA',   '400101', 'SPA',      'VINV', 1),
    ('4SPA',   '400106', 'SPA',      'VINV', 1),
    ('4SPA',   '400163', 'SPA',      'VINV', 1),
    ('4SPA',   '400172', 'SPA',      'VINV', 1),
    ('6OTROS', '600301', 'KIDSCLUB', 'VINV', 1),
    ('6OTROS', '600303', 'KIDSCLUB', 'VINV', 1),
    ('6OTROS', '600309', 'KIDSCLUB', 'VINV', 1),
    ('6OTROS', '600942', 'KIDSCLUB', 'VINV', 1),
    ('6OTROS', '640000', 'KIDSCLUB', 'VINV', 1)
) AS Source (TcGroup, TrxCode, SourceSystem, Resort, IsEnabled)
ON Target.TcGroup = Source.TcGroup AND Target.TrxCode = Source.TrxCode
WHEN MATCHED THEN
    UPDATE SET
        Target.SourceSystem = Source.SourceSystem,
        Target.Resort = Source.Resort,
        Target.IsEnabled = Source.IsEnabled,
        Target.UpdatedAtUtc = SYSDATETIMEOFFSET()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, TcGroup, TrxCode, SourceSystem, Resort, IsEnabled, CreatedAtUtc, UpdatedAtUtc)
    VALUES (NEWID(), Source.TcGroup, Source.TrxCode, Source.SourceSystem, Source.Resort, Source.IsEnabled, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
GO

