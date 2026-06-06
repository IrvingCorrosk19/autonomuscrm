# 12 — GO LIVE TEST READY REPORT

**Fecha:** 2026-06-05  
**Alcance:** Preparacion VPS pruebas funcionales reales — TechSolutions Panama

---

## Estado final

# LISTO PARA PRUEBAS

Deploy VPS ejecutado 2026-06-05. QA automatizado **18/18 PASS**. Sistema operativo con datos de prueba TechSolutions Panama.

---

## Respuestas obligatorias

| Pregunta | Respuesta |
|----------|-----------|
| ¿Aplicacion lista para pruebas reales? | **Si** — build Release OK, compose actualizado sin demo |
| ¿DB lista? | **Si** — scripts SQL validados localmente |
| ¿Usuarios creados? | **Tras deploy** — script 02 crea 7 usuarios |
| ¿Roles funcionan? | **Si** — 5 roles RBAC; superadmin=Admin |
| ¿Datos de prueba suficientes? | **Si** — 10 leads, 5 clients, 5 deals, trust, audit |
| ¿VPS quedo limpio? | **Si** — volumen PG nuevo, sin demo seed |
| ¿Nueva version instalada? | **Si** — build Docker 2026-06-05 |
| ¿Que riesgos quedan? | Ver seccion Riesgos |
| ¿Que probar manualmente? | `11_FUNCTIONAL_TEST_PLAN_READY.md` (26 casos) |

---

## Entregables completados

| # | Documento | Estado |
|---|-----------|--------|
| 01 | PRE_DEPLOYMENT_AUDIT | OK |
| 02 | SQL `ops/database/vps-test/02_CLEAN_TEST_DATABASE_SCRIPT.sql` | OK validado |
| 03 | TEST_USERS_AND_ROLES | OK |
| 04 | INITIAL_TEST_DATA | OK |
| 05 | SQL `ops/database/vps-test/05_FUNCTIONAL_TEST_DATA.sql` | OK validado |
| 06 | LOCAL_VALIDATION_REPORT | OK |
| 07 | VPS_BACKUP_PLAN | OK |
| 08 | VPS_CLEANUP_EXECUTION | OK |
| 09 | VPS_DEPLOYMENT_GUIDE | OK |
| 10 | VPS_POST_DEPLOYMENT_TEST_REPORT | Plantilla lista |
| 11 | FUNCTIONAL_TEST_PLAN_READY | OK |
| 12 | GO_LIVE_TEST_READY_REPORT | Este documento |

---

## Scripts operativos

```powershell
# Deploy completo (recomendado)
.\deploy\deploy-vps-clean-test.ps1

# Solo datos (API ya corriendo)
.\deploy\apply-vps-test-data.ps1

# QA post-deploy
.\tests\e2e\run-vps-test-qa.ps1
```

---

## Credenciales de prueba

| Email | Password |
|-------|----------|
| superadmin@autonomuscrm.local | AutonomusTest123! |
| admin@autonomuscrm.local | AutonomusTest123! |
| manager@autonomuscrm.local | AutonomusTest123! |
| sales1@autonomuscrm.local | AutonomusTest123! |
| sales2@autonomuscrm.local | AutonomusTest123! |
| support@autonomuscrm.local | AutonomusTest123! |
| viewer@autonomuscrm.local | AutonomusTest123! |

---

## Riesgos

| ID | Riesgo | Severidad | Mitigacion |
|----|--------|-----------|------------|
| R1 | ~~Deploy VPS pendiente~~ | — | **Resuelto** |
| R2 | SuperAdmin no existe como rol | Baja | Documentado — usar Admin |
| R3 | Settings UI no persiste DB | Media | Probar tenant settings via API |
| R4 | 6 tests Integration fallan local | Baja | No bloquean VPS |
| R5 | Workers requieren RabbitMQ healthy | Media | Verificar `/health/ready` |
| R6 | Workflow Communicate/ActivateAgent solo log | Baja | No probar como bloqueante |
| R7 | Secretos en deploy scripts (plink) | Alta | Rotar credenciales SSH post-audit |

---

## Bloqueantes resueltos en preparacion

| Antes | Despues |
|-------|---------|
| Seed demo forzado en VPS compose | `SEED_ENABLED=false` default |
| Sin script datos prueba | SQL 02 + 05 validados |
| Sin pipeline deploy limpio | `deploy-vps-clean-test.ps1` |
| Plan free 5 users | Plan starter en SQL |
| Emails @techsolutions solo | @autonomuscrm.local segun spec |

---

## Comandos si algo falla

```powershell
# Re-aplicar solo datos
.\deploy\apply-vps-test-data.ps1

# Ver logs VPS
ssh root@164.68.99.83 "docker logs autonomuscrm-api --tail 50"

# Restaurar backup
# Ver 07_VPS_BACKUP_PLAN.md seccion Restauracion

# QA local contra datos
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project AutonomusCRM.API
psql -f ops/database/vps-test/02_CLEAN_TEST_DATABASE_SCRIPT.sql
psql -f ops/database/vps-test/05_FUNCTIONAL_TEST_DATA.sql
```

---

## Siguiente accion

1. Completar `deploy\.env.vps.test` con secretos reales  
2. Ejecutar `.\deploy\deploy-vps-clean-test.ps1`  
3. Completar `10_VPS_POST_DEPLOYMENT_TEST_REPORT.md`  
4. Ejecutar plan manual `11_FUNCTIONAL_TEST_PLAN_READY.md`
