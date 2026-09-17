:setvar DatabaseName "TicketsVidanta"

IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN
    PRINT N'Creating database $(DatabaseName)...';
    CREATE DATABASE [$(DatabaseName)];
END;
GO

ALTER DATABASE [$(DatabaseName)] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO
