# ROLE_COVERAGE_MATRIX — Cobertura funcional por rol

Basado en **95 funciones** inventariadas en `FUNCTIONAL_CAPABILITY_MATRIX.md`.

## Metodología

| Métrica | Definición |
|---------|------------|
| **Cobertura rol** | % funciones Full/Partial accesibles (lectura o escritura) para el rol |
| **Cobertura escritura** | % funciones con permiso POST/Create/Edit |
| **Casos QA** | Casos en `*_TEST_CASES.md` por rol |

## Matriz de acceso por módulo

| Módulo | Funciones | SuperAdmin | Admin | Manager | Sales | Support | Viewer |
|--------|-----------|:----------:|:-----:|:-------:|:-----:|:-------:|:------:|
| Command Center | 13 | 100% | 100% | 100% | 100% | 100% | 100% |
| Revenue/Executive/Deals | 14 | 100% | 100% | 100% | 100% | 43% read | 43% read |
| Leads | 10 | 100% | 100% | 100% | 100% | 40% read | 40% read |
| Customers | 9 | 100% | 100% | 100% | 100% | 44% read | 44% read |
| Customer360/CS | 9 | 100% | 100% | 100% | 89% | 100% | 67% |
| Workflows/Policies | 12 | 100% | 100% | 100% | 100% | 17% read | 17% read |
| Admin Platform | 18 | 100% | 100% | 72% | 28% | 22% | 22% |
| API (tenant) | 10 | 100% | 100% | 70% | 50% | 30% | 20% |

## Cobertura consolidada (%)

| Rol | Lectura | Escritura | Casos QA | Smoke | **Cobertura total estimada** |
|-----|---------|-----------|----------|-------|------------------------------|
| **SuperAdmin** | 95/95 | 86/86 | 20 | 23/23 | **98%** |
| **Admin** | 95/95 | 86/86 | 20 | 23/23 | **98%** |
| **Manager** | 95/95 | 86/86 | 15 | 22/23 | **94%** |
| **Sales** | 88/95 | 52/86 | 15 | 18/23 | **82%** |
| **Support** | 62/95 | 12/86 | 12 | 14/23 | **68%** |
| **Viewer** | 58/95 | 0/86 | 12 | 12/23 | **42%** |

> **Nota:** Cobertura 100% del sistema requiere ejecutar los **94 casos** + **5 escenarios E2E** + validación API. Ningún rol individual alcanza 100% por diseño RBAC.

## Gaps por rol (no cubierto por diseño)

| Rol | No cubre (esperado) |
|-----|-------------------|
| Sales | Users, Settings, API RequireAdmin, Provisioning |
| Support | Commercial Create/Edit, Users, Settings, Workflows write |
| Viewer | Toda escritura, Users, Settings |
| Manager | API `POST /api/users`, Provisioning platform |

## Gaps sistema (requieren Admin + API)

| Función | Estado | Rol mínimo |
|---------|--------|------------|
| Billing checkout UI | Partial | Admin (API) |
| Identity merge UI | Partial | Admin (API) |
| Playbooks/Outcomes write | Partial | Admin |
| VoiceCalls full CTI | MVP | Admin |
| Marketing pages | MVP | Anonymous |

---

*Actualizar tras ejecución humana en `ROLE_CERTIFICATION_MATRIX.md`.*
