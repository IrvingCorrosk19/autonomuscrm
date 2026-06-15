# DATA HUB — CERTIFICATION EVIDENCE

**Date:** 2026-06-13  
**Scope:** Close blockers from `DATA_HUB_ENTERPRISE_FINAL_AUDIT.md` (score 74/100, REJECTED)  
**Rule:** SKIP ≠ PASS. Each item requires code fix + real test + executed PASS + verifiable evidence.

---

## Validation Commands (executed)

```powershell
cd c:\Proyectos\autonomuscrm
dotnet build                                    # PASS — 0 errors
$env:INTEGRATION_TEST_RABBITMQ_HOST="127.0.0.1"
$env:INTEGRATION_TEST_RABBITMQ_PORT="5672"
$env:INTEGRATION_TEST_RABBITMQ_USER="autonomus"
$env:INTEGRATION_TEST_RABBITMQ_PASSWORD="autonomus123"
dotnet test AutonomusCRM.Tests --filter "FullyQualifiedName~DataHub"
```

**Result:** `Passed: 137 | Failed: 0 | Skipped: 0 | Duration: ~1m 11s`

---

## H-01 — RabbitMQ Operational

| Field | Detail |
|-------|--------|
| **Hallazgo** | 7 tests SKIPPED, 0 PASS — no broker; tests used consumer only |
| **Corrección** | `DataHubImportRabbitWorker.ProcessOneCycleAsync()` for all ops tests; poison JSON wrapped in try/catch; dispatcher throws in RabbitMQ mode; broker creds `autonomus/autonomus123`; prefer local broker before Testcontainers |
| **Tests** | `DataHubRabbitMqOperationalTests` (7) via `DataHubImportRabbitWorker` |
| **Result** | **7 PASS / 0 FAIL / 0 SKIP** |
| **Evidencia** | Broker: `127.0.0.1:5672` (Docker `autonomuscrm-rabbitmq`). Tests cover: poison→DLQ, tenant reject→DLQ, duplicate ack/idempotency, retry→DLQ, publish/consume/ack (worker), worker restart after dispatcher reset, lock contention nack |

---

## H-02 — SignalR Certification

| Field | Detail |
|-------|--------|
| **Hallazgo** | 0 SignalR tests |
| **Corrección** | `DataHubProgressHubTests` with `HubConnection` + `WebApplicationFactory`; hub auth extended to `Admin,Manager,Owner`; `SubscribeTenant` requires Admin/Owner |
| **Tests** | `DataHubProgressHubTests` (10 tests) |
| **Result** | **10 PASS / 0 FAIL / 0 SKIP** |
| **Evidencia** | Admin job/tenant subscribe, manager own-job, cross-user deny, cross-tenant deny, GUID tampering, tenant hijack block, owner tenant group, job group progress notification |

---

## M-01 — Migration E2E (Wizard / Scheduled / Manual)

| Field | Detail |
|-------|--------|
| **Hallazgo** | MissingOwner quality gate not proven across migration paths |
| **Corrección** | Shared `MigrationSyncCompleter` + quality API; bypass re-attempt blocked |
| **Tests** | `E2E_Migration_MissingOwner_BlocksSync` (Wizard, Scheduled, Manual) |
| **Result** | **3 PASS** |
| **Evidencia** | MissingOwner → quality FAIL → `migrationSyncBlocked=true`, no `migrationSyncCompleted`; second sync attempt still blocked |

---

## M-02 — Rollback E2E via API

| Field | Detail |
|-------|--------|
| **Hallazgo** | Integration rollback PASS but no API E2E |
| **Corrección** | `ExecuteRollbackAsync` uses `ExecuteInTransactionAsync` (Npgsql retry strategy); metadata copy for EF tracking |
| **Tests** | `E2E_ImportThenRollbackViaApi_VerifiesDatabaseState` |
| **Result** | **PASS** |
| **Evidencia** | Upload→analyze→autofix→validate→import→POST rollback→lead count decreases→job status `RolledBack` |

---

## M-03 — Key Rotation

