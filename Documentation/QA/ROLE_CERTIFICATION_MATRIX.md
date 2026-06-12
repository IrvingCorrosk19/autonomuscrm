# ROLE_CERTIFICATION_MATRIX

Registro de certificación por rol. Completar durante ejecución QA humana.

**Fecha inicio:** ___________ | **QA Lead:** ___________

## Resumen ejecutivo

| Rol | Casos | Ejecutados | PASS | FAIL | BLOCKED | Cobertura | Certificado |
|-----|-------|------------|------|------|---------|-----------|-------------|
| SuperAdmin | 20 | | | | | 98% | ☐ |
| Admin | 20 | | | | | 98% | ☐ |
| Manager | 15 | | | | | 94% | ☐ |
| Sales | 15 | | | | | 82% | ☐ |
| Support | 12 | | | | | 68% | ☐ |
| Viewer | 12 | | | | | 42% | ☐ |
| **E2E Scenarios** | 6 | | | | | — | ☐ |
| **Smoke global** | 23 | | | | | — | ☐ |
| **TOTAL** | **94** | | | | | | ☐ |

## Automatización pre-ejecutada (2026-06-06)

| Suite | Resultado | Evidencia |
|-------|-----------|-----------|
| `run-vps-test-qa.ps1` | 18/18 PASS | `tests/qa-evidence/first-client/` |
| `run-rc-smoke.ps1` (VPS) | 23/23 PASS | `tests/qa-evidence/rc-zero/` |

## Criterios de certificación por rol

| Rol | Mínimo PASS | Smoke | E2E obligatorios |
|-----|-------------|-------|------------------|
| SuperAdmin | 18/20 (90%) | 15/15 | E2E-02, E2E-05, E2E-06 |
| Admin | 18/20 (90%) | 13/13 | E2E-01, E2E-05 |
| Manager | 13/15 (87%) | 10/10 | E2E-04, E2E-06 |
| Sales | 13/15 (87%) | 10/10 | E2E-01 |
| Support | 10/12 (83%) | 10/10 | E2E-03 |
| Viewer | 10/12 (83%) | 10/10 | E2E-06 (bloqueos) |

## Registro de fallos

| ID Caso | Rol | Severidad | Descripción | Estado |
|---------|-----|-----------|-------------|--------|
| | | P0/P1/P2 | | Open/Fixed/Wontfix |

## Sign-off

| Rol certificado | QA | Fecha | Firma |
|-----------------|-----|-------|-------|
| SuperAdmin | | | |
| Admin | | | |
| Manager | | | |
| Sales | | | |
| Support | | | |
| Viewer | | | |
| **Release** | | | |
