# HTTPS crm.autonomousflow.lat — requiere DNS A: crm -> 164.68.99.83
$ErrorActionPreference = "Stop"
$VpsIp = "164.68.99.83"
$VpsUser = "root"
$VpsPassword = "DC26Y0U5ER6sWj"
$HostKey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$Plink = "C:\Program Files\PuTTY\plink.exe"
$Pscp = "C:\Program Files\PuTTY\pscp.exe"

Write-Host "==> Subiendo Nginx (8091 + crm.autonomousflow.lat)..."
& $Pscp -pw $VpsPassword -batch -hostkey $HostKey "$PSScriptRoot\nginx-autonomuscrm-vps.conf" "${VpsUser}@${VpsIp}:/etc/nginx/sites-available/autonomuscrm.conf"
& $Plink -ssh -pw $VpsPassword -batch -hostkey $HostKey "${VpsUser}@${VpsIp}" "ln -sf /etc/nginx/sites-available/autonomuscrm.conf /etc/nginx/sites-enabled/autonomuscrm.conf && nginx -t && systemctl reload nginx"

Write-Host "==> Ampliando certificado Let's Encrypt (crm.autonomousflow.lat)..."
$certCmd = @'
certbot certonly --webroot -w /var/www/certbot \
  -d autonomousflow.lat \
  -d carnet.autonomousflow.lat \
  -d fixhub.autonomousflow.lat \
  -d n8n.autonomousflow.lat \
  -d travel.autonomousflow.lat \
  -d restbar.autonomousflow.lat \
  -d crm.autonomousflow.lat \
  --expand --non-interactive --agree-tos 2>&1 | tail -25
systemctl reload nginx
'@
& $Plink -ssh -pw $VpsPassword -batch -hostkey $HostKey "${VpsUser}@${VpsIp}" $certCmd

Write-Host "==> Verificacion..."
curl.exe -sI "https://crm.autonomousflow.lat/Account/Login" 2>&1 | Select-Object -First 8
Write-Host "Listo: https://crm.autonomousflow.lat/Account/Login"
