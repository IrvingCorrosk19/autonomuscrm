# Global Manufacturing Group — Demo Database Scripts

Enterprise sales demo ERP datasets for Database Intelligence flows.

## Primary volume database (recommended for live demo)

**PostgreSQL** — schema `gmg_erp` in the same database as AutonomusCRM (`autonomuscrm`):

```powershell
psql -U postgres -d autonomuscrm -f ops/demo/manufacturing/01_postgresql_gmg_erp_schema.sql
psql -U postgres -d autonomuscrm -f ops/demo/manufacturing/02_postgresql_gmg_erp_data.sql
```

| Table | Rows | DIP entity |
|-------|------|------------|
| `cust_master` | 50,000 | Customer |
| `facturacion` | 500,000 | Invoice |
| `pagos` | 2,000,000 | Payment |
| `customer_contacts` | 75,000 | Contact |
| `products` | 320 | Product |
| `activities` | 15,000 | Activity |
| `crm_deals` | 2,500 | Deal |
| `crm_tasks` | 1,000 | Task |
| `empresas` | 200 | Company |

## Other engines (sample volume for connector demos)

| Script | Engine | Notes |
|--------|--------|-------|
| `03_sqlserver_gmg_erp.sql` | SQL Server | 5k customers, 50k invoices, 200k payments |
| `04_mysql_gmg_erp.sql` | MySQL | Same sample scale |
| `05_mariadb_gmg_erp.sql` | MariaDB | Alias to MySQL script |
| `06_oracle_gmg_erp.sql` | Oracle | Sample scale in `gmg_erp` schema |

Register each engine manually in **Database Intelligence → Connect** using read-only credentials after loading the script.

## CRM tenant seed (AutonomusCRM app database)

Set in `appsettings.Development.json` or environment:

```json
"Seed": {
  "Enabled": true,
  "GlobalManufacturing": {
    "Enabled": true,
    "LiteMode": false
  }
}
```

Use `"LiteMode": true` for 500 customers (fast local dev). Full demo uses 50,000 customers in CRM plus dashboard signals.

Login tenant: **Global Manufacturing Group** (`admin@autonomuscrm.local` / `Admin123!`).

## 30-minute demo flow

See [DEMO_READINESS_REPORT.md](../../DEMO_READINESS_REPORT.md) at repository root.
