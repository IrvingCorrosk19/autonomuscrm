# QA post-deploy VPS — TechSolutions Panama (@autonomuscrm.local)
param(
    [string]$ConfigPath = (Join-Path (Split-Path $PSScriptRoot -Parent) "vps-test\config.vps.json")
)
if (-not (Test-Path $ConfigPath)) {
    $ConfigPath = (Join-Path (Split-Path $PSScriptRoot -Parent) "vps-test\config.json")
}
& (Join-Path $PSScriptRoot "run-first-client-qa.ps1") -ConfigPath $ConfigPath
