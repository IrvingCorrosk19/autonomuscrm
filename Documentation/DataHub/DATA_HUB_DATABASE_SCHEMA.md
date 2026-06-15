# Data Hub — Database Schema

Migration: `DataHubEnterpriseEtl`

## Tables

### DataHubImportJobs
Primary job record. Tracks file, target entity, status, progress counters, rollback flag.

| Column | Type | Notes |
|--------|------|-------|
| Id | uuid PK | |
| TenantId | uuid | Required, indexed |
| CreatedByUserId | uuid | |
| FileName | varchar(500) | |
| TargetEntity | varchar(50) | Customer, Lead, Deal, User, etc. |
| Status | varchar(50) | Uploaded → Completed / Failed / RolledBack |
| LoadMode | varchar(50) | InsertOnly, Upsert, DryRun, ... |
| TotalRows, ProcessedRows, SuccessRows, FailedRows, SkippedRows | int | |
| DetectedColumns | jsonb | |
| Metadata | jsonb | |
| RollbackAvailable | bool | |

### DataHubImportRows
Staging rows with raw + transformed JSONB payloads.

### DataHubImportErrors
Per-row validation/load errors with retry flag.

### DataHubImportMappings
Column → field mappings per job.

### DataHubImportBatches
Batch tracking for large jobs (1K–1M rows).

### DataHubImportTemplates
Reusable mapping templates per tenant.

### DataHubTransformationRules / DataHubValidationRules
Tenant-configurable ETL rules.

### DataHubRollbackSnapshots
Entity IDs created per job for rollback audit.

### DataHubImportLogs
Job-level operational logs.

## Indexes

- `(TenantId, CreatedAt)` on jobs
- `(TenantId, Status)` on jobs
- `(JobId, RowNumber)` on rows and errors

## Entity Mapping Notes

AutonomusCRM has **no separate Contact/Company/Activity tables**. Company is a string on Lead/Customer. Data Hub maps accordingly.
