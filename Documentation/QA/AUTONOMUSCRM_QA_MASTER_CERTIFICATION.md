# AUTONOMUSCRM — QA MASTER CERTIFICATION

**Programa:** Enterprise QA Certification  
**Versión:** RC Zero + VPS Test Ready  
**Fecha documentación:** 2026-06-06  
**Entorno:** http://164.68.99.83:8091

---

## 1. Declaración de alcance

Este paquete certifica **funcionalidad implementada** en `AutonomusCRM.API` derivada del código fuente, no aspiraciones de roadmap.

| Área | Funciones inventariadas | Implementación Full |
|------|-------------------------|---------------------|
| UI Razor Pages | 63 PageModels | 86/95 (91%) |
| API Controllers | 35 controllers | 9/10 core Full |
| Roles RBAC | 5 roles | 100% definidos |
| Idiomas | en, es, es-PA | 1411 keys |

---

## 2. ¿Está probado el 100% del sistema?

| Pregunta | Respuesta |
|----------|-----------|
| ¿100% automatizado? | **No** — 94 casos + 6 E2E requieren ejecución humana parcial |
| ¿100% inventariado? | **Sí** — `FUNCTIONAL_CAPABILITY_MATRIX.md` (95 funciones) |
| ¿100% por un solo rol? | **No** — por diseño RBAC (Viewer 42%, Admin 98%) |
| ¿Smoke automatizado PASS? | **Sí** — 23/23 rutas + 18/18 role QA (2026-06-06) |
| ¿Listo para QA humano sin dev? | **Sí** — manuales, casos, datos seed, VPS activo |

**Veredicto pre-certificación:** **READY FOR HUMAN QA CERTIFICATION** — certificación final pendiente sign-off en `ROLE_CERTIFICATION_MATRIX.md`.

---

## 3. Módulos sin cobertura completa

| Módulo | Gap | Tipo |
|--------|-----|------|
| Billing checkout | Sin botón UI Stripe | Partial — API only |
| Identity merge C360 | Sin botón merge UI | Partial — API only |
| Playbooks / Outcomes | Solo lectura | Partial |
| VoiceCalls | Log manual MVP | Partial |
| Marketing (landing, pricing) | Static MVP | Fuera scope CRM ops |
| Enterprise SAML/SCIM | Requiere IdP externo | Integration |
| OAuth Integrations live | Requiere client secrets | Integration |

---

## 4. Rutas no en sidebar (probar manualmente)

| Ruta | Rol mínimo | Prioridad |
|------|------------|-----------|
| `/Workflows` | CW | P0 |
| `/FailedEvents` | Admin | P1 |
| `/command/decisions` | All | P1 |
| `/command/playbooks` | All | P2 |
| `/flow/components` | Dev | P3 |
| `/customers/{id}/360` | All | P0 |

---

## 5. Formularios — cobertura

| Formulario | Create | Edit | Delete | Import | Rol write |
|------------|:------:|:----:|:------:|:------:|-----------|
| Leads | ✅ | ✅ | ✅ | ✅ | CW |
| Customers | ✅ | ✅ | ✅ | ✅ | CW |
| Deals | ✅ | ✅ | ✅ | ✅ | CW |
| Users | ✅ | ✅ | — | ✅ | AM |
| Workflows | ✅ | ✅ | ✅ | ✅ | CW |
| Policies | ✅ | ✅ | ✅ | ✅ | CW |
| Settings | — | ✅ | — | ✅ | AM |
| Customer Success | ✅ | — | — | — | All |
| Trust | — | — | — | — | All (actions) |
| VoiceCalls | ✅ | — | — | — | All |

---

## 6. Modales — cobertura

| Modal | Ubicación | Probado auto | Manual |
|-------|-----------|:------------:|:------:|
| addTrigger/Condition/Action | Workflows/Edit | ☐ | TC-SA-008 |
| updateProbability/Stage | Deals/Details | ☐ | TC-SALES-008 |
| closeDeal/loseDeal | Deals/Details | ☐ | TC-ADM-018 |
| createDealModal | Leads/Customers Details | ☐ | TC-SALES-005 |
| importModal | Leads/Customers/Deals/Users | ☐ | TC-ADM-004 |
| bulkActionsModal | Leads/Customers/Deals/Users | ☐ | TC-MGR-014 |
| eventDetailsModal | Audit | ☐ | TC-SA-010 |
| importConfigModal | Settings | ☐ | TC-ADM-012 |
| flow-palette | Layout Ctrl+K | ☐ | TC-ADM-013 |
| flow-drawer | List pages | ☐ | Smoke |

---

## 7. Workflows — cobertura

| Workflow seed | Activo | TC relacionado |
|---------------|:------:|----------------|
| Auto-asignar lead nuevo | ✅ | E2E-02 |
| Tarea seguimiento deal | ✅ | E2E-02 |
| Workflow inactivo email | ❌ | TC-SA-008 |
| Workflow inactivo churn | ❌ | — |

Acciones modal: Assign, UpdateStatus, CreateTask — validar en `Workflows/Edit`.

---

## 8. Dashboards — cobertura

| Dashboard | Ruta | Roles | Smoke |
|-----------|------|-------|:-----:|
| Command Center | `/` | All | ✅ |
| Executive OS | `/executive` | All | ✅ |
| Revenue OS | `/revenue` | All | ✅ |
| Customer360 | `/Customer360` | All | ✅ |
| Customer Success | `/customer-success` | All | ✅ |
| Memory | `/Memory` | All | ✅ |
| Billing usage | `/billing` | All | ✅ |
| Trust Studio | `/TrustInbox` | All | ✅ |
| Agents/Workforce | `/Agents` | All | ✅ |

---

## 9. Paquete entregado

| Documento | Ubicación |
|-----------|-----------|
| Inventario funcional | `FUNCTIONAL_CAPABILITY_MATRIX.md` |
| Guías Academy × 6 roles | `../Academy/Guides/*_GUIDE.md` |
| Casos × 6 roles | `*_TEST_CASES.md` (94 casos) |
| Cobertura | `ROLE_COVERAGE_MATRIX.md` |
| E2E × 6 | `END_TO_END_SCENARIOS.md` |
| Smoke × 6 roles | `ROLE_SMOKE_TESTS.md` |
| Certificación | `ROLE_CERTIFICATION_MATRIX.md` |
| Índice | `README.md` |
| Handoff operativo | `../../QA_HANDOFF_READY.md` (repo root) |

## Scripts regeneración

```powershell
.\scripts\generate-qa-test-cases.ps1
.\tests\e2e\run-vps-test-qa.ps1
.\tests\e2e\run-rc-smoke.ps1 -ConfigPath tests\vps-test\config.vps.json
```

---

## 10. Certificación final

| Gate | Estado |
|------|--------|
| Inventario funcional real | ✅ COMPLETE |
| Manuales 6 roles | ✅ COMPLETE |
| Casos prueba 94 | ✅ COMPLETE |
| Matrices cobertura/cert | ✅ COMPLETE |
| E2E scenarios 6 | ✅ COMPLETE |
| Smoke por rol | ✅ DOCUMENTED |
| Ejecución humana 94 casos | ⏳ PENDING |
| Sign-off ROLE_CERTIFICATION | ⏳ PENDING |

**Estado:** **QA PACKAGE COMPLETE — HUMAN EXECUTION REQUIRED FOR FINAL CERTIFICATION**

---

*AutonomusCRM Enterprise QA Certification Program — derivado de código fuente, no documentación genérica.*
