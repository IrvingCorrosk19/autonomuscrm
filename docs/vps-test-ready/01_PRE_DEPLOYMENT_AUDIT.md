# 01 — PRE-DEPLOYMENT AUDIT

**Fecha:** 2026-06-05  
**VPS:** 164.68.99.83:8091  
**Objetivo:** Instalacion limpia para pruebas funcionales reales (primer cliente simulado)

---

## 1. Estado actual del sistema

| Componente | Estado actual | Evidencia |
|------------|---------------|-----------|
| API | ASP.NET Core, puerto 8080 en Docker, expuesto 8091 via Nginx | `deploy/docker-compose.vps.yml` |
| Workers | Contenedor separado, RabbitMQ events | `Dockerfile.workers` |
| PostgreSQL 16 | Volumen `autonomuscrm_pgdata` | compose VPS |
| Redis 7 | Cache produccion obligatorio | `ProductionConfigurationGuard` |
| RabbitMQ 3 | Event bus produccion | compose VPS |
| Migraciones | 17 migraciones EF, AutoMigrate=true | `WebApplicationExtensions.cs` |
| Seed demo | **Era `Seed__Enabled=true` en VPS** — corregido a `${SEED_ENABLED:-false}` | compose actualizado |
| Deploy script | `deploy-vps.ps1` — backup obligatorio, down -v (borra PG) | `deploy/backup-vps.ps1` |

---

## 2. Dependencias de datos demo (eliminar en pruebas)

| Elemento | Riesgo | Accion |
|----------|--------|--------|
| `DatabaseSeeder` | Inyecta CEO_DEMO, QA-B, DemoRoleUsers | `SEED_ENABLED=false` |
| `CeoDemoSeeder` | 50+ clientes ejecutivos | No ejecutar seed |
| `DemoRoleUsers` | Password `{Role}123!` | Reemplazar por SQL test |
| Login panel demo | Solo dev + seed | Production oculto |
| `deploy-vps.ps1` mensaje demo | Confusion | Actualizado |

---

## 3. Que debe quedarse

- Stack Docker: postgres, redis, rabbitmq, api, workers
- Nginx puerto 8091, SSL/certbot paths
- Secretos: JWT, IntegrationEncryption, RabbitMQ, Postgres
- Migraciones automaticas
- Health `/health`, `/health/ready`
- Middleware: RBAC UI, Trust, Plan limits

---

## 4. Que debe eliminarse (VPS limpio)

| Item | Metodo |
|------|--------|
| Contenedores anteriores | `docker compose down -v` |
| Volumen PostgreSQL demo | `deploy_autonomuscrm_pgdata` |
| Tenants demo (CEO_DEMO, AutonomusCRM Demo) | BD nueva vacia + SQL test |
| Config seed demo | `.env` con `SEED_ENABLED=false` |

**Conservar siempre:** backups en `/opt/autonomuscrm-backups/`, Nginx, certificados, dominio.

---

## 5. Que debe inicializarse automaticamente

| Elemento | Como |
|----------|------|
| Schema BD | AutoMigrate al arrancar API |
| Tenant TechSolutions Panama | `02_CLEAN_TEST_DATABASE_SCRIPT.sql` |
| 7 usuarios + roles | Mismo script |
| Plan starter (10 users) | TenantBillingAccounts en script |
| Workflows 2+2 | Script 02 |
| Policy base | Script 02 |
| Datos CRM | `05_FUNCTIONAL_TEST_DATA.sql` |
| Trust threshold 70 | Tenant Settings JSON |

---

## 6. SuperAdmin — aclaracion critica

**No existe rol SuperAdmin en codigo.**  
`superadmin@autonomuscrm.local` se crea con rol **Admin** (maximo privilegio RBAC).

Roles reales: Admin, Manager, Sales, Support, Viewer.

---

## 7. Que puede romper instalacion limpia

| Bloqueante | Mitigacion |
|------------|------------|
| Secretos faltantes en `.env` | `deploy/.env.vps.test.example` |
| `Seed__Enabled=true` | `SEED_ENABLED=false` |
| Sin datos post-migrate | `apply-vps-test-data.ps1` |
| Plan free 5 usuarios | Plan starter en SQL |
| Email Log con AllowSimulation=false | `COMMS_ALLOW_SIMULATION=true` |
| Deploy sin backup | `backup-vps.ps1` obligatorio |
| Emails duplicados multi-tenant | VPS con BD nueva un solo tenant |

---

## 8. Archivos preparados para deploy pruebas

| Archivo | Proposito |
|---------|-----------|
| `ops/database/vps-test/02_CLEAN_TEST_DATABASE_SCRIPT.sql` | Tenant + usuarios + workflows |
| `ops/database/vps-test/05_FUNCTIONAL_TEST_DATA.sql` | Leads, deals, trust, audit |
| `deploy/apply-vps-test-data.ps1` | Ejecutar SQL en VPS |
| `deploy/deploy-vps-clean-test.ps1` | Pipeline completo |
| `deploy/.env.vps.test.example` | Plantilla env sin demo |
| `tests/e2e/run-vps-test-qa.ps1` | QA automatizado post-deploy |
