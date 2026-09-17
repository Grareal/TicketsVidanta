:setvar DatabaseName "TicketsVidanta"
USE [$(DatabaseName)];
GO

IF EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ProcessingRecords')
      AND name = N'UX_ProcessingRecords_ActiveBusinessKey'
)
BEGIN
    DROP INDEX UX_ProcessingRecords_ActiveBusinessKey ON dbo.ProcessingRecords;
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ProcessingRecords')
      AND name = N'UQ_ProcessingRecords_BusinessKey'
)
BEGIN
    ;WITH Duplicates AS
    (
        SELECT Id,
               ROW_NUMBER() OVER
               (
                   PARTITION BY Resort, ReservationId, CheckNumber, SourceSystem
                   ORDER BY StartedAtUtc DESC, Id DESC
               ) AS RowNumber
        FROM dbo.ProcessingRecords
    )
    DELETE FROM Duplicates WHERE RowNumber > 1;

    ALTER TABLE dbo.ProcessingRecords
        ADD CONSTRAINT UQ_ProcessingRecords_BusinessKey
        UNIQUE (Resort, ReservationId, CheckNumber, SourceSystem);
END;
GO
