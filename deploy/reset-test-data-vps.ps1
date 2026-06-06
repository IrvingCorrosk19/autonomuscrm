# Limpia datos comerciales en VPS para pruebas manuales (conserva usuarios y tenants)
$ErrorActionPreference = "Stop"

$VpsIp = "164.68.99.83"
$VpsUser = "root"
$VpsPassword = "DC26Y0U5ER6sWj"
$HostKey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$Plink = "C:\Program Files\PuTTY\plink.exe"
$Pscp = "C:\Program Files\PuTTY\pscp.exe"
$RemoteDir = "/opt/autonomuscrm"
$LocalSql = Join-Path $PSScriptRoot "..\ops\database\09_reset_test_data.sql"
$RemoteSql = "/tmp/09_reset_test_data.sql"

Write-Host "==> 1/4 Backup rapido PostgreSQL..."
& $PSScriptRoot\backup-vps.ps1

Write-Host "==> 2/4 Subiendo script SQL..."
& $Pscp -pw $VpsPassword -batch -hostkey $HostKey $LocalSql "${VpsUser}@${VpsIp}:$RemoteSql"

Write-Host "==> 3/4 Ejecutando limpieza en PostgreSQL..."
$SqlCmd = "docker cp $RemoteSql autonomuscrm-postgres:/tmp/reset.sql && docker exec autonomuscrm-postgres psql -U postgres -d autonomuscrm -v ON_ERROR_STOP=1 -f /tmp/reset.sql"
& $Plink -ssh -pw $VpsPassword -batch -hostkey $HostKey "${VpsUser}@${VpsIp}" $SqlCmd

Write-Host "==> 4/5 Desactivando re-seed automatico (Seed__Enabled=false)..."
$SeedOff = @"
if grep -q '^Seed__Enabled=' $RemoteDir/deploy/.env; then
  sed -i 's/^Seed__Enabled=.*/Seed__Enabled=false/' $RemoteDir/deploy/.env
else
  echo 'Seed__Enabled=false' >> $RemoteDir/deploy/.env
fi
grep Seed__Enabled $RemoteDir/deploy/.env
"@
& $Plink -ssh -pw $VpsPassword -batch -hostkey $HostKey "${VpsUser}@${VpsIp}" $SeedOff

Write-Host "==> 5/5 Reiniciando API y Workers..."
& $Plink -ssh -pw $VpsPassword -batch -hostkey $HostKey "${VpsUser}@${VpsIp}" "cd $RemoteDir/deploy && docker compose -f docker-compose.vps.yml --env-file .env restart api workers"

Write-Host ""
Write-Host "LISTO. Base limpia para pruebas."
Write-Host "Usuarios conservados (password = Rol123!):"
Write-Host "  admin@autonomuscrm.local / Admin123!"
Write-Host "  sales@autonomuscrm.local / Sales123!"
Write-Host "  manager@autonomuscrm.local / Manager123!"
Write-Host "  support@autonomuscrm.local / Support123!"
Write-Host "  viewer@autonomuscrm.local / Viewer123!"
Write-Host "URL: http://164.68.99.83:8091"