| Field | Detail |
|-------|--------|
| **Hallazgo** | No v1/v2 interoperability tests |
| **Corrección** | Streaming encrypt reads key id from header; multi-key dictionary |
| **Tests** | `Encryption_KeyRotation_V1AndV2_AreInteroperable` |
| **Result** | **PASS** |
| **Evidencia** | Encrypt v1 → decrypt with v1+v2 keys → re-encrypt v2 → decrypt with v1 key still in ring |

---

## M-04 — Export Streaming (remove byte[])

| Field | Detail |
|-------|--------|
| **Hallazgo** | `ExportAsync` / `ExportErrorsAsync` returned `byte[]` |
| **Corrección** | Removed from `IDataHubExportService` and `DataHubExportService`; API uses `ExportToStreamAsync` only |
| **Tests** | `ExportStreaming_Csv_LargeRowCounts_CompletesWithoutFullBuffer` (100K, 500K, 1M) |
| **Result** | **3 PASS** (theory rows) |
| **Evidencia** | Streaming CSV completes without full-buffer memory spike |

---

## M-05 — Full Pipeline E2E

| Field | Detail |
|-------|--------|
| **Hallazgo** | Pipeline stages not covered end-to-end |
| **Corrección** | `E2E_FullPipeline_AllStagesPass` with autofix + validation gate |
| **Tests** | Upload, analyze, automap, autofix, rules, validate, preview, import, quality, export, template, migration sources, schedules, rollback |
| **Result** | **PASS** |
| **Evidencia** | All HTTP stages return 200/204; final job status `RolledBack` |

---

## LOW — SubscribeTenant / Dispatcher / COPY 100K

| ID | Hallazgo | Corrección | Test | Result |
|----|----------|------------|------|--------|
| L-01 | Manager could `SubscribeTenant` | `SubscribeTenant` requires Admin/Owner | `Manager_CannotHijackTenantWideSubscription` | PASS |
| L-02 | Silent in-process fallback on RabbitMQ dispatch fail | `EnqueueImportJobAsync` throws; log message fixed | `Dispatcher_RabbitMode_ThrowsWhenBrokerUnavailable` | PASS |
| L-03 | COPY chunk failure at scale | Transaction rollback via `ExecuteInTransactionAsync` | `BulkInsertRowsCopyAsync_100K_ChunkFailure_RollsBackAllRows` | PASS |

---

## Additional Production Fixes (supporting evidence)

| Area | File | Change |
|------|------|--------|
| Poison message | `DataHubImportWorker.cs` | `JsonException` → DLQ (no unhandled throw) |
| SignalR auth | `DataHubProgressHub.cs` | `[Authorize(Roles = "Admin,Manager,Owner")]` |
| Rollback TX | `DataHubEnterpriseServices.cs` | Npgsql execution strategy compatible transaction |
| Test quotas | `CustomWebApplicationFactory.cs` | Raised import/export quotas for E2E stability |
| RabbitMQ init | `DataHubRabbitMqOperationalTests.cs` | Local broker first; serial collection; DLQ passive assert |

---

## Summary Matrix

| Blocker | Tests | PASS | FAIL | SKIP |
|---------|-------|------|------|------|
| H-01 RabbitMQ | 7 | 7 | 0 | 0 |
| H-02 SignalR | 10 | 10 | 0 | 0 |
| M-01 Migration | 3 | 3 | 0 | 0 |
| M-02 Rollback E2E | 1 | 1 | 0 | 0 |
| M-03 Key rotation | 1 | 1 | 0 | 0 |
| M-04 Export scale | 3 | 3 | 0 | 0 |
| M-05 Full pipeline | 1 | 1 | 0 | 0 |
| LOW (3 items) | 3+ | PASS | 0 | 0 |
| **DataHub total** | **137** | **137** | **0** | **0** |

---

## Prerequisites for Reproduction

1. **PostgreSQL** reachable (local `:5432` or `INTEGRATION_TEST_CONNECTION_STRING`)
2. **RabbitMQ** on `127.0.0.1:5672` with credentials `autonomus` / `autonomus123` (Docker Compose service `autonomuscrm-rabbitmq`)
3. E2E CSV fixtures at `ops/certification/datahub-e2e/leads-valid.csv`

---

## Next Step

**Request hostile final audit** (`DATA_HUB_ENTERPRISE_FINAL_AUDIT.md` methodology) with mandate to reject unless Critical=0, High=0, Medium=0 blockers.
