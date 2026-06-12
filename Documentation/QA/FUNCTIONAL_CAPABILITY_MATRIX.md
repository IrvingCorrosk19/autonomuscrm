# FUNCTIONAL_CAPABILITY_MATRIX — AutonomusCRM

Inventario derivado del código fuente (`AutonomusCRM.API`).  
**Entorno QA:** http://164.68.99.83:8091 | **Password:** `AutonomusTest123!`

**Leyenda Estado:** `Full` = implementado E2E UI | `Partial` = lectura o API sin UI completa | `MVP` = stub/marketing | `Redirect` = redirección

**Leyenda Rol:** `All` = autenticado | `CW` = Commercial Write (Admin, Manager, Sales) | `AM` = Admin, Manager | `Adm` = Admin (policy API)

---

## Account & Public

| Módulo | Función | Ruta | Rol | Estado |
|--------|---------|------|-----|--------|
| Auth | Login email/password | `/Account/Login` | Anonymous | Full |
| Auth | MFA verify | `/Account/Login?handler=VerifyMfa` | Anonymous | Full |
| Auth | SSO external | `/Account/Login?handler=ExternalLogin` | Anonymous | Partial |
| Auth | Logout | `/Account/Logout` | All | Full |
| Auth | Access denied | `/Account/AccessDenied` | All | Full |
| Marketing | Landing | `/landing` | Anonymous | MVP |
| Marketing | Pricing | `/pricing` | Anonymous | MVP |
| Marketing | Demo CEO | `/demo` | Anonymous | MVP |

---

## Command Center

| Módulo | Función | Ruta | Rol | Estado |
|--------|---------|------|-----|--------|
| Command | Dashboard principal | `/`, `/Index` | All | Full |
| Command | Trust Studio inbox | `/TrustInbox` | All | Full |
| Command | Approve HITL | POST TrustInbox Approve | All | Full |
| Command | Reject HITL | POST TrustInbox Reject | All | Full |
| Command | Rollback decision | POST TrustInbox Rollback | All | Full |
| Command | Set threshold | POST TrustInbox SetThreshold | All | Full |
| Command | Workforce / Agents | `/Agents` | All | Full |
| Command | Flow actions (insights) | POST `/FlowActions` | All | Full |
| Command | Decision history | `/command/decisions` | All | Full |
| Command | Playbooks | `/command/playbooks` | All | Partial |
| Command | Outcomes | `/command/outcomes` | All | Partial |
| Command | Command palette | `#flow-palette` (layout) | All | Full |
| Command | Dashboard redirect | `/Dashboard` | All | Redirect → `/` |
| Command | AI Command redirect | `/AiCommandCenter` | All | Redirect → `/` |

---

## Revenue & Executive

| Módulo | Función | Ruta | Rol | Estado |
|--------|---------|------|-----|--------|
| Revenue | Revenue OS dashboard | `/revenue` | All | Full |
| Executive | Executive OS dashboard | `/executive` | All | Full |
| Executive | Export board HTML | `/executive?handler=Export` | All | Full |
| Deals | Lista pipeline | `/Deals` | All read / CW write | Full |
| Deals | Crear deal | `/Deals/Create` | CW | Full |
| Deals | Editar deal | `/Deals/Edit/{id}` | CW | Full |
| Deals | Detalle + modales | `/Deals/Details/{id}` | All read / CW POST | Full |
| Deals | Modal probability | Details updateProbability | CW | Full |
| Deals | Modal stage | Details updateStage | CW | Full |
| Deals | Close Won | Details closeDeal | CW | Full |
| Deals | Close Lost | Details loseDeal | CW | Full |
| Deals | Delete | Details Delete | CW | Full |
| Deals | Import CSV | `/Deals/Import` POST | CW | Full |
| Deals | Bulk actions | `/Deals/BulkActions` POST | CW | Full |
| Deals | Drawer preview | `_FlowDrawer` | All | Full |

---

## Leads

| Módulo | Función | Ruta | Rol | Estado |
|--------|---------|------|-----|--------|
| Leads | Lista | `/Leads` | All read / CW write | Full |
| Leads | Crear | `/Leads/Create` | CW | Full |
| Leads | Editar | `/Leads/Edit/{id}` | CW | Full |
| Leads | Detalle | `/Leads/Details/{id}` | All read / CW POST | Full |
| Leads | Qualify | Details Qualify | CW | Full |
| Leads | Convert to Customer | Details ConvertToCustomer | CW | Full |
| Leads | Create deal modal | Details createDealModal | CW | Full |
| Leads | Delete | Details Delete | CW | Full |
| Leads | Import | `/Leads/Import` POST | CW | Full |
| Leads | Bulk | `/Leads/BulkActions` POST | CW | Full |

---

## Customers

| Módulo | Función | Ruta | Rol | Estado |
|--------|---------|------|-----|--------|
| Customers | Lista | `/Customers` | All read / CW write | Full |
| Customers | Crear | `/Customers/Create` | CW | Full |
| Customers | Editar | `/Customers/Edit/{id}` | CW | Full |
| Customers | Detalle | `/Customers/Details/{id}` | All read / CW POST | Full |
| Customers | Create deal modal | Details createDealModal | CW | Full |
| Customers | Record contact | Details RecordContact | CW | Full |
| Customers | Delete | Details Delete | CW | Full |
| Customers | Import / Bulk | Import, BulkActions | CW | Full |

