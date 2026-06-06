# Despliegue VPS limpio para pruebas funcionales reales (sin demo seed)
$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$EnvTest = Join-Path $PSScriptRoot ".env.vps.test"
$EnvTarget = Join-Path $PSScriptRoot ".env.vps"

Write-Host "=== AutonomusCRM - Deploy VPS limpio para pruebas ===" -ForegroundColor Cyan

if (-not (Test-Path $EnvTest)) {
    throw "Falta $EnvTest. Copie .env.vps.test.example y complete secretos."
}

Write-Host "==> 1/5 Backup obligatorio..."
& (Join-Path $PSScriptRoot "backup-vps.ps1")
if ($LASTEXITCODE -ne 0) { throw "Backup fallo - abortando." }

Write-Host "==> 2/5 Copiando .env de prueba..."
Copy-Item $EnvTest $EnvTarget -Force

Write-Host "==> 3/5 Deploy stack (sin seed demo)..."
& (Join-Path $PSScriptRoot "deploy-vps.ps1")
if ($LASTEXITCODE -ne 0) { throw "Deploy fallo." }

Write-Host "==> 4/5 Esperando migraciones API..."
Start-Sleep -Seconds 20

Write-Host "==> 5/5 Cargando datos de prueba SQL..."
& (Join-Path $PSScriptRoot "apply-vps-test-data.ps1")
if ($LASTEXITCODE -ne 0) { throw "Carga SQL fallo." }

Write-Host ""
Write-Host "=== DEPLOY PRUEBAS COMPLETADO ===" -ForegroundColor Green
Write-Host "URL: http://164.68.99.83:8091/Account/Login"
Write-Host "Password: AutonomusTest123!"
Write-Host "Docs: docs/vps-test-ready/12_GO_LIVE_TEST_READY_REPORT.md"
