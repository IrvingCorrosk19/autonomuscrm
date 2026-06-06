# RC QA Package — AutonomusCRM Release Candidate

Operational package for human QA, UAT, and first-client onboarding.

**Primary handoff document:** [`QA_HANDOFF_READY.md`](../../QA_HANDOFF_READY.md)  
**Certification:** [`ENTERPRISE_CERTIFICATION_FINAL_REPORT.md`](../../ENTERPRISE_CERTIFICATION_FINAL_REPORT.md)

---

## Test users (TechSolutions Panamá)

| Role | Email | Password | Home |
|------|-------|----------|------|
| Admin | `admin@techsolutions.pa` | `TechSolutions2026!` | `/executive` |
| Admin Ops | `ops@techsolutions.pa` | `TechSolutions2026!` | `/executive` |
| Manager | `manager@techsolutions.pa` | `TechSolutions2026!` | `/executive` |
| Sales 1 | `sales1@techsolutions.pa` | `TechSolutions2026!` | `/revenue` |
| Sales 2 | `sales2@techsolutions.pa` | `TechSolutions2026!` | `/revenue` |
| Support | `support@techsolutions.pa` | `TechSolutions2026!` | `/Customer360` |
| Viewer | `viewer@techsolutions.pa` | `TechSolutions2026!` | `/` |

**TenantId:** `49b6cf6a-ee7f-49b3-9296-6fa20ec3129b`  
**Login URL:** http://127.0.0.1:5154/Account/Login

See [ROLE_TEST_MATRIX.md](../../ROLE_TEST_MATRIX.md) for per-role test cases.

---

## One-command bootstrap + gates

```powershell
# API running separately:
dotnet run --project AutonomusCRM.API --urls http://127.0.0.1:5154

# Bootstrap + all operational gates:
.\deploy\bootstrap-first-client.ps1
.\tests\e2e\run-rc-all-gates.ps1

# Or orchestrated:
.\scripts\start-testing-today.ps1 -StartApi -RunAllGates
```

> Use `127.0.0.1` instead of `localhost` on Windows (IPv6 timeout).

---

## Smoke routes (Gate 8)

| Route | Role min |
|-------|----------|
| `/Account/Login` | — |
| `/`, `/executive`, `/revenue` | Authenticated |
| `/TrustInbox`, `/Customer360` | Admin/Manager/Support |
| `/Leads`, `/Customers`, `/Deals`, `/Tasks` | Sales+ |
| `/Users`, `/Policies`, `/Workflows`, `/Settings` | Admin/Manager |
| `/billing`, `/Integrations`, `/Audit`, `/Memory`, `/customer-success` | Admin |

Script: `tests/e2e/run-rc-smoke.ps1`

---

## Responsive (Gate 6)

Viewports: 320, 375, 768, 1024, 1440, 1920, 2560 px  
Script: `tests/e2e/run-responsive-gate.ps1` (Playwright headless)

---

## Localization verification

- Cultures: **en**, **es**, **es-PA** (language switcher in shell).
- Automated: `dotnet test -c Release --filter LocalizationCoverageTests` (7 tests, 1411 keys each).

---

## Build & test gates (1–3)

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Integration/E2E tests **skip** when Docker/PostgreSQL unavailable (Xunit.SkippableFact).

---

## Manual QA checklist

- [ ] Login all 7 roles — correct redirect and menu visibility
- [ ] Switch en / es / es-PA on Dashboard, Revenue, Leads
- [ ] CRUD: Lead → Customer → Deal flow
- [ ] Modals: Deals Details, Customers Details, Workflows Edit
- [ ] Responsive: 375, 768, 1440 (shell + tables)
- [ ] Keyboard: Tab through login, modal open/ESC, form submit

---

## Known external dependencies

| Dependency | Required for |
|------------|--------------|
| Docker Desktop / Postgres | Integration + E2E tests |
| RabbitMQ | Event bus, failed-events queue |
| Redis | Production session/cache |
| Stripe keys | Billing checkout (optional UAT) |
| OAuth credentials | Integrations live connect |

---

**RC Zero status:** CERTIFIED — ready for human QA without developer assistance.
