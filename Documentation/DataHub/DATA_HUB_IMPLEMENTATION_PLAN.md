# Data Hub — Implementation Plan (Supreme Phase)

## Phase 1 — Done ✅

- Staging tables + migration `DataHubEnterpriseEtl`
- ETL pipeline (Extract, Transform, Validate, Load)
- 10-step visual wizard
- Smart Analysis with confidence %
- Rules Engine (IF/THEN defaults)
- Auto-fix button
- Data Quality Score 0–100
- Templates Center
- Job queue + BackgroundService
- 14 unit tests
- API endpoints for analyze, autofix, metrics, cleaning summary

## Phase 2 — Next (scale & parity)

| Task | Effort | Impact |
|------|--------|--------|
| PostgreSQL COPY bulk load | M | 1M rows |
| SignalR live progress | S | UX |
| Full entity rollback (delete) | M | Trust |
| RabbitMQ job messages in Workers | M | Multi-instance |
| LLM-powered analysis (optional AI:Enabled) | S | Accuracy |
| RequireSameTenant on all endpoints | S | Security |
| Visual rule builder (drag-drop) | L | UX |

## Phase 3 — Competitive parity

- Salesforce Data Import Wizard parity: matching rules, duplicate rules UI
- HubSpot-style column mapping with live preview
- Scheduled recurring imports
- ClamAV scan on upload

## Files added/modified (Supreme)

```
Application/DataHub/DataHubSupremeDtos.cs
Infrastructure/DataHub/DataHubSupremeServices.cs
Infrastructure/DataHub/DataHubOrchestrator.cs (extended)
Pages/DataHub/Wizard.cshtml(+cs)
Pages/DataHub/Rules.cshtml(+cs)
Pages/DataHub/Templates.cshtml(+cs)
wwwroot/css/flow-datahub.css
```
