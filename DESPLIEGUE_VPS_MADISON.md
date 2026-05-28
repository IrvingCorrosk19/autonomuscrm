# DESPLIEGUE VPS - MADISON ROYALES

Este es el unico documento operativo para subir la pagina al VPS.

## Credenciales y datos del servidor

- VPS IP: `164.68.99.83`
- Usuario SSH: `root`
- Password SSH: `DC26Y0U5ER6sWj`
- HostKey SSH:
  `ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0`

## Archivos y rutas usadas

- Archivo local principal: `C:\Proyectos\Madison Royales\Madison Royales.html`
- Destino en VPS: `/var/www/madison-royales/index.html`
- Config Nginx en VPS: `/etc/nginx/sites-available/madison-royales.conf`
- Enlace Nginx: `/etc/nginx/sites-enabled/madison-royales.conf`
- Preview activa: `http://164.68.99.83:8090/`

## Requisitos en Windows

- `plink.exe` y `pscp.exe` de PuTTY instalados en:
  `C:\Program Files\PuTTY\`

## 1) Subir o actualizar HTML al VPS

```powershell
& "C:\Program Files\PuTTY\plink.exe" -ssh -pw "DC26Y0U5ER6sWj" -batch -hostkey "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0" root@164.68.99.83 "mkdir -p /var/www/madison-royales && chown www-data:www-data /var/www/madison-royales"

& "C:\Program Files\PuTTY\pscp.exe" -pw "DC26Y0U5ER6sWj" -batch -hostkey "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0" "C:\Proyectos\Madison Royales\Madison Royales.html" root@164.68.99.83:/var/www/madison-royales/index.html
```

## 2) Cargar configuracion Nginx

El contenido de referencia esta en:
`C:\Proyectos\Madison Royales\nginx-madison-royales-vps.conf`

Subir y activar:

```powershell
& "C:\Program Files\PuTTY\pscp.exe" -pw "DC26Y0U5ER6sWj" -batch -hostkey "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0" "C:\Proyectos\Madison Royales\nginx-madison-royales-vps.conf" root@164.68.99.83:/tmp/madison-royales.conf

& "C:\Program Files\PuTTY\plink.exe" -ssh -pw "DC26Y0U5ER6sWj" -batch -hostkey "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0" root@164.68.99.83 "mv /tmp/madison-royales.conf /etc/nginx/sites-available/madison-royales.conf && ln -sf /etc/nginx/sites-available/madison-royales.conf /etc/nginx/sites-enabled/madison-royales.conf && nginx -t && systemctl reload nginx"
```

## 3) Abrir puerto de preview (solo si hace falta)

```powershell
& "C:\Program Files\PuTTY\plink.exe" -ssh -pw "DC26Y0U5ER6sWj" -batch -hostkey "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0" root@164.68.99.83 "ufw allow 8090/tcp comment 'Madison Royales static' && ufw reload"
```

## 4) Verificaciones rapidas

Desde Windows:

```powershell
curl.exe -sI http://164.68.99.83:8090/
```

Desde VPS:

```bash
curl -sI http://127.0.0.1:8090/
ss -tlnp | grep 8090
```

## 5) Activar dominio HTTPS final

Dominio objetivo:
`madisonroyales.autonomousflow.lat`

### Paso DNS

Crear registro `A`:

- Host: `madisonroyales`
- Valor: `164.68.99.83`

### Paso certificado (cuando DNS ya resuelva)

```bash
certbot certonly --webroot -w /var/www/certbot \
  -d autonomousflow.lat \
  -d carnet.autonomousflow.lat \
  -d fixhub.autonomousflow.lat \
  -d n8n.autonomousflow.lat \
  -d travel.autonomousflow.lat \
  -d restbar.autonomousflow.lat \
  -d madisonroyales.autonomousflow.lat \
  --expand --non-interactive --agree-tos

systemctl reload nginx
```

## 6) Resultado esperado

- Preview inmediata: `http://164.68.99.83:8090/`
- Produccion final: `https://madisonroyales.autonomousflow.lat`

