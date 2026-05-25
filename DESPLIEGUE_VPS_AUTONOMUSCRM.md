# DESPLIEGUE VPS - AUTONOMUS CRM

Basado en `DESPLIEGUE_VPS_MADISON.md`. **No modifica** Madison (8090) ni apps en 8081-8084.

## Servidor

- VPS IP: `164.68.99.83`
- Usuario SSH: `root`
- HostKey: `ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0`
- Credenciales SSH: ver `DESPLIEGUE_VPS_MADISON.md` (mismo VPS)

## Rutas aisladas

| Recurso | Ruta / Puerto |
|---------|----------------|
| App Docker | `/opt/autonomuscrm` |
| API (interno) | `127.0.0.1:5080` |
| Preview Nginx | `http://164.68.99.83:8091/` |
| Postgres (solo red Docker) | contenedor `autonomuscrm-postgres` |
| Volumen BD pruebas | `autonomuscrm_pgdata` |

## Preview

- URL: **http://164.68.99.83:8091/**
- Login: `admin@autonomuscrm.local` / `Admin123!`
- Tenant ID: ver logs `docker logs autonomuscrm-api | grep TenantId`

## Despliegue rápido (Windows)

```powershell
cd C:\Proyectos\autonomuscrm
.\deploy\deploy-vps.ps1
```

## Comandos útiles en VPS

```bash
cd /opt/autonomuscrm/deploy
docker compose -f docker-compose.vps.yml ps
docker logs -f autonomuscrm-api
docker compose -f docker-compose.vps.yml restart api workers
```

## HTTPS — crm.autonomousflow.lat

Nginx ya está configurado en el VPS. Falta **solo el registro DNS** y renovar el certificado.

### 1) DNS (panel del registrador — Namecheap / registrar-servers)

| Tipo | Host | Valor |
|------|------|--------|
| A | `crm` | `164.68.99.83` |

Esperar propagación (1–30 min). Verificar:

```bash
dig +short crm.autonomousflow.lat
# debe devolver: 164.68.99.83
```

### 2) Emitir / ampliar certificado (en VPS o desde Windows)

```powershell
cd C:\Proyectos\autonomuscrm
.\deploy\setup-https-crm.ps1
```

### 3) URLs finales

- **HTTPS:** https://crm.autonomousflow.lat/Account/Login
- **Preview IP:** http://164.68.99.83:8091/ (sigue activa)

### Credenciales prueba VPS

- Email: `admin@autonomuscrm.local`
- Password: `Admin123!`
- Tenant ID: ver `docker logs autonomuscrm-api | grep TenantId`

## Render producción

**No se toca.** Esta BD es solo para pruebas en VPS.
