param(
    [string]$ServerInstance = ".\SQLEXPRESS",
    [string]$DatabaseName = "TicketsVidanta",
    [switch]$IncludeDevelopmentSeed
)

$ErrorActionPreference = "Stop"
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path

function Invoke-DatabaseScript([string]$FileName) {
    sqlcmd -S $ServerInstance -E -b -v DatabaseName=$DatabaseName `
        -i (Join-Path $scriptDirectory $FileName)
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed with exit code $LASTEXITCODE while executing $FileName."
    }
}

Invoke-DatabaseScript "001-create-database.sql"
Invoke-DatabaseScript "002-schema.sql"
Invoke-DatabaseScript "004-processing-retries.sql"
Invoke-DatabaseScript "005-financial-transaction-routing.sql"
Invoke-DatabaseScript "006-configurable-database-sources.sql"

if ($IncludeDevelopmentSeed) {
    Invoke-DatabaseScript "003-development-seed.sql"
}

Write-Host "Database $DatabaseName installed on $ServerInstance."
