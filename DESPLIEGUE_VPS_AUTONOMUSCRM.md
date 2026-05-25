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

## Dominio futuro (opcional)

`crm.autonomousflow.lat` → A record → `164.68.99.83` + bloque HTTPS en nginx (igual que Madison).

## Render producción

**No se toca.** Esta BD es solo para pruebas en VPS.
