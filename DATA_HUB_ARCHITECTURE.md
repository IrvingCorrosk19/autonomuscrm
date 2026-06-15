# Data Hub Enterprise Supreme — Architecture

## Product vision

Data Hub is a **visual, guided data onboarding system** for non-technical administrators. Zero SQL, zero ETL jargon in the UI.

## Layers (reused from AutonomusCRM)

| Layer | Path |
|-------|------|
| Application | `AutonomusCRM.Application/DataHub/` |
| Infrastructure | `AutonomusCRM.Infrastructure/DataHub/` |
| API | `Controllers/DataHubController.cs` |
| UI | `Pages/DataHub/` + `wwwroot/css/flow-datahub.css` |

## Supreme components (new)

| Service | Role |
|---------|------|
| `DataHubIntelligenceService` | Smart column detection + confidence % + entity suggestion |
| `DataHubRulesEngineService` | Visual IF/THEN rules (no code) |
| `DataHubAutoFixService` | One-click data cleaning |
| `DataHubQualityScoreService` | Tenant data quality score 0–100 |
| `DataHubJobQueue` | Async job channel + BackgroundService worker |

## 10-step wizard (`/DataHub/Wizard`)

1. Upload → 2. Analyze → 3. Detect type → 4. Map → 5. Rules → 6. Validate → 7. Preview → 8. Confirm → 9. Import → 10. Finish

## Pipeline (unchanged core, enhanced)

```
File → Extract → Staging → AI Analysis → Map → Rules → Transform → Validate → AutoFix → Preview → Load → Audit
```

## Integration (no duplication)

- **Load** reuses `CreateCustomerCommand`, `CreateLeadCommand`, `CreateDealCommand`, `CreateUserCommand`
- **Legacy** `ImportController` unchanged
- **Integrations** HubSpot/Salesforce remain in `/Integrations`
- **Identity** quality reuses tenant-scoped EF queries

## Async processing

- `DataHubBackgroundProcessor`: queue dequeue + poll pending jobs every 5s
- Batch size: 1,000 rows
- Max file: 100 MB
- RabbitMQ: optional Phase 3 (Workers poll staging jobs today via API host)

## Multi-tenant

All staging entities: `TenantId` + EF global query filters + API claim validation.
