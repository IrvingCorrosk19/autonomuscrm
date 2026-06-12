# Aplica scripts SQL de prueba en VPS (post-migraciones)
$ErrorActionPreference = "Stop"

$VpsIp = "164.68.99.83"
$VpsUser = "root"
$VpsPassword = "DC26Y0U5ER6sWj"
$HostKey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$Plink = "C:\Program Files\PuTTY\plink.exe"
$Pscp = "C:\Program Files\PuTTY\pscp.exe"
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$SqlDir = Join-Path $ProjectRoot "ops\database\vps-test"
$RemoteSql = "/tmp/autonomuscrm-vps-test"

function Invoke-Vps([string]$Command) {
    & $Plink -ssh -pw $VpsPassword -batch -hostkey $HostKey "${VpsUser}@${VpsIp}" $Command
    if ($LASTEXITCODE -ne 0) { throw "VPS command failed: $Command" }
}

Write-Host "==> Verificando contenedor PostgreSQL..."
Invoke-Vps "docker ps --filter name=autonomuscrm-postgres --format '{{.Names}}' | grep -q autonomuscrm-postgres"

Write-Host "==> Subiendo scripts SQL..."
Invoke-Vps "mkdir -p $RemoteSql"
& $Pscp -pw $VpsPassword -batch -hostkey $HostKey "$SqlDir\02_CLEAN_TEST_DATABASE_SCRIPT.sql" "${VpsUser}@${VpsIp}:${RemoteSql}/"
& $Pscp -pw $VpsPassword -batch -hostkey $HostKey "$SqlDir\05_FUNCTIONAL_TEST_DATA.sql" "${VpsUser}@${VpsIp}:${RemoteSql}/"
if ($LASTEXITCODE -ne 0) { throw "pscp SQL failed" }

Write-Host "==> Ejecutando 02_CLEAN_TEST_DATABASE_SCRIPT.sql..."
Invoke-Vps "docker cp ${RemoteSql}/02_CLEAN_TEST_DATABASE_SCRIPT.sql autonomuscrm-postgres:/tmp/02.sql && docker exec autonomuscrm-postgres psql -U postgres -d autonomuscrm -v ON_ERROR_STOP=1 -f /tmp/02.sql"

Write-Host "==> Ejecutando 05_FUNCTIONAL_TEST_DATA.sql..."
Invoke-Vps "docker cp ${RemoteSql}/05_FUNCTIONAL_TEST_DATA.sql autonomuscrm-postgres:/tmp/05.sql && docker exec autonomuscrm-postgres psql -U postgres -d autonomuscrm -v ON_ERROR_STOP=1 -f /tmp/05.sql"

Write-Host "==> Verificacion conteos..."
Invoke-Vps "docker exec autonomuscrm-postgres psql -U postgres -d autonomuscrm -t -A -c `"SELECT COUNT(*) FROM \`"Users\`" WHERE \`"TenantId\`" = 'b1000000-0000-4000-8000-000000000001';`""

Write-Host ""
Write-Host "LISTO. Datos de prueba cargados en VPS." -ForegroundColor Green
Write-Host "Login: http://${VpsIp}:8091/Account/Login"
Write-Host "  superadmin@autonomuscrm.local / AutonomusTest123!"
