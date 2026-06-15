# Data Hub — Roadmap

## Phase 1 (Done)
- Staging tables, ETL pipeline, async jobs, UI hub, export, quality scan

## Phase 2 (Next)
- PostgreSQL COPY + staging table bulk load for 100K–1M rows
- SignalR real-time job progress
- Full rollback (delete created entities from snapshots)
- Load support for WorkflowTask, Policy, Workflow
- `RequireSameTenant` on all endpoints

## Phase 3
- RabbitMQ job queue (optional, for multi-instance API)
- Zoho, Dynamics, Pipedrive migration connectors with pagination
- Import templates UI (save/load mappings)
- Scheduled sync jobs in Workers
- ClamAV virus scan on upload

## Phase 4
- AI-assisted column mapping
- Data lineage to Knowledge Graph / Business Memory
- Cross-tenant SuperAdmin audit dashboard (read-only, no data mix)