---

## Customer 360 & Success

| Módulo | Función | Ruta | Rol | Estado |
|--------|---------|------|-----|--------|
| C360 | Directorio búsqueda | `/Customer360` | All | Full |
| C360 | Detalle enterprise | `/customers/{id}/360` | All | Full |
| C360 | Duplicate alert | Customer360 (display) | All | Partial |
| C360 | Merge identity | API `POST api/data/identity/merge` | Adm | Partial (sin UI) |
| CS | Customer Success OS | `/customer-success` | All | Full |
| CS | Create ticket | POST CreateTicket | All | Full |
| CS | Close ticket | POST CloseTicket | All | Full |
| CS | Run playbook | POST RunPlaybook | All | Full |
| Support | Redirect | `/Support` | All | Redirect → customer-success |

---

## Workflows & Policies

| Módulo | Función | Ruta | Rol | Estado |
|--------|---------|------|-----|--------|
| Workflows | Lista | `/Workflows` | All read / CW write | Full |
| Workflows | Crear | `/Workflows/Create` POST | CW | Full |
| Workflows | Editar | `/Workflows/Edit/{id}` | CW | Full |
| Workflows | Modal add trigger | `_WorkflowEditModals` | CW | Full |
| Workflows | Modal add condition | `_WorkflowEditModals` | CW | Full |
| Workflows | Modal add action | `_WorkflowEditModals` | CW | Full |
| Workflows | Duplicate / Delete | Edit handlers | CW | Full |
| Workflows | Import CSV | `/Workflows/Import` | CW | Full |
| Policies | Lista | `/Policies` | All read / CW write | Full |
| Policies | Crear / Editar | Create, Edit | CW | Full |
| Policies | Duplicate / Delete | Edit | CW | Full |
| Policies | Import | `/Policies/Import` | CW | Full |

---

## Admin & Platform

| Módulo | Función | Ruta | Rol | Estado |
|--------|---------|------|-----|--------|
| Users | Lista | `/Users` | AM | Full |
| Users | Crear | `/Users/Create` POST | AM | Full |
| Users | Editar / roles | `/Users/Edit/{id}` | AM | Full |
| Users | Matriz roles | `/Users/Roles` | AM | Full |
| Users | Import / Bulk | Import, BulkActions | AM | Full |
| Settings | Tenant settings | `/Settings` | AM | Full |
| Settings | Export/Import config | Settings handlers | AM | Full |
| Settings | Import modal | importConfigModal | AM | Full |
| Audit | Log + filtros | `/Audit` | All | Full |
| Audit | Export | POST Export | All | Full |
| Audit | Event modal | eventDetailsModal | All | Full |
| Billing | Usage dashboard | `/billing` | All | Partial |
| Billing | Stripe checkout | API `POST api/billing/checkout` | All | Partial |
| Integrations | Marketplace | `/Integrations` | All | Full |
| Integrations | OAuth connect | Integrations OAuth | All | Partial |
| Integrations | OAuth callback | `/Integrations/OAuthCallback` | Anonymous | Full |
| Memory | Business memory | `/Memory` | All | Full |
| Tasks | Lista + filtros | `/Tasks` | All | Full |
| Tasks | Complete / Assign | POST handlers | All | Full |
| Voice | Call log MVP | `/VoiceCalls` | All | Partial |
| Failed Events | Ops queue | `/FailedEvents` | All | Full |
| Failed Events | Replay | POST Replay | All | Full |
| Dev | Flow components | `/flow/components` | All | MVP |

---

## API (autenticado salvo nota)

| Módulo | Función | Ruta | Rol | Estado |
|--------|---------|------|-----|--------|
| API Auth | Login JWT | `POST api/auth/login` | Anonymous | Full |
| API Users | Create user | `POST api/users` | Adm | Full |
| API Tenants | Create tenant | `POST api/tenants` | Adm | Full |
| API Leads/Customers/Deals | CRUD REST | `api/leads`, `api/customers`, `api/deals` | CW* | Full |
| API Trust | Inbox/metrics | `api/trust/*` | All | Full |
| API Revenue | Dashboard KPIs | `api/revenue/*` | All | Full |
| API Memory | Search/timeline | `api/memory/*` | All | Full |
| API Import | CSV bulk | `api/import/*` | CW* | Full |
| API Provisioning | New tenant | `POST api/provisioning/tenants` | Platform key | Full |
| API Billing | Webhook Stripe | `POST api/billing/stripe/webhook` | Anonymous | Partial |

\*Commercial write vía autenticación + tenant; sin policy role explícita en todos los endpoints.

---

## Resumen inventario

| Categoría | Funciones | Full | Partial/MVP |
|-----------|-----------|------|-------------|
| Command | 13 | 10 | 3 |
| Revenue/Deals | 14 | 14 | 0 |
| Leads | 10 | 10 | 0 |
| Customers | 9 | 9 | 0 |
| C360/CS | 9 | 7 | 2 |
| Workflows/Policies | 12 | 12 | 0 |
| Admin/Platform | 18 | 15 | 3 |
| API | 10 | 9 | 1 |
| **Total** | **95** | **86 (91%)** | **9 (9%)** |

---

*Generado para Enterprise QA Certification Program — ver [README.md](README.md) en esta carpeta.*
