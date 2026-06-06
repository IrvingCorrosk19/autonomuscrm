# 10 — VPS POST-DEPLOYMENT TEST REPORT

**Deploy ejecutado 2026-06-05**

---

## Informacion del deploy

| Campo | Valor |
|-------|-------|
| Fecha deploy | 2026-06-05 06:51 UTC |
| Backup previo | `/opt/autonomuscrm-backups/20260605-065104` |
| Backup adicional | `/opt/autonomuscrm-backups/20260605-065133` |
| URL | http://164.68.99.83:8091 |
| QA automatizado | **18/18 PASS** |

---

## Checklist infraestructura

| # | Prueba | Comando | Esperado | Resultado | Estado |
|---|--------|---------|----------|-----------|--------|
| 1 | API health | `curl /health` | Healthy | | |
| 2 | Ready check | `curl /health/ready` | PG+Redis+Rabbit OK | | |
| 3 | PostgreSQL | `docker exec ... psql` | Conecta | | |
| 4 | Redis | health ready | OK | | |
| 5 | RabbitMQ | health ready | OK | | |
| 6 | Workers running | `docker ps` | Up | | |
| 7 | Nginx 8091 | `curl -I :8091` | 200/302 | | |
| 8 | Logs API sin FTL | `docker logs api` | Sin crash | | |
| 9 | Logs Workers | `docker logs workers` | Sin error loop | | |
| 10 | Seed desactivado | env `SEED_ENABLED=false` | Sin CEO_DEMO | | |

---

## Checklist funcional

| # | Prueba | Usuario | Esperado | Resultado | Estado |
|---|--------|---------|----------|-----------|--------|
| 11 | Login superadmin | superadmin@ | /executive | | |
| 12 | Login admin | admin@ | /executive | | |
| 13 | Login manager | manager@ | /executive | | |
| 14 | Login sales1 | sales1@ | /revenue | | |
| 15 | Login support | support@ | /Customer360 | | |
| 16 | Login viewer | viewer@ | / | | |
| 17 | Admin crea usuario | admin@ | 201/redirect Users | | |
| 18 | Sales crea lead | sales1@ | Lead en lista | | |
| 19 | Support bloqueado write | support@ | AccessDenied | | |
| 20 | Viewer bloqueado write | viewer@ | AccessDenied | | |
| 21 | Executive OS datos | manager@ | KPIs > 0 | | |
| 22 | Revenue OS pipeline | sales1@ | 5 deals | | |
| 23 | Trust Studio | admin@ | 1 pending HITL | | |
| 24 | Audit eventos | admin@ | >= 3 eventos | | |
| 25 | Billing page | admin@ | Plan starter | | |
| 26 | Integrations | admin@ | Empty OK | | |
| 27 | Workflows activos | admin@ | 2 activos | | |
| 28 | QA automatizado | run-vps-test-qa.ps1 | PASS=18 FAIL=0 | | |

---

## Conteos BD esperados

```
Users: 7 | Customers: 5 | Leads: 10 | Deals: 5
WorkflowTasks: 8 | AiDecisionAudits: 2 | Workflows: 4 (2 active)
```

---

## Resultado post-deploy

| Estado | Criterio |
|--------|----------|
| LISTO | >= 25/28 checklist PASS |
| RIESGOS | 20-24 PASS o workers degradados |
| NO LISTO | Login falla o BD vacia |

**Estado actual:** **LISTO PARA PRUEBAS** — health OK, 7 usuarios, datos CRM cargados, QA 18/18
