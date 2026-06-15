# DEMO READINESS REPORT — Global Manufacturing Group

**Date:** 2026-05-28  
**Sprint:** 2 — Demo Tenant Enterprise  
**Basis:** [AUTONOMUSCRM_REALITY_CHECK_2026.md](AUTONOMUSCRM_REALITY_CHECK_2026.md)  
**Scope:** Demo data, scripts, and runbook only — **no new product features**

---

## Executive summary

AutonomusCRM can deliver a **credible 30-minute commercial demo** when the environment is prepared using this package:

| Layer | Status | Notes |
|-------|--------|-------|
| CRM tenant **Global Manufacturing Group** | ✅ Ready (gated seed) | 50k customers, deals, leads, tasks, product usage |
| ERP demo DB **PostgreSQL** (`gmg_erp`) | ✅ Ready (SQL scripts) | 500k invoices, 2M payments, full entity set |
| ERP demo **SQL Server / MySQL / MariaDB / Oracle** | ✅ Sample scripts | Connector story; not full volume |
| DIP flow Connect → … → Rollback | ✅ Runnable | PostgreSQL primary; mappings + session prep required |
| Dashboards (Executive, Revenue, Health, Insights, Graph) | ✅ Populated | CRM + DIP pages with demo tenant |

**Commercial verdict (unchanged from reality check):** suitable for **controlled sales/demo/pilot**, not general self-serve SaaS launch.

---

## Demo tenant — Global Manufacturing Group

| Property | Value |
|----------|--------|
| Tenant name | Global Manufacturing Group |
| Stable tenant ID | `d0e00000-0000-4000-8000-000000000002` |
| Login | `admin@autonomuscrm.local` / `Admin123!` (Admin role) |
| Default on login page | Yes (preferred over CEO_DEMO when seeded) |

### CRM volumes (app database)

| Entity | Full demo | Lite (`LiteMode: true`) |
|--------|-----------|-------------------------|
| Customers | 50,000 | 500 |
| Leads (contacts pipeline) | 5,000 | 250 |
| Deals | 2,500 | 120 |
| Tasks (`WorkflowTasks`) | 1,000 | 100 |
| Product usage events | 3,200 | 200 |
| Companies (divisions) | 200 unique `Company` values | same |

Invoices and payments live in **`gmg_erp`** (external schema), not as CRM tables — aligned with product architecture.

### Enable CRM seed

```json
"Seed": {
  "Enabled": true,
  "GlobalManufacturing": {
    "Enabled": true,
    "LiteMode": false
  }
}
```

Or environment variables: `Seed__Enabled=true`, `Seed__GlobalManufacturing__Enabled=true`.

**Tests:** `Seed:GlobalManufacturing:Enabled` defaults to **false** so CI and `CustomWebApplicationFactory` are not impacted.

---

## ERP demo databases

Scripts: [`ops/demo/manufacturing/`](ops/demo/manufacturing/)

### PostgreSQL (primary — full volume)

```powershell
psql -U postgres -d autonomuscrm -f ops/demo/manufacturing/01_postgresql_gmg_erp_schema.sql
psql -U postgres -d autonomuscrm -f ops/demo/manufacturing/02_postgresql_gmg_erp_data.sql
```

| Table | Rows | Business entity |
|-------|------|-----------------|
| `gmg_erp.empresas` | 200 | Company |
| `gmg_erp.cust_master` | 50,000 | Customer |
| `gmg_erp.customer_contacts` | 75,000 | Contact |
| `gmg_erp.products` | 320 | Product |
| `gmg_erp.tbl_ventas` | 120,000 | Sale |
| `gmg_erp.facturacion` | 500,000 | Invoice |
| `gmg_erp.pagos` | 2,000,000 | Payment |
| `gmg_erp.activities` | 15,000 | Activity |
| `gmg_erp.crm_deals` | 2,500 | Deal |
| `gmg_erp.crm_tasks` | 1,000 | Task |

Load time: typically **2–8 minutes** on a local PostgreSQL instance.

### Other engines

| Engine | Script | Scale |
|--------|--------|-------|
| SQL Server | `03_sqlserver_gmg_erp.sql` | Sample (5k / 50k / 200k core rows) |
| MySQL | `04_mysql_gmg_erp.sql` | Sample |
| MariaDB | `05_mariadb_gmg_erp.sql` | Uses MySQL script |
| Oracle | `06_oracle_gmg_erp.sql` | Sample |

Use **Connect** UI to register read-only profiles per engine after loading scripts. Live multi-engine demo requires those instances running; **PostgreSQL-only is the recommended default narrative**.

Auto-seeded DIP profile (when CRM seed runs): **GMG ERP — PostgreSQL (Primary Demo)**.

