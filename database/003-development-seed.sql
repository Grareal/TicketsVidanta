:setvar DatabaseName "TicketsVidanta"
USE [$(DatabaseName)];
GO

IF NOT EXISTS
(
    SELECT 1 FROM dbo.CommerceChecks
    WHERE Resort=N'TEST' AND ReservationId=N'123456'
      AND CheckNumber=N'CHK-SQL-001' AND SourceSystem=N'LOCALSQL'
)
BEGIN
    INSERT dbo.CommerceChecks (Resort, ReservationId, CheckNumber, SourceSystem, Currency, Total)
    VALUES (N'TEST', N'123456', N'CHK-SQL-001', N'LOCALSQL', 'MXN', 290.00);

    DECLARE @CheckId bigint = SCOPE_IDENTITY();
    INSERT dbo.CommerceCheckItems (CheckId, LineNumber, Description, Quantity, Amount)
    VALUES
        (@CheckId, 1, N'Desayuno', 2, 240.00),
        (@CheckId, 2, N'Bebida', 1, 50.00);
END;
GO

IF NOT EXISTS
(
    SELECT 1 FROM dbo.MasterTransactions
    WHERE Resort=N'TEST' AND ReservationId=N'123456'
      AND CheckNumber=N'CHK-SQL-001' AND SourceSystem=N'LOCALSQL'
)
BEGIN
    INSERT dbo.MasterTransactions
        (Resort, ReservationId, CheckNumber, Room, Reference, SourceSystem)
    VALUES
        (N'TEST', N'123456', N'CHK-SQL-001', N'100', N'LOCAL-DEMO', N'LOCALSQL');
END;
GO
