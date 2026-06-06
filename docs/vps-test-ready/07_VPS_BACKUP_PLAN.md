# 07 — VPS BACKUP PLAN

**OBLIGATORIO antes de eliminar cualquier cosa del VPS.**

---

## Script automatizado

```powershell
cd c:\Proyectos\autonomuscrm
.\deploy\backup-vps.ps1
```

Crea backup en: `/opt/autonomuscrm-backups/YYYYMMDD-HHMMSS/`

---

## Contenido del backup

| Artefacto | Ruta backup | Comando base |
|-----------|-------------|--------------|
| PostgreSQL | `db/autonomuscrm.dump` | `pg_dump -Fc` via docker exec |
| Validacion dump | `db/restore-list.txt` | `pg_restore --list` |
| Aplicacion | `app/autonomuscrm-app.tar.gz` | tar `/opt/autonomuscrm` |
| `.env` | `config/.env` | cp |
| docker-compose | `config/docker-compose.vps.yml` | cp |
| docker ps | `config/docker-ps.txt` | snapshot |
| Nginx | `nginx/autonomuscrm.conf` | cp |
| SSL | `ssl/letsencrypt.tar.gz` | tar `/etc/letsencrypt` |
| Checksums | `CHECKSUMS.sha256` | sha256sum |

---

## Comandos manuales (referencia)

### Backup PostgreSQL

```bash
ssh root@164.68.99.83
docker exec autonomuscrm-postgres pg_dump -U postgres -d autonomuscrm -Fc -f /tmp/autonomuscrm.dump
docker cp autonomuscrm-postgres:/tmp/autonomuscrm.dump /opt/autonomuscrm-backups/manual/autonomuscrm.dump
```

### Backup volumen Docker (alternativa)

```bash
docker run --rm -v deploy_autonomuscrm_pgdata:/data -v /opt/autonomuscrm-backups:/backup alpine \
  tar czf /backup/pgdata-volume.tar.gz -C /data .
```

### Backup .env

```bash
cp /opt/autonomuscrm/deploy/.env /opt/autonomuscrm-backups/manual/.env.$(date +%Y%m%d)
```

### Backup Nginx

```bash
cp /etc/nginx/sites-available/autonomuscrm.conf /opt/autonomuscrm-backups/manual/
nginx -T > /opt/autonomuscrm-backups/manual/nginx-full.conf
```

### Backup logs recientes

```bash
docker logs autonomuscrm-api --tail 500 > /opt/autonomuscrm-backups/manual/api.log
docker logs autonomuscrm-workers --tail 500 > /opt/autonomuscrm-backups/manual/workers.log
docker logs autonomuscrm-postgres --tail 200 > /opt/autonomuscrm-backups/manual/postgres.log
```

---

## Verificacion post-backup

1. `ls -lh /opt/autonomuscrm-backups/*/db/autonomuscrm.dump` — tamano > 0
2. `pg_restore --list` sin errores
3. `BACKUP_MANIFEST.txt` contiene `BACKUP_OK`
4. Guardar checksums localmente

---

## Restauracion (si necesario)

```bash
docker exec -i autonomuscrm-postgres pg_restore -U postgres -d autonomuscrm --clean --if-exists < autonomuscrm.dump
```

Solo ejecutar si el deploy de pruebas falla y se requiere rollback.
