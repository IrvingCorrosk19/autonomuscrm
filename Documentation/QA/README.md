# AutonomusCRM — Enterprise QA Certification Package

Paquete de certificación funcional por rol. Derivado del código fuente (`AutonomusCRM.API`, middleware, políticas RBAC).

## Entorno QA oficial

| Campo | Valor |
|-------|-------|
| URL | http://164.68.99.83:8091/Account/Login |
| Password | `AutonomusTest123!` |
| Tenant | TechSolutions Panama (`b1000000-0000-4000-8000-000000000001`) |
| TenantId login | vacío o `00000000-0000-0000-0000-000000000000` |

## Usuarios

| Rol QA | Email |
|--------|-------|
| SuperAdmin* | `superadmin@autonomuscrm.local` |
| Admin | `admin@autonomuscrm.local` |
| Manager | `manager@autonomuscrm.local` |
| Sales | `sales1@autonomuscrm.local`, `sales2@autonomuscrm.local` |
| Support | `support@autonomuscrm.local` |
| Viewer | `viewer@autonomuscrm.local` |

\*No existe rol `SuperAdmin` en RBAC; el usuario tiene rol **Admin** con máximos privilegios del tenant.

## University (capacitación Trailhead)

**Plataforma oficial:** [../University/README.md](../University/README.md) · App: `/University`

| Documento | Propósito |
|-----------|-----------|
| [AUTONOMUSCRM_UNIVERSITY_MASTER_PLAN.md](../University/AUTONOMUSCRM_UNIVERSITY_MASTER_PLAN.md) | Visión y arquitectura |
| [LEARNING_PATHS.md](../University/LEARNING_PATHS.md) | Rutas modulares |
| [QUICK_START_GUIDES.md](../University/QUICK_START_GUIDES.md) | **Entrada oficial** — 13 mini-cursos Client First |
| [CERTIFICATION_PROGRAM.md](../University/CERTIFICATION_PROGRAM.md) | 5 certificaciones |

## Índice QA técnico

| Documento | Propósito |
|-----------|-----------|
| [FUNCTIONAL_CAPABILITY_MATRIX.md](FUNCTIONAL_CAPABILITY_MATRIX.md) | Inventario funcional real |
| [SUPERADMIN_TEST_CASES.md](SUPERADMIN_TEST_CASES.md) | Casos de prueba |
| [ADMIN_TEST_CASES.md](ADMIN_TEST_CASES.md) | Casos de prueba |
| [MANAGER_TEST_CASES.md](MANAGER_TEST_CASES.md) | Casos de prueba |
| [SALES_TEST_CASES.md](SALES_TEST_CASES.md) | Casos de prueba |
| [SUPPORT_TEST_CASES.md](SUPPORT_TEST_CASES.md) | Casos de prueba |
| [VIEWER_TEST_CASES.md](VIEWER_TEST_CASES.md) | Casos de prueba |
| [ROLE_COVERAGE_MATRIX.md](ROLE_COVERAGE_MATRIX.md) | Cobertura por rol |
| [END_TO_END_SCENARIOS.md](END_TO_END_SCENARIOS.md) | Escenarios E2E |
| [ROLE_SMOKE_TESTS.md](ROLE_SMOKE_TESTS.md) | Smoke por rol (65 checks) |
| [ROLE_CERTIFICATION_MATRIX.md](ROLE_CERTIFICATION_MATRIX.md) | Matriz certificación + sign-off |
| [AUTONOMUSCRM_QA_MASTER_CERTIFICATION.md](AUTONOMUSCRM_QA_MASTER_CERTIFICATION.md) | Certificación maestra |

## Automatización

```powershell
.\tests\e2e\run-vps-test-qa.ps1
.\tests\e2e\run-rc-smoke.ps1 -ConfigPath tests\vps-test\config.vps.json
.\scripts\generate-qa-test-cases.ps1   # regenera 94 casos desde inventario
```

## Resumen del paquete

| Artefacto | Cantidad |
|-----------|----------|
| Funciones inventariadas | 95 |
| University paths + certs | ver ../University/ |
| Casos de prueba | 94 |
| Escenarios E2E | 6 |
| Smoke checks por rol | 65 |
| Cobertura Admin/SuperAdmin | ~98% |
| Cobertura Viewer | ~42% (diseño RBAC) |

## Datos de prueba precargados (VPS)

5 clientes, 10 leads, 5 deals, 8 tareas, 4 workflows, 1 policy — ver `ops/database/vps-test/05_FUNCTIONAL_TEST_DATA.sql`.
