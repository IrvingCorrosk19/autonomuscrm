# 06 — LOCAL VALIDATION REPORT

**Fecha:** 2026-06-05  
**Entorno:** Windows, PostgreSQL 18 local, API :5154

---

## Comandos ejecutados

| Comando | Resultado |
|---------|-----------|
| `dotnet build AutonomusCRM.sln -c Release` | **0 errores** (24 warnings) |
| `dotnet test -c Release --filter Category!=Integration` | **195 pass / 6 fail** |
| `ops/database/vps-test/02_CLEAN_TEST_DATABASE_SCRIPT.sql` | **OK** — 1 tenant, 7 users, 4 workflows |
| `ops/database/vps-test/05_FUNCTIONAL_TEST_DATA.sql` | **OK** — 5 cust, 10 leads, 5 deals, 8 tasks, 2 trust |

---

## Tests fallidos (esperado sin Docker/Integration)

| Test | Causa |
|------|-------|
| Phase4OperationalValidationTests (6) | Requiere factory/integration host |

No bloquean deploy VPS.

---

## Validacion SQL local — conteos

```
Tenants:          1
Users:            7
Workflows:        4
Customers:        5
Leads:           10
Deals:            5
WorkflowTasks:    8
AiDecisionAudits:   2
DomainEvents:     3
```

---

## Login / permisos (local)

| Prueba | Resultado |
|--------|-----------|
| superadmin@ login → /executive | OK |
| sales1@ login → /revenue | OK |
| sales1@ crear lead | OK |
| Admin navegacion Leads/Users/Audit | OK |
| admin@ duplicado multi-tenant local | FAIL esperado (emails en tenants viejos) |

En **VPS con BD nueva** (un solo tenant) todos los logins deben pasar.

---

## Workers / RabbitMQ local

No validado en local (EventBus InMemory en Development). En VPS: Workers + RabbitMQ obligatorios — validar post-deploy.

---

## Correcciones aplicadas en esta fase

1. `docker-compose.vps.yml` — `SEED_ENABLED=false` default, AI/Comms modo prueba
2. SQL scripts — UUIDs hex validos, Customers sin AssignedToUserId
3. `02` — re-ejecutable por nombre tenant
4. Scripts deploy: `apply-vps-test-data.ps1`, `deploy-vps-clean-test.ps1`

---

## Pendiente (solo VPS)

- [ ] Ejecutar `deploy-vps-clean-test.ps1`
- [ ] `run-vps-test-qa.ps1` contra :8091
- [ ] Verificar workers logs sin errores criticos

---

## Veredicto local

**LISTO PARA VPS** — build OK, SQL validado, scripts documentados. Deploy VPS pendiente de ejecucion manual.
