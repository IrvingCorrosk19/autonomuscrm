# 08 — VPS CLEANUP EXECUTION

**Pre-requisito:** Backup completado (`07_VPS_BACKUP_PLAN.md`)

---

## Opcion A — Pipeline automatizado (recomendado)

```powershell
cd c:\Proyectos\autonomuscrm

# 1. Crear .env de prueba desde plantilla
copy deploy\.env.vps.test.example deploy\.env.vps.test
# Editar secretos reales en deploy\.env.vps.test

# 2. Deploy limpio completo (backup + cleanup + install + SQL)
.\deploy\deploy-vps-clean-test.ps1
```

---

## Opcion B — Comandos manuales en VPS

### 1. Detener y eliminar stack anterior

```bash
ssh root@164.68.99.83
cd /opt/autonomuscrm/deploy
docker compose -f docker-compose.vps.yml --env-file .env down -v --remove-orphans
```

### 2. Eliminar contenedores huerfanos

```bash
docker rm -f autonomuscrm-api autonomuscrm-workers autonomuscrm-postgres autonomuscrm-redis autonomuscrm-rabbitmq 2>/dev/null || true
```

### 3. Eliminar volumenes PostgreSQL (BD anterior)

```bash
docker volume rm -f deploy_autonomuscrm_pgdata autonomuscrm_pgdata deploy_autonomuscrm_dataprotection 2>/dev/null || true
docker volume ls | grep autonomus
```

### 4. Conservar (NO borrar)

- `/opt/autonomuscrm-backups/**`
- `/etc/nginx/sites-available/autonomuscrm.conf`
- `/etc/letsencrypt/**`
- Puerto 8091 en UFW
- `deploy/.env` (copiar a backup antes de reemplazar)

### 5. Limpiar solo imagenes viejas (opcional)

```bash
docker image prune -f
docker images | grep autonomuscrm
```

---

## Que se elimina vs conserva

| Eliminar | Conservar |
|----------|-----------|
| Contenedores API/Workers/PG/Redis/Rabbit | Backups |
| Volumen PG (datos demo/CEO_DEMO) | Nginx config |
| Imagenes Docker rebuild | SSL certs |
| Datos CRM anteriores | Dominio/DNS |
| Seed demo en runtime | Secretos en `.env.vps.test` |

---

## Verificacion post-cleanup

```bash
docker ps -a | grep autonomus   # vacio o solo nuevos
docker volume ls | grep autonomus  # sin pgdata hasta nuevo up
curl -sI http://164.68.99.83:8091/health  # 502 hasta nuevo deploy — esperado
```
