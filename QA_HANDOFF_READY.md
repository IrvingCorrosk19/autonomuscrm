# QA Handoff Ready — AutonomusCRM RC Zero

**Prepared:** 2026-06-06  
**Scenario:** TechSolutions Panamá (first client simulation)  
**Ready for:** Independent human QA / UAT tomorrow

---

## 1. Environment setup (15 min)

### Prerequisites

- .NET 9 SDK
- PostgreSQL 16+ running on `localhost:5432`
- Database: `autonomuscrm` (migrations applied)

### Start application

```powershell
cd c:\Proyectos\autonomuscrm
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project AutonomusCRM.API --no-launch-profile --urls "http://127.0.0.1:5154"
```

> **Important:** Use `127.0.0.1`, not `localhost` — on Windows, `localhost` resolves to IPv6 and can timeout.

Verify: open `http://127.0.0.1:5154/health/live` → response `Healthy`

### Bootstrap users (if not already provisioned)

```powershell
.\deploy\bootstrap-first-client.ps1 -ConfigPath tests\first-client\config.json
```

---

## 2. Test users & passwords

**URL:** http://127.0.0.1:5154/Account/Login  
**Password (all users):** `TechSolutions2026!`  
**Tenant ID:** `49b6cf6a-ee7f-49b3-9296-6fa20ec3129b`

| Role | Email | Expected home after login |
|------|-------|---------------------------|
| Admin | admin@techsolutions.pa | `/executive` |
| Admin Ops | ops@techsolutions.pa | `/executive` |
| Manager | manager@techsolutions.pa | `/executive` |
| Sales 1 | sales1@techsolutions.pa | `/revenue` |
| Sales 2 | sales2@techsolutions.pa | `/revenue` |
| Support | support@techsolutions.pa | `/Customer360` |
| Viewer | viewer@techsolutions.pa | `/` (dashboard) |

**Login tip:** Leave Tenant ID empty or `00000000-0000-0000-0000-000000000000` — the system finds the user by email across tenants.

---

## 3. Smoke routes (must load — no 500/404)

Login as **Admin** first, then visit each route:

| # | Route | Min role |
|---|-------|----------|
| 1 | `/Account/Login` | — |
| 2 | `/` | Authenticated |
| 3 | `/executive` | Admin, Manager |
| 4 | `/revenue` | Sales+ |
| 5 | `/TrustInbox` | Admin, Manager, Support |
| 6 | `/Customer360` | Admin, Manager, Support |
| 7 | `/Leads` | Sales+ |
| 8 | `/Customers` | Sales+ |
| 9 | `/Deals` | Sales+ |
| 10 | `/Tasks` | Sales+ |
| 11 | `/Users` | Admin, Manager |
| 12 | `/Policies` | Admin, Manager |
| 13 | `/Settings` | Admin, Manager |
| 14 | `/Integrations` | Admin |
| 15 | `/Audit` | Admin |
| 16 | `/billing` | Admin |
| 17 | `/Workflows` | Admin, Manager |
| 18 | `/Memory` | Admin |
| 19 | `/customer-success` | Admin |
| 20 | `/Leads/Create` | Sales+ |
| 21 | `/Customers/Create` | Sales+ |
| 22 | `/Deals/Create` | Sales+ |
| 23 | `/Users/Create` | Admin, Manager |

**Automated smoke:** `.\tests\e2e\run-rc-smoke.ps1`

---

## 4. Cross-role checklist

| Test | Role | Action | Expected |
|------|------|--------|----------|
| CR-01 | Admin | Login | Redirect to `/executive`, full menu |
| CR-02 | Manager | Login | Redirect to `/executive`, no billing delete |
| CR-03 | Sales | Login | Redirect to `/revenue`, CRUD leads |
| CR-04 | Support | Login | Redirect to `/Customer360` |
| CR-05 | Viewer | Login | Redirect to `/`, read-only |
| CR-06 | Support | Open `/Leads/Create` | Access Denied |
| CR-07 | Viewer | Open `/Leads/Create` | Access Denied |
| CR-08 | Sales | Create lead | Success, appears in list |
| CR-09 | Admin | Open `/Users`, `/Audit`, `/Workflows` | HTTP 200 |

**Automated:** `.\tests\e2e\run-first-client-qa.ps1`

Reference: `ROLE_TEST_MATRIX.md`

---

## 5. Responsive checklist

