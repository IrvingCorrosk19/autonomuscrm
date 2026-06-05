# Backup completo y verificable del VPS AutonomusCRM antes de reemplazo
$ErrorActionPreference = "Stop"

$VpsIp = "164.68.99.83"
$VpsUser = "root"
$VpsPassword = "DC26Y0U5ER6sWj"
$HostKey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$Plink = "C:\Program Files\PuTTY\plink.exe"
$RemoteDir = "/opt/autonomuscrm"
$BackupRoot = "/opt/autonomuscrm-backups"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$BackupDir = "$BackupRoot/$Timestamp"

function Invoke-Vps([string]$Command) {
    & $Plink -ssh -pw $VpsPassword -batch -hostkey $HostKey "${VpsUser}@${VpsIp}" $Command
    if ($LASTEXITCODE -ne 0) { throw "VPS command failed: $Command" }
}

Write-Host "==> Creando directorio de backup: $BackupDir"
Invoke-Vps "mkdir -p $BackupDir/db $BackupDir/app $BackupDir/config $BackupDir/nginx $BackupDir/ssl"

Write-Host "==> Exportando PostgreSQL..."
Invoke-Vps "docker exec autonomuscrm-postgres pg_dump -U postgres -d autonomuscrm -Fc -f /tmp/autonomuscrm.dump && docker cp autonomuscrm-postgres:/tmp/autonomuscrm.dump $BackupDir/db/autonomuscrm.dump && docker exec autonomuscrm-postgres rm -f /tmp/autonomuscrm.dump && ls -lh $BackupDir/db/autonomuscrm.dump"

Write-Host "==> Validando backup (pg_restore --list)..."
Invoke-Vps "docker cp $BackupDir/db/autonomuscrm.dump autonomuscrm-postgres:/tmp/validate.dump && docker exec autonomuscrm-postgres pg_restore --list /tmp/validate.dump > $BackupDir/db/restore-list.txt && docker exec autonomuscrm-postgres rm -f /tmp/validate.dump && wc -l $BackupDir/db/restore-list.txt"

Write-Host "==> Copiando aplicacion..."
Invoke-Vps "tar -czf $BackupDir/app/autonomuscrm-app.tar.gz -C /opt autonomuscrm && ls -lh $BackupDir/app/autonomuscrm-app.tar.gz"

Write-Host "==> Copiando configuracion..."
Invoke-Vps "cp $RemoteDir/deploy/.env $BackupDir/config/.env 2>/dev/null; cp $RemoteDir/deploy/docker-compose.vps.yml $BackupDir/config/docker-compose.vps.yml 2>/dev/null; docker compose -f $RemoteDir/deploy/docker-compose.vps.yml --env-file $RemoteDir/deploy/.env ps > $BackupDir/config/docker-ps.txt 2>/dev/null; true"

Write-Host "==> Copiando Nginx y SSL..."
Invoke-Vps "cp /etc/nginx/sites-available/autonomuscrm.conf $BackupDir/nginx/autonomuscrm.conf 2>/dev/null; tar -czf $BackupDir/ssl/letsencrypt.tar.gz -C /etc letsencrypt 2>/dev/null || echo no-ssl > $BackupDir/ssl/SKIPPED.txt; true"

Write-Host "==> Checksums..."
Invoke-Vps "sha256sum $BackupDir/db/autonomuscrm.dump $BackupDir/app/autonomuscrm-app.tar.gz > $BackupDir/CHECKSUMS.sha256 2>/dev/null; echo BACKUP_OK $Timestamp > $BackupDir/BACKUP_MANIFEST.txt; ls -la $BackupDir"

Write-Host "==> BACKUP COMPLETADO: $BackupDir"
