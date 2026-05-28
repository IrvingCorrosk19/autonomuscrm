# RESULTADOS_EJECUCION_AUTONOMUSFLOW

**Fecha:** 2026-05-27  
**Entorno:** http://localhost:5154  
**Fase:** 2 — Ejecución real QA + estabilización

---

## Resumen ejecutivo de ejecución

| Suite | Casos | PASS | FAIL | BLOCKED | SKIP |
|-------|:-----:|:----:|:----:|:-------:|:----:|
| P0 (`run-p0-qa.ps1`) | 19 | 19 | 0 | 0 | 0 |
| Regresión local (`run-local-e2e.ps1`) | 39 | 39 | 0 | 0 | 0 |

---

## P0 — Orden obligatorio (post-fixes)

| ID | Estado | Nota |
|----|:------:|------|
| AUTH-001 … AUTH-006 | PASS | 5 roles + credencial inválida |
| SEC-S-01 | PASS | Anónimo → Login |
| SEC-V-01 | PASS | Viewer → AccessDenied en `/Leads/Create` |
| SEC-S-02 | PASS | Sales → AccessDenied en `/Users` |
| API-001 | PASS | `/health` |
| API-002 / API-003 | PASS | JWT + GET leads |
| TEN-003 | PASS | Cross-tenant query → HTTP 403 |
| TEN-004 | PASS | `tenantId` ajeno en API → HTTP 403 |
| E2E-001-L | PASS | Crear lead UI |
| TRZ-001 | PASS | Audit sin KPIs/fila demo hardcoded |
| NAV-L/C/D-01 | PASS | HTTP 200 |

Evidencia: `tests/qa-evidence/2026-05-27/p0-results-20260527203827.csv`

---

## Regresión ampliada (muestra)

| Área | IDs ejecutados | Estado |
|------|----------------|--------|
| Auth 5 roles | E2E-AUTH-03-* | PASS |
| Lead → Customer → Deal | E2E-L-*, E2E-D-*, FLUJO-01 | PASS |
| RBAC API | E2E-SEC-05 | PASS |
| Navegación | E2E-NAV/* | PASS |
| Auditoría | E2E-AUD-01, E2E-AUD-02 | PASS |

CSV: `tests/e2e/results-local-*.csv` (última ejecución en sesión)

---

## Casos NO ejecutados en esta sesión (catálogo 118)

Prioridad siguiente: IMP-*, PROC-GAP (SKIP documentado), AUT-*, concurrencia multiusuario manual, pentest OWASP completo.

---

## Estado catálogo P0 (referencia CASOS_PRUEBA)

| ID | Estado Fase 2 |
|----|---------------|
| AUTH-001 … 006 | PASS |
| SEC-V-01 / SEC-S-01 | PASS |
| E2E-001 (crear lead) | PASS (parcial flujo completo en FLUJO-01) |
| TEN-003 / TEN-004 | PASS |
| API-001 (health; JWT inválido no re-ejecutado aisladamente) | PASS health |
| TRZ-001 | PASS |

---

## Bloqueadores resueltos en sesión

Ver `FIXES_APLICADOS.md` y `ERRORES_QA.md` (DEF-001 … DEF-005 CLOSED).
