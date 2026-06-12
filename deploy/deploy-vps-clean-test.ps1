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

Write-Host "==> 5/6 Cargando datos de prueba SQL..."
& (Join-Path $PSScriptRoot "apply-vps-test-data.ps1")
if ($LASTEXITCODE -ne 0) { throw "Carga SQL fallo." }

Write-Host "==> 6/6 QA automatizado contra VPS..."
Start-Sleep -Seconds 10
$QaScript = Join-Path $ProjectRoot "tests\e2e\run-vps-test-qa.ps1"
$VpsConfig = Join-Path $ProjectRoot "tests\vps-test\config.vps.json"
if (Test-Path $QaScript) {
    & $QaScript -ConfigPath $VpsConfig
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[WARN] QA automatizado reporto fallos - revise evidencia en tests/qa-evidence/" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== DEPLOY PRUEBAS COMPLETADO ===" -ForegroundColor Green
Write-Host "URL: http://164.68.99.83:8091/Account/Login"
Write-Host "Password: AutonomusTest123!"
Write-Host "Insumos QA: tests/vps-test/VPS_QA_START_HERE.md"
Write-Host "Docs: docs/vps-test-ready/12_GO_LIVE_TEST_READY_REPORT.md"
