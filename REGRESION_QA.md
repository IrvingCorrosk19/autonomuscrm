# REGRESION_QA

**Fecha:** 2026-05-27  
**Build:** AutonomusCRM.sln — 0 errores

## Matriz post-fix

| Caso original | Retest | Regresión relacionada | Resultado |
|---------------|--------|----------------------|-----------|
| SEC-V-01 | PASS | E2E-AUTH-03-Viewer, NAV Leads | PASS |
| TEN-004 | PASS | TEN-003, E2E-API-03 | PASS |
| TRZ-001 | PASS | E2E-AUD-01, E2E-AUD-02 | PASS |
| DEF-001 fix | — | E2E-L-01 crear lead Sales | PASS |
| DEF-002 fix | — | E2E-SEC-05 Sales POST Users | PASS |

## Suite completa

`run-local-e2e.ps1`: **39/39 PASS**, 0 FAIL.

## Áreas no regresadas automáticamente

- Import CSV inválido (IMP/DAT)
- Concurrencia deals simultánea
- Session expiration (SEC-SES-01)
- Pentest OWASP manual

Programar en siguiente iteración antes de SaaS multi-tenant productivo.
