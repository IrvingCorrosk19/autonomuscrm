# ROLE_SMOKE_TESTS — Pruebas de humo por rol

**URL:** http://164.68.99.83:8091 | **Password:** `AutonomusTest123!`  
**Automatizado:** `.\tests\e2e\run-vps-test-qa.ps1` + `run-rc-smoke.ps1 -ConfigPath tests\vps-test\config.vps.json`

---

## SuperAdmin — `superadmin@autonomuscrm.local`

| # | Check | Ruta | Esperado |
|---|-------|------|----------|
| SA-S01 | Login | `/Account/Login` | → `/executive` |
| SA-S02 | Health | `/health/live` | Healthy |
| SA-S03 | Executive | `/executive` | HTTP 200 |
| SA-S04 | Users | `/Users` | Lista 7 usuarios |
| SA-S05 | Create user | `/Users/Create` | Form OK |
| SA-S06 | Settings | `/Settings` | HTTP 200 |
| SA-S07 | Trust | `/TrustInbox` | HTTP 200 |
| SA-S08 | Audit | `/Audit` | HTTP 200 |
| SA-S09 | Billing | `/billing` | HTTP 200 |
| SA-S10 | Workflows | `/Workflows` | HTTP 200 |
| SA-S11 | Memory | `/Memory` | HTTP 200 |
| SA-S12 | Integrations | `/Integrations` | HTTP 200 |
| SA-S13 | Leads CRUD | `/Leads/Create` | HTTP 200 |
| SA-S14 | API JWT | `POST api/auth/login` | accessToken |
| SA-S15 | Failed Events | `/FailedEvents` | HTTP 200 |

---

## Admin — `admin@autonomuscrm.local`

| # | Check | Ruta | Esperado |
|---|-------|------|----------|
| AD-S01 | Login | `/Account/Login` | → `/executive` |
| AD-S02 | Revenue | `/revenue` | HTTP 200 |
| AD-S03 | Leads | `/Leads` | 10 leads seed |
| AD-S04 | Customers | `/Customers` | 5 clientes |
| AD-S05 | Deals | `/Deals` | 5 deals |
| AD-S06 | Tasks | `/Tasks` | 8 tasks |
| AD-S07 | Customer360 | `/Customer360` | HTTP 200 |
| AD-S08 | CS | `/customer-success` | HTTP 200 |
| AD-S09 | Policies | `/Policies` | HTTP 200 |
| AD-S10 | Users/Roles | `/Users/Roles` | HTTP 200 |
| AD-S11 | Command | `/` | HTTP 200 |
| AD-S12 | Agents | `/Agents` | HTTP 200 |
| AD-S13 | Localization | selector es-PA | Labels cambian |

---

## Manager — `manager@autonomuscrm.local`

| # | Check | Ruta | Esperado |
|---|-------|------|----------|
| MG-S01 | Login | `/Account/Login` | → `/executive` |
| MG-S02 | Executive export | `/executive?handler=Export` | Download |
| MG-S03 | Users create | `/Users/Create` | Permitido |
| MG-S04 | Leads create | `/Leads/Create` | Permitido |
| MG-S05 | Deals create | `/Deals/Create` | Permitido |
| MG-S06 | Workflows | `/Workflows` | HTTP 200 |
| MG-S07 | Trust approve | `/TrustInbox` POST | OK |
| MG-S08 | API users | `POST api/users` | 403 |
| MG-S09 | Settings | `/Settings` | HTTP 200 |
| MG-S10 | Decisions | `/command/decisions` | HTTP 200 |

---

## Sales — `sales1@autonomuscrm.local`

| # | Check | Ruta | Esperado |
|---|-------|------|----------|
| SL-S01 | Login | `/Account/Login` | → `/revenue` |
| SL-S02 | Revenue home | `/revenue` | HTTP 200 |
| SL-S03 | Create lead | `/Leads/Create` POST | Redirect /Leads |
| SL-S04 | Edit lead | `/Leads/Edit/{id}` | Permitido |
| SL-S05 | Create deal | `/Deals/Create` | Permitido |
| SL-S06 | Deal details modals | `/Deals/Details/{id}` | Modales abren |
| SL-S07 | Users blocked | `/Users` | Denied |
| SL-S08 | Settings blocked | `/Settings` | Denied |
| SL-S09 | Tasks | `/Tasks` | HTTP 200 |
| SL-S10 | sales2 parity | `sales2@` login | → `/revenue` |

---

## Support — `support@autonomuscrm.local`

| # | Check | Ruta | Esperado |
|---|-------|------|----------|
| SP-S01 | Login | `/Account/Login` | → `/Customer360` |
| SP-S02 | C360 search | `/Customer360?q=Banco` | Resultados |
| SP-S03 | Leads read | `/Leads` | HTTP 200 |
| SP-S04 | Leads write block | `/Leads/Create` | AccessDenied |
| SP-S05 | Deals read | `/Deals` | HTTP 200 |
| SP-S06 | Deals write block | `/Deals/Create` | AccessDenied |
| SP-S07 | CS ticket | `/customer-success` POST | Ticket creado |
| SP-S08 | Users block | `/Users` | Denied |
| SP-S09 | Trust read | `/TrustInbox` | HTTP 200 |
| SP-S10 | Workflows block | `/Workflows/Create` | AccessDenied |

---

## Viewer — `viewer@autonomuscrm.local`

| # | Check | Ruta | Esperado |
|---|-------|------|----------|
| VW-S01 | Login | `/Account/Login` | → `/` |
| VW-S02 | Command | `/` | HTTP 200 |
| VW-S03 | Executive read | `/executive` | HTTP 200 |
| VW-S04 | Revenue read | `/revenue` | HTTP 200 |
| VW-S05 | Leads read | `/Leads` | HTTP 200 |
| VW-S06 | Create block | `/Leads/Create` | AccessDenied |
| VW-S07 | Customers read | `/Customers` | HTTP 200 |
| VW-S08 | Audit read | `/Audit` | HTTP 200 |
| VW-S09 | Users block | `/Users` | Denied |
| VW-S10 | Workflows edit block | `/Workflows/Edit/{id}` | AccessDenied |

---

## Smoke global (todos los roles Admin)

Ejecutar `run-rc-smoke.ps1` — 23 rutas operativas. Ver `QA_HANDOFF_READY.md`.
