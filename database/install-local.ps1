param(
    [string]$ServerInstance = ".\SQLEXPRESS",
    [string]$DatabaseName = "TicketsVidanta",
    [switch]$IncludeDevelopmentSeed
)

$ErrorActionPreference = "Stop"
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path

sqlcmd -S $ServerInstance -E -C -b -v DatabaseName=$DatabaseName `
    -i (Join-Path $scriptDirectory "001-create-database.sql")
sqlcmd -S $ServerInstance -E -C -b -v DatabaseName=$DatabaseName `
    -i (Join-Path $scriptDirectory "002-schema.sql")
sqlcmd -S $ServerInstance -E -C -b -v DatabaseName=$DatabaseName `
    -i (Join-Path $scriptDirectory "004-processing-retries.sql")

if ($IncludeDevelopmentSeed) {
    sqlcmd -S $ServerInstance -E -C -b -v DatabaseName=$DatabaseName `
        -i (Join-Path $scriptDirectory "003-development-seed.sql")
}

Write-Host "Database $DatabaseName installed on $ServerInstance."