Validated automatically at: **320, 375, 768, 1024, 1440, 1920, 2560 px**

Manual spot-check (recommended):

- [ ] Login page usable at 320px (single column, no horizontal scroll)
- [ ] Hamburger menu opens sidebar on mobile
- [ ] Leads/Customers tables scroll horizontally inside table area (not whole page)
- [ ] Modals fit viewport on mobile

**Automated:** `.\tests\e2e\run-responsive-gate.ps1`

---

## 6. First-client business flow (manual UAT)

Execute as **Sales** then **Admin**:

1. **Lead** — Create lead from `/Leads/Create`
2. **Customer** — Convert or create customer
3. **Deal** — Create deal linked to customer
4. **Closed Won** — Move deal stage to won
5. **Task** — Create task on deal/customer
6. **Workflow** — Admin: verify workflow list loads, edit modal opens
7. **Audit** — Admin: verify audit log shows recent actions
8. **Trust** — Admin: `/TrustInbox` loads
9. **Billing** — Admin: `/billing` loads (Stripe optional)
10. **Users/Roles** — Admin: verify team list shows 7 users

---

## 7. Localization spot-check

- Switch language: **en** → **es** → **es-PA** (topbar selector)
- Verify Dashboard, Revenue, Leads labels change
- Automated: `dotnet test -c Release --filter LocalizationCoverageTests`

---

## 8. Automated gate commands (full suite)

```powershell
cd c:\Proyectos\autonomuscrm

# Build & unit tests
dotnet build -c Release
dotnet test -c Release

# Operational gates (API must be running)
.\deploy\bootstrap-first-client.ps1 -ConfigPath tests\first-client\config.json
.\tests\e2e\run-rc-all-gates.ps1

# Or individually:
# .\tests\e2e\run-rc-smoke.ps1
# .\tests\e2e\run-first-client-qa.ps1
# .\tests\e2e\run-responsive-gate.ps1
```

Evidence output: `tests/qa-evidence/` (dated subfolders)

---

## 9. Defects corrected in RC Zero (for QA awareness)

| ID | Severity | Issue | Resolution |
|----|----------|-------|------------|
| RC-001 | Blocker | Bootstrap health wait never succeeded | Use `/health/live` + `127.0.0.1` |
| RC-002 | Blocker | Wrong tenant name in config (Panama vs Panamá) | Fixed `config.json` tenant name |
| RC-003 | Blocker | API login 401 with explicit tenantId | Empty TenantId for email lookup |
| RC-004 | High | Horizontal page scroll 768–1920px | Flex shell constraints |
| RC-005 | High | Login 2-column layout on mobile | Auth CSS cascade fix |
| RC-006 | Medium | Data tables overflow page | `flow-datatable` scroll wrap |
| RC-007 | Medium | Missing Support/Viewer users | Bootstrap created users |

No open P0/P1 defects at handoff.

---

## 10. Known limitations (not blocking QA)

| Item | Note |
|------|------|
| Integration tests | 29 tests skip without Docker/Postgres service |
| RabbitMQ / Redis | Optional for UI QA; required for full event-bus production |
| Stripe billing | Checkout needs live keys for end-to-end payment |
| OAuth integrations | Live connect needs provider credentials |
| Demo tenant | Separate tenant `TechSolutions Panama` (demo seed) — use `@techsolutions.pa` accounts |

---

## 11. QA sign-off template

```
QA Lead: _______________  Date: ___________

[ ] Environment started without dev help
[ ] All 7 roles login successfully
[ ] Smoke routes 1–23 load
[ ] Responsive spot-check 320/768/1440 OK
[ ] Lead → Customer → Deal flow OK
[ ] No unexplained 500/403 on allowed actions
[ ] Localization en/es/es-PA OK

Notes:
_______________________________________________
```

---

## 12. Support contacts

- **Bootstrap guide:** `FIRST_CUSTOMER_BOOTSTRAP_GUIDE.md`
- **Role matrix:** `ROLE_TEST_MATRIX.md`
- **RC package:** `ops/qa/RC_QA_PACKAGE.md`
- **Certification:** `ENTERPRISE_CERTIFICATION_FINAL_REPORT.md`

**RC Zero status: READY FOR HUMAN QA**

---

*Last automated validation: 2026-06-06 — build 0 errors, 202 tests passed, Gates 6–9 all green via `run-rc-all-gates.ps1`.*
