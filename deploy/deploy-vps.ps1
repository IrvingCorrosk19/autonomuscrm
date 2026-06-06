# Despliegue AutonomusCRM al VPS (aislado, estilo Madison Royales)
$ErrorActionPreference = "Stop"

$VpsIp = "164.68.99.83"
$VpsUser = "root"
$VpsPassword = "DC26Y0U5ER6sWj"
$HostKey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$Plink = "C:\Program Files\PuTTY\plink.exe"
$Pscp = "C:\Program Files\PuTTY\pscp.exe"
$RemoteDir = "/opt/autonomuscrm"
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$Archive = Join-Path $env:TEMP "autonomuscrm-deploy.tar.gz"

function Invoke-Vps([string]$Command) {
    & $Plink -ssh -pw $VpsPassword -batch -hostkey $HostKey "${VpsUser}@${VpsIp}" $Command
    if ($LASTEXITCODE -ne 0) { throw "VPS command failed: $Command" }
}

Write-Host "==> Backup obligatorio antes de reemplazo..."
$BackupScript = Join-Path $PSScriptRoot "backup-vps.ps1"
if (-not (Test-Path $BackupScript)) { throw "Falta backup-vps.ps1 - no se puede desplegar sin backup." }
& $BackupScript
if ($LASTEXITCODE -ne 0) { throw "Backup fallo - abortando despliegue." }

Write-Host "==> Creando paquete de despliegue..."
if (Test-Path $Archive) { Remove-Item $Archive -Force }
Push-Location $ProjectRoot
tar --exclude="./.git" --exclude="./.vs" --exclude="./**/bin" --exclude="./**/obj" --exclude="./logs" --exclude="./**/node_modules" -czf $Archive .
Pop-Location

Write-Host "==> Deteniendo y eliminando stack anterior (contenedores + volumen PostgreSQL)..."
Invoke-Vps "cd $RemoteDir/deploy 2>/dev/null && docker compose -f docker-compose.vps.yml --env-file .env down -v --remove-orphans 2>/dev/null || true"
Invoke-Vps "docker rm -f autonomuscrm-api autonomuscrm-workers autonomuscrm-postgres autonomuscrm-redis autonomuscrm-rabbitmq 2>/dev/null || true"
Invoke-Vps "docker volume rm -f deploy_autonomuscrm_pgdata autonomuscrm_pgdata 2>/dev/null || true"

Write-Host "==> Preparando directorio en VPS..."
Invoke-Vps "mkdir -p $RemoteDir/deploy && mkdir -p /var/www/certbot"

Write-Host "==> Subiendo codigo..."
& $Pscp -pw $VpsPassword -batch -hostkey $HostKey $Archive "${VpsUser}@${VpsIp}:${RemoteDir}/autonomuscrm-deploy.tar.gz"
if ($LASTEXITCODE -ne 0) { throw "pscp archive failed" }

& $Pscp -pw $VpsPassword -batch -hostkey $HostKey "$PSScriptRoot\.env.vps" "${VpsUser}@${VpsIp}:${RemoteDir}/deploy/.env"
& $Pscp -pw $VpsPassword -batch -hostkey $HostKey "$PSScriptRoot\docker-compose.vps.yml" "${VpsUser}@${VpsIp}:${RemoteDir}/deploy/docker-compose.vps.yml"
Write-Host "==> Extrayendo codigo..."
Invoke-Vps "cd $RemoteDir && tar -xzf autonomuscrm-deploy.tar.gz && rm -f autonomuscrm-deploy.tar.gz"
Write-Host "==> Construyendo Docker (api + workers)..."
Invoke-Vps "cd $RemoteDir/deploy && docker compose -f docker-compose.vps.yml --env-file .env build api workers"
Write-Host "==> Levantando stack..."
Invoke-Vps "cd $RemoteDir/deploy && docker compose -f docker-compose.vps.yml --env-file .env up -d --force-recreate"

Write-Host "==> Configurando Nginx (puerto 8091)..."
& $Pscp -pw $VpsPassword -batch -hostkey $HostKey "$PSScriptRoot\nginx-autonomuscrm-vps.conf" "${VpsUser}@${VpsIp}:/etc/nginx/sites-available/autonomuscrm.conf"
Invoke-Vps "ln -sf /etc/nginx/sites-available/autonomuscrm.conf /etc/nginx/sites-enabled/autonomuscrm.conf && nginx -t && systemctl reload nginx"
Invoke-Vps "ufw allow 8091/tcp comment 'AutonomusCRM preview' 2>/dev/null || true"

Write-Host "==> Esperando API..."
Start-Sleep -Seconds 25
Invoke-Vps "docker logs autonomuscrm-api 2>&1 | tail -15"

Write-Host "==> Aplicando optimizacion PostgreSQL (indices, VACUUM, validacion)..."
$DbOptScript = Join-Path $PSScriptRoot "apply-db-optimization-vps.ps1"
if (-not (Test-Path $DbOptScript)) { throw "Falta apply-db-optimization-vps.ps1" }
& $DbOptScript
if ($LASTEXITCODE -ne 0) { throw "Optimizacion BD fallo." }

Write-Host "==> Verificacion..."
curl.exe -sI "http://${VpsIp}:8091/Account/Login" | Select-Object -First 5
Write-Host ""
Write-Host "LISTO: http://${VpsIp}:8091/Account/Login"
$seedEnabled = (Get-Content (Join-Path $PSScriptRoot ".env.vps") -ErrorAction SilentlyContinue | Select-String "SEED_ENABLED=false")
if ($seedEnabled) {
    Write-Host "Modo pruebas: ejecute deploy\apply-vps-test-data.ps1 para cargar usuarios TechSolutions."
    Write-Host "  superadmin@autonomuscrm.local / AutonomusTest123!"
} else {
    Write-Host "Seed demo activo. Para pruebas limpias use deploy-vps-clean-test.ps1"
}
