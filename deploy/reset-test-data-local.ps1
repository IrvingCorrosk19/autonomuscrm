# Limpia BD local PostgreSQL para pruebas (conserva usuarios y tenants)
$ErrorActionPreference = "Stop"

$Root = Split-Path $PSScriptRoot -Parent
$SqlFile = Join-Path $Root "ops\database\09_reset_test_data.sql"
$BackupDir = Join-Path $Root "ops\postgres\backups"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$PgBin = "C:\Program Files\PostgreSQL\18\bin"
$Psql = Join-Path $PgBin "psql.exe"
$PgDump = Join-Path $PgBin "pg_dump.exe"
$DbHost = "localhost"
$DbPort = "5432"
$DbName = "autonomuscrm"
$DbUser = "postgres"
$DbPassword = "Panama2020$"

if (-not (Test-Path $Psql)) {
    throw "psql no encontrado. Instale PostgreSQL o ajuste la ruta en reset-test-data-local.ps1"
}

$env:PGPASSWORD = $DbPassword
New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null

Write-Host "==> 1/3 Backup local..."
$BackupFile = Join-Path $BackupDir "autonomuscrm-before-reset-$Timestamp.dump"
& $PgDump -h $DbHost -p $DbPort -U $DbUser -d $DbName -Fc -f $BackupFile
Write-Host "    Backup: $BackupFile ($((Get-Item $BackupFile).Length / 1KB) KB)"

Write-Host "==> 2/3 Ejecutando limpieza..."
& $Psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -v ON_ERROR_STOP=1 -f $SqlFile

Write-Host "==> 3/3 Desactivando Seed en appsettings.Development.json..."
$DevSettings = Join-Path $Root "AutonomusCRM.API\appsettings.Development.json"
$json = Get-Content $DevSettings -Raw | ConvertFrom-Json
$json.Seed.Enabled = $false
$json | ConvertTo-Json -Depth 10 | Set-Content $DevSettings -Encoding UTF8

Write-Host ""
Write-Host "LISTO. BD local limpia para pruebas."
Write-Host "Login: http://localhost:5000 o https://localhost:5001 (dotnet run)"
Write-Host "  sales@autonomuscrm.local / Sales123!"
Write-Host "  admin@autonomuscrm.local / Admin123!"
Write-Host "Seed desactivado en Development para evitar datos demo automaticos."
