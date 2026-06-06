# Arranque rapido — pruebas primer cliente hoy
param(
    [switch]$CleanSlate,
    [switch]$SkipBootstrap,
    [switch]$RunQa,
    [switch]$RunAllGates,
    [switch]$StartApi,
    [string]$ConfigPath = (Join-Path (Split-Path $PSScriptRoot -Parent) "tests\first-client\config.json")
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
$PgBin = "C:\Program Files\PostgreSQL\18\bin"
$Psql = Join-Path $PgBin "psql.exe"
$PgDump = Join-Path $PgBin "pg_dump.exe"

if (-not (Test-Path $ConfigPath)) {
    Copy-Item (Join-Path $Root "tests\first-client\config.example.json") $ConfigPath
    Write-Host "Creado $ConfigPath — revise password de postgres antes de continuar." -ForegroundColor Yellow
}

$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$pg = $config.postgres
$backupDir = Join-Path $Root "ops\postgres\backups"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

Write-Host "=== AutonomusCRM — Iniciar pruebas hoy ===" -ForegroundColor Cyan

# 1. Verificar postgres
if (Test-Path $Psql) {
    $env:PGPASSWORD = $pg.password
    try {
        & $Psql -h $pg.host -p $pg.port -U $pg.user -d $pg.database -c "SELECT 1" | Out-Null
        Write-Host "[OK] PostgreSQL $($pg.host):$($pg.port)/$($pg.database)" -ForegroundColor Green
    } catch {
        throw "PostgreSQL no accesible. Verifique servicio y credenciales en config.json"
    } finally { $env:PGPASSWORD = $null }
} else {
    Write-Host "[WARN] psql no en $PgBin — omitiendo checks SQL" -ForegroundColor Yellow
}

# 2. Clean slate opcional
if ($CleanSlate) {
    if (-not (Test-Path $Psql)) { throw "CleanSlate requiere psql" }
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    $backupFile = Join-Path $backupDir "before-clean-slate-$timestamp.dump"
    $env:PGPASSWORD = $pg.password
    Write-Host "==> Backup: $backupFile"
    & $PgDump -h $pg.host -p $pg.port -U $pg.user -d $pg.database -Fc -f $backupFile
    $sql = Join-Path $Root "ops\database\11_clean_slate_first_client.sql"
    & $Psql -h $pg.host -p $pg.port -U $pg.user -d $pg.database -v ON_ERROR_STOP=1 -f $sql
    $env:PGPASSWORD = $null
    Write-Host "[OK] BD en slate limpio" -ForegroundColor Green
}

# 3. Asegurar Seed desactivado
$devSettings = Join-Path $Root "AutonomusCRM.API\appsettings.Development.json"
$json = Get-Content $devSettings -Raw | ConvertFrom-Json
if ($json.Seed.Enabled -ne $false) {
    $json.Seed.Enabled = $false
    $json | ConvertTo-Json -Depth 10 | Set-Content $devSettings -Encoding UTF8
    Write-Host "[OK] Seed.Enabled=false en Development" -ForegroundColor Green
}

# 4. Arrancar API en background
$apiUrl = $config.baseUrl.TrimEnd("/")
$apiListen = if ($apiUrl -match 'localhost') { $apiUrl -replace 'localhost', '127.0.0.1' } else { $apiUrl }
if ($StartApi) {
    Write-Host "==> Iniciando API en background ($apiListen)..."
    $apiJob = Start-Job -ScriptBlock {
        param($root, $listen)
        Set-Location $root
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        dotnet run --project (Join-Path $root "AutonomusCRM.API") --no-launch-profile --urls $listen
    } -ArgumentList $Root, $apiListen
    Write-Host "    Job Id: $($apiJob.Id) — espere ~30s antes del bootstrap"
    Start-Sleep -Seconds 25
}

# 5. Bootstrap
if (-not $SkipBootstrap) {
    & (Join-Path $Root "deploy\bootstrap-first-client.ps1") -ConfigPath $ConfigPath
}

# 6. QA opcional
if ($RunAllGates) {
    Start-Sleep -Seconds 3
    & (Join-Path $Root "tests\e2e\run-rc-all-gates.ps1") -ConfigPath $ConfigPath
} elseif ($RunQa) {
    Start-Sleep -Seconds 3
    & (Join-Path $Root "tests\e2e\run-first-client-qa.ps1") -ConfigPath $ConfigPath
}

Write-Host ""
Write-Host "=== Siguiente paso ===" -ForegroundColor Cyan
if (-not $StartApi) {
    Write-Host "Terminal 1: dotnet run --project AutonomusCRM.API"
}
Write-Host "Terminal 2: .\deploy\bootstrap-first-client.ps1   (si API ya corre y aun no bootstrap)"
Write-Host "Terminal 3: .\tests\e2e\run-rc-all-gates.ps1   (Gates 6–9)"
Write-Host "           .\tests\e2e\run-first-client-qa.ps1  (solo Gate 7/9)"
Write-Host ""
Write-Host "Handoff QA: QA_HANDOFF_READY.md"
Write-Host "Login: $apiListen/Account/Login"
Write-Host "  admin@techsolutions.pa / $($config.defaultPassword)"
Write-Host "Matriz: ROLE_TEST_MATRIX.md"
