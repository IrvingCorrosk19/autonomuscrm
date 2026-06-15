# Data Hub — Test Plan

## Automated (14 tests — PASS)

| Area | Tests |
|------|-------|
| CSV parsing | Quoted commas, delimiter detection |
| Transform | Email, trim |
| Security | Path traversal, CSV injection |
| Field catalog | Synonym mapping |
| Intelligence | Email confidence, Lead detection |
| Rules engine | Default source rule |
| Constants | 100MB limit, Excel extension |

Run: `dotnet test --filter FullyQualifiedName~DataHub`

## Manual QA checklist

### Import wizard
- [ ] Upload CSV leads → wizard step 2 shows Smart Analysis
- [ ] Column detection shows confidence %
- [ ] Map columns → save → validate
- [ ] Auto-fix reduces warnings
- [ ] Import completes with progress in Jobs Monitor

### Formats
- [ ] CSV, XLSX, JSON, TXT delimited

### Security
- [ ] Tenant A cannot open Tenant B job URL
- [ ] Path traversal filename rejected
- [ ] Manager role required

### Error center
- [ ] Retry failed rows only (not successful)
- [ ] Export errors CSV

### Rollback
- [ ] Rollback marks job RolledBack

### Performance (smoke)
- [ ] 10,000 row CSV parses and stages
- [ ] Job queue processes without timeout

## Regression
- [ ] Legacy `/api/import/leads` still works
- [ ] Integrations sync unchanged