---

## 30-minute demo script

| Min | Step | Route / action | Story |
|-----|------|----------------|-------|
| 0–2 | Login | `/Account/Login` | Global Manufacturing Group, Admin |
| 2–4 | Executive snapshot | `/Executive` | AI decisions, revenue signals |
| 4–6 | Revenue | `/Revenue` | Quota, pipeline, contracts |
| 6–8 | Connect | `/DatabaseIntelligence/Connect` | GMG ERP PostgreSQL profile |
| 8–11 | Discover | `/DatabaseIntelligence/Explore` | Schema catalog, row counts |
| 11–14 | Understand | `/DatabaseIntelligence/Understand` | Confirm Customer / Invoice / Payment mappings |
| 14–17 | Health | `/DatabaseIntelligence/Health` | Quality score, orphans, duplicates |
| 17–20 | Graph | `/DatabaseIntelligence/Graph` | Customer → Invoice → Payment graph |
| 20–23 | Insights | `/DatabaseIntelligence/Insights` | Prioritized business insights |
| 23–27 | Operate | `/DatabaseIntelligence/Operate` | Studios → Preview → Execute (Admin) |
| 27–29 | Rollback | Operate → Rollback | Undo CRM import safely |
| 29–30 | Close | Dashboard / CRM | Customers & deals in CRM |

### Preconditions (day before demo)

1. PostgreSQL running; run ERP scripts `01` + `02`.
2. App started with `Seed:GlobalManufacturing:Enabled=true` (full or lite).
3. RabbitMQ optional (SignalR in-process progress still works).
4. **Do not** enable autonomous agents for unattended execution.
5. Complete **Understand** mapping confirmation once per environment.

### Known limitations (from reality check)

- Multi-page DIP flow (not single wizard) — allow time for navigation.
- Non-PostgreSQL engines: connector demo only unless instances are provisioned.
- First **Operate** session requires confirmed mappings and successful extract.
- Data Hub CSV path remains an alternate demo — not required for this script.

---

## Dashboards checklist

| Dashboard | URL | Data source |
|-----------|-----|-------------|
| Executive | `/Executive` | `AiDecisionAudit`, analytics snapshots |
| Revenue | `/Revenue` | `SalesQuota`, `Deals`, `CustomerContract` |
| Health | `/DatabaseIntelligence/Health` | DIP health engine on `gmg_erp` |
| Insights | `/DatabaseIntelligence/Insights` | DIP insight engine |
| Graph | `/DatabaseIntelligence/Graph` | DIP business graph |

---

## Verification

```powershell
dotnet build
dotnet test --filter "Category=Demo"
dotnet test --filter "Category=DatabaseIntelligence"
```

| Check | Expected |
|-------|----------|
| Demo unit tests | 3 PASS |
| DIP regression | 149 PASS / 0 SKIP (with PostgreSQL) |
| ERP row counts | Query at end of `02_postgresql_gmg_erp_data.sql` |
| CRM customers | `SELECT COUNT(*) FROM "Customers" WHERE "TenantId" = 'd0e00000-0000-4000-8000-000000000002'` → 50000 (or 500 lite) |

---

## Deliverables (Sprint 2)

| Artifact | Path |
|----------|------|
| Tenant ID | `Application/Common/Tenancy/TenantIds.cs` |
| CRM seeder | `Infrastructure/Persistence/Seed/GlobalManufacturingDemoSeeder.cs` |
| Bulk SQL helpers | `Infrastructure/Persistence/Seed/GlobalManufacturingBulkSql.cs` |
| ERP scripts | `ops/demo/manufacturing/*.sql` |
| Demo config | `appsettings.Development.json` → `Seed:GlobalManufacturing`, `Demo:Manufacturing` |
| Login default tenant | `Pages/Account/Login.cshtml.cs` |
| Tests | `Tests/Demo/GlobalManufacturingDemoTargetsTests.cs` |
| Tracker update | `DATABASE_INTELLIGENCE_PLATFORM_MASTER_TRACKER.md` |

---

## Go / no-go for sales meeting

| Criterion | Go | No-go |
|-----------|-----|-------|
| ERP scripts loaded | ✅ | ❌ Skip Health/Graph/Insights depth |
| CRM seed enabled | ✅ | ❌ Executive/Revenue thin |
| Mappings confirmed | ✅ | ❌ Operate session fails |
| Scope agreed (PG primary) | ✅ | ❌ Promising all engines day-one |

**Recommendation:** **GO** for scripted demo with PostgreSQL ERP + Global Manufacturing Group tenant, **NO-GO** for claiming full multi-engine production parity.
