# 09 — VPS DEPLOYMENT GUIDE

**URL final:** http://164.68.99.83:8091  
**Directorio remoto:** `/opt/autonomuscrm`

---

## Paso 1 — Preparar entorno local

```powershell
cd c:\Proyectos\autonomuscrm
dotnet build -c Release
dotnet test AutonomusCRM.Tests -c Release --filter "Category!=Integration"
```

---

## Paso 2 — Configurar secretos de prueba

```powershell
copy deploy\.env.vps.test.example deploy\.env.vps.test
```

Editar `deploy\.env.vps.test`:

```env
POSTGRES_PASSWORD=<fuerte>
RABBITMQ_PASSWORD=<fuerte>
JWT_KEY=<minimo-32-caracteres>
INTEGRATION_ENCRYPTION_KEY=<base64-32-bytes>
PROVISIONING_API_KEY=<bootstrap-secret>
SEED_ENABLED=false
AI_ENABLED=false
COMMS_ALLOW_SIMULATION=true
```

---

## Paso 3 — Deploy completo

```powershell
.\deploy\deploy-vps-clean-test.ps1
```

Esto ejecuta:
1. `backup-vps.ps1`
2. Copia `.env.vps.test` → `.env.vps`
3. `deploy-vps.ps1` (build, up, nginx)
4. `apply-vps-test-data.ps1` (SQL 02 + 05)

---

## Paso 4 — Verificar servicios

```bash
ssh root@164.68.99.83
docker compose -f /opt/autonomuscrm/deploy/docker-compose.vps.yml ps
docker logs autonomuscrm-api --tail 30
docker logs autonomuscrm-workers --tail 20
curl -s http://127.0.0.1:5080/health
curl -s http://127.0.0.1:5080/health/ready
```

---

## Paso 5 — Verificar datos

```bash
docker exec autonomuscrm-postgres psql -U postgres -d autonomuscrm -c \
  "SELECT COUNT(*) FROM \"Users\" WHERE \"TenantId\"='b1000000-0000-4000-8000-000000000001';"
# Esperado: 7
```

---

## Paso 6 — QA automatizado

```powershell
.\tests\e2e\run-vps-test-qa.ps1
```

---

## Paso 7 — Login manual

| URL | http://164.68.99.83:8091/Account/Login |
| User | superadmin@autonomuscrm.local |
| Pass | AutonomusTest123! |

---

## Re-deploy solo datos (sin rebuild)

```powershell
.\deploy\apply-vps-test-data.ps1
```

---

## Troubleshooting

| Problema | Solucion |
|----------|----------|
| API no arranca | `docker logs autonomuscrm-api` — validar JWT/Encryption |
| 502 Nginx | Esperar migraciones; verificar puerto 5080 |
| Login falla | Verificar SQL aplicado; un solo tenant |
| 403 crear usuario | Plan starter en SQL (10 users) |
| Workers sin eventos | `docker logs autonomuscrm-rabbitmq` |

---

## Archivos clave

| Archivo | Rol |
|---------|-----|
| `deploy/docker-compose.vps.yml` | Stack produccion |
| `deploy/.env.vps.test` | Secretos prueba |
| `ops/database/vps-test/02_*.sql` | Base tenant+users |
| `ops/database/vps-test/05_*.sql` | Datos funcionales |
| `deploy/nginx-autonomuscrm-vps.conf` | Proxy 8091→5080 |
