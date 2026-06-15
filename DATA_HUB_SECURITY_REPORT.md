# Data Hub — Security Report

## Controls Implemented

| Control | Implementation |
|---------|------------------|
| Authentication | All API/UI endpoints require auth |
| Authorization | `RequireManager` (Admin, Manager) on Data Hub |
| Tenant isolation | EF query filters + API tenant claim check |
| File size limit | 100 MB (`DataHubConstants.MaxFileBytes`) |
| Extension whitelist | .csv, .json, .xlsx, .xls, .txt |
| Path traversal | Reject `..`, `/`, `\` in filenames; storage path validation |
| CSV/Excel injection | `SanitizeCellValue` prefixes `=+-@` cells |
| IDOR on jobs | `GetJobAsync(tenantId, jobId)` — cross-tenant returns null |
| Staging before load | No direct insert to CRM tables without validation |
| Audit | `DataHubImportLogs` + job metadata |

## Residual Risks

1. **Rollback** does not yet delete created entities — only marks job RolledBack
2. **Rate limiting** — uses global API rate limiter, no Data Hub-specific quota
3. **Virus scan** — not integrated (placeholder)
4. **SuperAdmin audit** — Admin can pass different tenantId in API if claim mismatch logic allows — tighten with dedicated audit policy

## Recommendations

- Add `RequireSameTenant` policy to all Data Hub endpoints
- Wire ClamAV or cloud scan on upload
- Encrypt stored files at rest in `DataHub:StoragePath`
