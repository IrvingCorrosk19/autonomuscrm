# Data Hub — User Guide

## Access

Navigate to **Platform → Data Hub** or `/DataHub`. Requires Manager or Admin role.

## Import Workflow

1. **Import Center** — Select entity (Customer, Lead, Deal, User), load mode, upload file
2. **Mapping Studio** — Review auto-mapped columns; adjust target fields
3. **Validation Center** — Run validation; fix errors or proceed if valid
4. **Job Detail** — Start import; monitor progress, logs, errors
5. **Error Review** — Retry failed rows or download error report

## Supported Formats

- CSV (comma or semicolon, quoted fields)
- JSON (array of objects)
- Excel (.xlsx, .xls)
- TXT (auto-detected delimiter)

## Load Modes

| Mode | Behavior |
|------|----------|
| Insert only | Always create new records |
| Upsert | Update existing customer by email |
| Skip duplicates | Skip if email exists |
| Dry run | Validate and simulate without writing |

## Export

**Export Center** — Choose entity and format (CSV, JSON, XLSX).

## Data Quality

**Data Quality Center** scans for missing emails, duplicate emails, leads without owner.

## Migration

Use **Migration Center** to connect HubSpot/Salesforce via Integrations, then import historical Excel via Import Center.

## Rollback

If rollback is available on a completed job, use **Rollback Center** or Job Detail. Currently marks job as rolled back and logs snapshot count.
