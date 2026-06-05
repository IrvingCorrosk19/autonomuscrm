# Aplica scripts de optimizacion PostgreSQL en el VPS (post-migraciones/seed)
$ErrorActionPreference = "Stop"

$VpsIp = "164.68.99.83"
$VpsUser = "root"
$VpsPassword = "DC26Y0U5ER6sWj"
$HostKey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$Plink = "C:\Program Files\PuTTY\plink.exe"
$Pscp = "C:\Program Files\PuTTY\pscp.exe"
$RemoteDir = "/opt/autonomuscrm"
$DbDir = "$RemoteDir/ops/database"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

function Invoke-Vps([string]$Command) {
    & $Plink -ssh -pw $VpsPassword -batch -hostkey $HostKey "${VpsUser}@${VpsIp}" $Command
    if ($LASTEXITCODE -ne 0) { throw "VPS command failed: $Command" }
}

function Invoke-PsqlFile([string]$FileName) {
    $remote = "$DbDir/$FileName"
    $container = "/tmp/dbopt-$FileName"
    Invoke-Vps "docker cp $remote autonomuscrm-postgres:$container && docker exec autonomuscrm-postgres psql -U postgres -d autonomuscrm -v ON_ERROR_STOP=1 -f $container && docker exec autonomuscrm-postgres rm -f $container"
}

Write-Host "==> Subiendo scripts SQL de optimizacion..."
Invoke-Vps "mkdir -p $DbDir"
Get-ChildItem (Join-Path $ProjectRoot "ops\database\*.sql") | ForEach-Object {
    & $Pscp -pw $VpsPassword -batch -hostkey $HostKey $_.FullName "${VpsUser}@${VpsIp}:${DbDir}/$($_.Name)"
    if ($LASTEXITCODE -ne 0) { throw "pscp failed for $($_.Name)" }
}

Write-Host "==> Esperando PostgreSQL..."
Invoke-Vps "docker exec autonomuscrm-postgres pg_isready -U postgres -d autonomuscrm"

Write-Host "==> 02_database_health_check.sql"
Invoke-PsqlFile "02_database_health_check.sql"

Write-Host "==> 03_indexes_optimization.sql (CONCURRENTLY, idempotente)"
Invoke-PsqlFile "03_indexes_optimization.sql"

Write-Host "==> 04_constraints_integrity.sql"
Invoke-PsqlFile "04_constraints_integrity.sql"

Write-Host "==> 06_vacuum_analyze.sql"
Invoke-PsqlFile "06_vacuum_analyze.sql"

Write-Host "==> 07_post_deploy_validation.sql"
Invoke-PsqlFile "07_post_deploy_validation.sql"

Write-Host "==> Exportando snapshot BD optimizada..."
$OptimizedDumpDir = "/opt/autonomuscrm-backups/optimized-latest"
Invoke-Vps "mkdir -p $OptimizedDumpDir && docker exec autonomuscrm-postgres pg_dump -U postgres -d autonomuscrm -Fc -f /tmp/autonomuscrm-optimized.dump && docker cp autonomuscrm-postgres:/tmp/autonomuscrm-optimized.dump $OptimizedDumpDir/autonomuscrm-optimized.dump && docker exec autonomuscrm-postgres rm -f /tmp/autonomuscrm-optimized.dump && docker cp $OptimizedDumpDir/autonomuscrm-optimized.dump autonomuscrm-postgres:/tmp/validate-opt.dump && docker exec autonomuscrm-postgres pg_restore --list /tmp/validate-opt.dump > $OptimizedDumpDir/restore-list.txt && docker exec autonomuscrm-postgres rm -f /tmp/validate-opt.dump && sha256sum $OptimizedDumpDir/autonomuscrm-optimized.dump > $OptimizedDumpDir/CHECKSUM.sha256 && ls -lh $OptimizedDumpDir"

Write-Host "==> BD optimizada aplicada y exportada en $OptimizedDumpDir"
