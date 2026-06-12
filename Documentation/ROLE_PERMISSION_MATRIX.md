# ROLE_PERMISSION_MATRIX — Matriz Global de Permisos

**Roles:** Admin, Manager, Sales, Support, Viewer  
**Leyenda:** ✅ Permitido · ❌ Denegado · 👁 Lectura · ⚠️ Brecha (API sin filtro rol)

---

## 1. Módulos × Rol

| Módulo | Ruta | Admin | Manager | Sales | Support | Viewer |
|--------|------|:-----:|:-------:|:-----:|:-------:|:------:|
| Command Center | `/` | ✅ | ✅ | ✅ | ✅ | ✅ |
| Trust Studio | `/TrustInbox` | ✅ | ✅ | 👁 | 👁 | 👁 |
| Workforce | `/Agents` | ✅ | ✅ | 👁 | 👁 | 👁 |
| Revenue OS | `/revenue` | ✅ | ✅ | ✅ | 👁 | 👁 |
| Executive OS | `/executive` | ✅ | ✅ | 👁 | 👁 | 👁 |
| Pipeline / Deals | `/Deals` | ✅ | ✅ | ✅ | 👁 | 👁 |
| Customers Directory | `/Customers` | ✅ | ✅ | ✅ | 👁 | 👁 |
| Customer 360 | `/Customer360` | ✅ | ✅ | 👁 | ✅ | 👁 |
| Customer Success | `/customer-success` | ✅ | ✅ | 👁 | ✅ | 👁 |
| Leads | `/Leads` | ✅ | ✅ | ✅ | 👁 | 👁 |
| Memory | `/Memory` | ✅ | ✅ | 👁 | 👁 | 👁 |
| Tasks | `/Tasks` | ✅ | ✅ | ✅ | ✅ | 👁 |
| Integrations | `/Integrations` | ✅ | ✅ | 👁 | 👁 | 👁 |
| Voice Calls | `/VoiceCalls` | ✅ | ✅ | ✅ | 👁 | 👁 |
| Users | `/Users` | ✅ | ✅ | ❌ | ❌ | ❌ |
| Policies (ABAC) | `/Policies` | ✅ | ✅ | ✅* | 👁 | 👁 |
| Audit | `/Audit` | ✅ | ✅ | 👁 | 👁 | 👁 |
| Settings | `/Settings` | ✅ | ✅ | ❌ | ❌ | ❌ |
| Billing | `/billing` | ✅ | ✅ | 👁 | 👁 | 👁 |
| Workflows | `/Workflows` | ✅ | ✅ | ✅ | 👁 | 👁 |
| Failed Events | `/FailedEvents` | ✅ | ✅ | 👁 | 👁 | 👁 |

\*Sales puede escribir Policies vía middleware comercial; operación típica es Admin/Manager.

---

## 2. Acciones comerciales × Rol

| Acción | Admin | Manager | Sales | Support | Viewer |
|--------|:-----:|:-------:|:-----:|:-------:|:------:|
| Crear Lead (UI) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Editar Lead (UI) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Qualify Lead | ✅ | ✅ | ✅ | ❌ | ❌ |
| Convert Lead | ✅ | ✅ | ✅ | ❌ | ❌ |
| Delete Lead | ✅ | ✅ | ✅ | ❌ | ❌ |
| Import Leads | ✅ | ✅ | ✅ | ❌ | ❌ |
| Crear Customer (UI) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Editar Customer (UI) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Crear Deal (UI) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Editar Deal / etapa | ✅ | ✅ | ✅ | ❌ | ❌ |
| Close / Lose Deal | ✅ | ✅ | ✅ | ❌ | ❌ |
| Import Deals | ✅ | ✅ | ✅ | ❌ | ❌ |
| Completar Task | ✅ | ✅ | ✅ | ✅ | 👁 |
| Crear Task manual | ✅ | ✅ | ✅ | ⚠️ | ❌ |

---

## 3. API REST × Rol

| Endpoint | Admin | Manager | Sales | Support | Viewer |
|----------|:-----:|:-------:|:-----:|:-------:|:------:|
| `POST /api/tenants` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `POST /api/users` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `POST /api/leads` | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |
| `POST /api/customers` | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |
| `POST /api/deals` | ✅ | ✅ | ✅ | ⚠️ | ⚠️ |
| `GET /api/ai/*` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `GET /api/flow/search` | ✅ | ✅ | ✅ | ✅ | ✅ |

⚠️ = Autenticado sin verificación de rol (brecha documentada).

---

## 4. Administración × Rol

| Acción | Admin | Manager | Sales | Support | Viewer |
|--------|:-----:|:-------:|:-----:|:-------:|:------:|
| Crear usuario (UI) | ✅ | ✅ | ❌ | ❌ | ❌ |
| Asignar roles (UI) | ✅ | ✅ | ❌ | ❌ | ❌ |
| Settings tenant | ✅ | ✅ | ❌ | ❌ | ❌ |
| MFA configuración | ✅ | ✅ | ❌ | ❌ | ❌ |
| AI kill-switch | ✅ | ✅ | ❌ | ❌ | ❌ |
| Integrations OAuth | ✅ | ✅ | ❌ | ❌ | ❌ |
| Audit export | ✅ | ✅ | ❌ | ❌ | ❌ |
| Billing | ✅ | ✅ | ❌ | ❌ | ❌ |
| Aprobar Trust (HITL) | ✅ | ✅ | ❌ | ❌ | ❌ |

---

## 5. Restricciones globales

| Restricción | Aplica a |
|-------------|----------|
| Tenant isolation | Todos — solo datos de su `TenantId` |
| Plan limits | `PlanLimitMiddleware` — según suscripción |
| Usuario inactivo | Login rechazado |
| Commercial write middleware | Bloquea Support/Viewer en POST y Create/Edit comercial |
| RequireAdmin policy | Solo Admin en tenants/users API |

---

## 6. Home redirect

| Rol | Ruta |
|-----|------|
| Admin | `/executive` |
| Manager | `/executive` |
| Sales | `/revenue` |
| Support | `/Customer360` |
| Viewer | `/` |

**Fuente:** `RoleHomeRedirect.cs`

---

*Matriz derivada de `CommercialWriteAuthorizationMiddleware.cs`, `[Authorize(Roles)]` en páginas, controllers API y `03_ROLE_MATRIX.md`.*
