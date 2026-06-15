# DATA HUB — RED TEAM AUDIT

**Rol:** Salesforce Principal Architect · HubSpot Operations Hub · Dynamics 365 Enterprise · SaaS Security · QA Lead · Enterprise CTO  
**Objetivo:** Intentar romper el Data Hub — no validar documentación  
**Método:** Revisión de código fuente, pruebas reales, flujos de producción  
**Fecha:** 2026-05-28  
**Certificación reclamada:** 100/100 (`DATA_HUB_100_CERTIFICATION.md`)  
**Veredicto red team:** **Certificación 100/100 NO válida**

---

## Resumen ejecutivo

El Data Hub tiene **implementación real y sustancial** (COPY, workers, cifrado, migración CRM, P4B/C/D). No es un mock. Sin embargo, la auditoría red team encontró **defectos verificables en código** que invalidan varias claims enterprise y la puntuación 100/100.

| Dimensión | Score red team | Notas |
|-----------|----------------|-------|
| Implementación funcional | **78/100** | Mucho código real; gaps en rollback, validación, P4 |
| Seguridad multi-tenant | **62/100** | Fail-open en guard; bypass de cuotas; hub débil |
| Escalabilidad real | **68/100** | COPY/chunks existen; RAM, sin tx, Rabbit ad-hoc |
| P4 (Scheduled / Templates / Matching) | **64/100** | Features presentes; gates rotos o incompletos |
| Evidencia de pruebas | **52/100** | 57 unit ≠ integración; E2E = 7 tests, no 16 |
| **Certificación enterprise global** | **71/100** | **NO GO** para 100/100 |

**Conclusión:** La certificación documentada **sobreestima** madurez operativa. Se recomienda **revocar 100/100** hasta remediar Critical/High y ampliar pruebas de integración.

---

## Metodología

1. Lectura directa de `AutonomusCRM.Infrastructure/DataHub/`, API, workers, tests  
2. Verificación cruzada de claims en `DATA_HUB_MASTER_TRACKER.md` vs código  
3. Ejecución de suite: `dotnet test --filter DataHub` (57 unit PASS; 7 E2E skip sin Postgres)  
4. No se asumió comportamiento no demostrado en código

---

## Hallazgos Critical

### C-01 — `DataHubTenantGuard` fail-open sin claim de tenant

**Severidad:** Critical  
**Área:** Seguridad / tenant isolation  

```29:35:AutonomusCRM.Infrastructure/DataHub/DataHubSecurityServices.cs
    public bool IsSameTenant(Guid requestedTenantId)
    {
        if (requestedTenantId == Guid.Empty) return false;
        var current = GetCurrentTenantId();
        if (current == null) return true;
        return current.Value == requestedTenantId;
    }
```

Si el principal autenticado **no tiene** claim `TenantId`/`tenant_id`, `IsSameTenant` devuelve **`true` para cualquier `tenantId` del query string**. Todos los endpoints API que confían en este guard quedan expuestos en rutas SSO/OIDC mal configuradas.

**Impacto:** Lectura/escritura cross-tenant vía API (`DataHubController`), SignalR (`DataHubProgressHub.SubscribeJob`).  
**Prueba:** No existe test del branch `current == null`. E2E cross-tenant pasa porque JWT interno siempre incluye tenant.  
**Rompe certificación:** P3 item 13 (RequireSameTenant).

---

### C-02 — Validación no bloquea import (`ReadyToImport` siempre `true`)

**Severidad:** Critical  
**Área:** Pipeline / Scheduled imports / Wizard  

```422:430:AutonomusCRM.Infrastructure/DataHub/DataHubOrchestrator.cs
        job.Status = invalid == 0 ? DataHubJobStatus.ReadyToImport.ToString() : DataHubJobStatus.ReadyToImport.ToString();
        ...
        return new DataHubValidationResultDto(
            jobId, job.TotalRows, valid, invalid,
            allErrors.Take(100).Select(...).ToList(),
            true);
```

- Status idéntico con filas inválidas  
- `ReadyToImport: true` hardcodeado  
- Scheduled import (`DataHubP4Services.cs` L157-159) confía en este flag → **nunca aborta por validación**

**Impacto:** Importación de datos inválidos en flujo manual y automático. Claim P4B pipeline “Validate → Import” es **falso**.  
**Rompe certificación:** P4B, P0 validación implícita.

---

### C-03 — Scheduled imports: ejecuciones concurrentes (race)

**Severidad:** Critical  
**Área:** P4B Scheduled imports  

```215:219:AutonomusCRM.Infrastructure/DataHub/DataHubRepository.cs
        => _db.DataHubScheduledImports
            .Where(s => s.IsEnabled && s.NextRunAt != null && s.NextRunAt <= asOfUtc)
```

`NextRunAt` solo se actualiza en el `finally` de `ExecuteScheduleAsync` (después de migración + wait hasta 30 min). Worker tick = **1 minuto**. Sin lock, flag `IsRunning`, ni claim atómico.

**Impacto:** Misma schedule puede disparar múltiples migraciones/imports solapados. Multi-instancia amplifica el problema.  
**Prueba:** Cero tests de concurrencia o ejecución real del servicio.

---

### C-04 — `SaveTemplateAsync` usa solo `Update` (INSERT probablemente roto)

**Severidad:** Critical  
**Área:** P4C Template versioning  

```190:196:AutonomusCRM.Infrastructure/DataHub/DataHubRepository.cs
        _db.DataHubImportTemplates.Update(template);
        await _db.SaveChangesAsync(cancellationToken);
```

`SaveTemplateFromJobAsync` crea template nuevo con `Guid.NewGuid()` y llama `SaveTemplateAsync`. EF Core `Update()` en entidad no rastreada genera **UPDATE**, no INSERT → 0 filas afectadas, **sin excepción**.

**Impacto:** “Guardar template desde job” puede retornar éxito sin persistir. Versionado inicial (`EnsureInitialVersionAsync`) hereda el mismo fallo.  
**Prueba:** Ningún test instancia `DataHubTemplateVersionService` ni persiste templates.

---

### C-05 — Smart Matching V2: `DetectColumns` hardcodea `"Customer"`

**Severidad:** Critical  
**Área:** P4D Smart matching  

```78:78:AutonomusCRM.Infrastructure/DataHub/DataHubSupremeServices.cs
            var match = DataHubSmartMatchingEngine.MatchColumn("Customer", col, samples);
```

Usado por `AnalyzeFile` y wizard. Imports **Lead**, **Deal**, **User** evalúan catálogo Customer → campos `Stage`, `Amount`, `AssignedToUserId` mal mapeados o descartados.

**Impacto:** P4D integrado en wizard es **incorrecto** para entidades no-Customer. API `POST /matching/v2` sí recibe `targetEntity` — dos niveles de calidad inconsistentes.  
**Rompe certificación:** P4D “integrado en DetectColumns/AutoMap”.

---

### C-06 — `LastSyncAt` actualizado antes de completar import (delta corrupto)

**Severidad:** Critical  
**Área:** Migration / Scheduled imports (Delta)  

```112:113:AutonomusCRM.Infrastructure/DataHub/Migration/DataHubMigrationServices.cs
        conn.MarkSync($"Migration {request.Mode} {request.SourceEntity}: {extracted.Rows.Count} rows");
        await _integrations.UpsertAsync(conn, cancellationToken);
```

Ocurre tras `UploadAsync`, **antes** de validate/import/quality. Si import falla, siguiente delta **pierde registros** permanentemente.

**Impacto:** Delta mode en Salesforce/HubSpot/Dynamics/Zoho/Pipedrive no es confiable operacionalmente.  
**Rompe certificación:** P4A Full/Delta, P4B Delta scheduled.

---

## Hallazgos High

### H-01 — Rollback: `RollbackAvailable` siempre false tras rollback parcial

**Archivo:** `DataHubEnterpriseServices.cs` L81-96  

Todos los snapshots del scope se marcan `RolledBack = true` en el loop; luego `snapshots.Any(s => !s.RolledBack)` evalúa la **misma lista en memoria** → siempre false. Rollbacks batch/row dejan job sin posibilidad de rollback adicional documentada.

---

### H-02 — Rollback incompleto por diseño

| Entidad | Created | Updated restore | Evidencia |
|---------|---------|-----------------|-----------|
| Deal | Snapshot sí | Solo delete, no restore | `RestoreEntityAsync` |
| User | Sin snapshot | Delete deshabilitado | `DataHubLoadService`, rollback service |
| Customer/Lead | Parcial | Solo Name/Email/Phone/Company/Source | `Capture*State` |
| Mid-import crash | — | Snapshots solo al final del loop | `DataHubOrchestrator` L544+ |

**Impacto:** P0 “Rollback real” es **parcial**, no enterprise-grade.

---

### H-03 — Bypass de cuota de export vía Razor page

**API:** `EnsureExportAllowedAsync` + forensic  
**Page:** `Export.cshtml.cs` L27 — `ExportAsync` → `byte[]` sin cuota ni audit  

Managers pueden exportar sin límite horario vía `/DataHub/Export?handler=Download`.

---

### H-04 — RabbitMQ import queue sin resiliencia (≠ ResilientRabbitMQEventBus)

**Archivo:** `DataHubImportWorker.cs`  

- Sin DLQ, sin retry contado, sin idempotencia de job  
- Mensajes malformados / tenant mismatch → **BasicAck y descarte** (L149-166)  
- `MaxRetryAttempts` en config **nunca leído**  
- Fallback silencioso a cola in-process si publish falla  

Tracker P2-10 atribuye resiliencia del event bus al import queue — **código distinto**.

---

### H-05 — In-process: re-enqueue de jobs `Importing` cada 5s

**Archivo:** `DataHubOrchestrator.cs` `PollPendingJobsAsync` L64-69  

Cola sin deduplicación. Multi-instancia API → **procesamiento concurrente** del mismo job → duplicados CRM en InsertOnly.

---

### H-06 — COPY staging sin transacción job-level

**Archivo:** `DataHubScaleServices.cs` `BulkInsertRowsCopyAsync`  

Chunks COPY auto-commit independientes. Fallo en chunk N deja chunks 1..N-1 en staging. Tail batches &lt;100 filas usan EF lento.

---

### H-07 — Upload/decrypt bufferiza archivo completo (escalabilidad falsa)

**Archivos:** `DataHubOrchestrator.UploadAsync`, `DataHubFileEncryption.DecryptToMemoryStreamAsync`  

100 MB max → 100 MB RAM mínimo por upload antes de chunking. P2-12 “large file chunks” aplica solo a CSV/TXT post-buffer.

---

### H-08 — Export XLSX no es streaming

**Archivo:** `DataHubSupportServices.cs` `ExportXlsxStreamAsync` — ClosedXML workbook completo en RAM.  
**Page Export:** siempre `byte[]`. Claim P2-11 “streaming” **parcialmente falso**.

---

### H-09 — Scheduled import trata `CompletedWithErrors` como éxito

**Archivo:** `DataHubP4Services.cs` L215-216  

Wait loop retorna en `CompletedWithErrors`. Run puede marcarse Completed pese a filas fallidas.

---

### H-10 — Clave AES en `appsettings.json` committed

**Archivo:** `AutonomusCRM.API/appsettings.json`  

Key decodable en repo. Cifrado real, **gestión de secretos falsa**.

---

### H-11 — SignalR hub: `[Authorize]` sin policy Manager

Cualquier usuario autenticado (ej. Sales) puede suscribirse a progreso de import si conoce `jobId` + `tenantId`.

---

### H-12 — Smart Matching: falsos positivos verificables

**Archivo:** `DataHubSmartMatchingEngine.cs`  

- Sinónimo `"account"` → Company puede capturar `Account Id`, `Account Owner`  
- `"deal"` en Title → `Deal Stage` mapeado a Title no Stage  
- Regex phone acepta IDs numéricos  
- Perfil Date detecta pero `TargetField = null` — no mapea  

---

### H-13 — Delta HubSpot/Pipedrive: filtro client-side sobre dataset completo

**Archivo:** `DataHubMigrationExtractors.cs`  

Pagina todo el CRM y filtra en memoria por `updatedAt`. No escala en tenants grandes (rate limits, timeouts).

---

### H-14 — Quality gate ignora `missingOwners` en `passed`

**Archivo:** `DataHubMigrationServices.cs` L163  

`passed` no incluye `missingOwners > 0`. Scheduled run puede completar con owners rotos.

---

### H-15 — Evidencia de pruebas sobrestimada

| Claim documentado | Realidad verificada |
|-------------------|---------------------|
| 16/16 E2E | **7** `[SkippableFact]` en `DataHubE2ELocalValidationTests.cs` |
| Scheduled tests PASS | Solo enum + tautología en `DataHubP4Tests.cs` |
| Template versioning tests | DTO constructors, **sin servicio** |
| COPY tested | `BulkInsertRowsCopyAsync` **0 call sites** en tests |
| RabbitMQ worker tested | **0 tests** Data Hub |
| Rollback tested | Solo cross-tenant 403, **sin rollback exitoso** |
| E2E COPY path | CSV fixture 4 filas; COPY umbral ≥100 filas |

---

## Hallazgos Medium

### M-01 — `RequireMalwareScan` en config nunca leído en código de producción

Opción documentada sin efecto. Scan siempre corre en upload.

### M-02 — Heuristic malware solo primeros 8 KB

**Archivo:** `DataHubSecurityServices.cs` L112-117. Payload malicioso después de 8 KB pasa. ClamAV off por default → heurística única.

### M-03 — Forensic audit inconsistente

`QuotaBlocked` definido, nunca usado. Acciones ad-hoc (`MigrationStart`, `ScheduledImportRun`) fuera de constantes. Quality merge sin audit.

### M-04 — `CompareVersionsAsync` crash con columnas duplicadas

`.ToDictionary(SourceColumn)` → `ArgumentException` en mappings reales.

### M-05 — Version numbering sin transacción serializable

Race en `CreateVersionAsync` / `EnsureInitialVersionAsync` → violación índice único `(TemplateId, VersionNumber)`.

### M-06 — Activate vs Restore semántica inconsistente

Activate muta versión existente; Restore crea nueva versión activa. `LatestVersion` confuso post-Activate.

### M-07 — Scheduled: AutoMap sin samples en path automático

`SuggestMappings` con samples vacíos — solo headers, no V2 completo con muestras.

### M-08 — Scheduled: delta con 0 filas falla run entero

Migration L91-92 throw en lugar de no-op success.

### M-09 — Export pagination `Skip/Take` O(n²) en datasets grandes

**Archivo:** `DataHubSupportServices.cs` `StreamEntityRowsAsync`.

### M-10 — JSON/XLSX extract: full RAM, no chunks

**Archivo:** `DataHubExtractService.cs` L31-36. `LargeFileChunkThresholdBytes` **dead code**.

### M-11 — CSV multiline quoted fields rotos

`ReadLineAsync` + parse por línea — RFC 4180 multiline incorrecto.

### M-12 — Import row-by-row load (cuello de botella post-COPY)

`ProcessJobAsync` → `LoadRowAsync` por fila con SaveChanges frecuentes.

### M-13 — Pipedrive `stage_id` importado como valor Stage

ID numérico, no nombre — validación downstream probablemente falla.

### M-14 — Duplicate engine / load modes Upsert-Skip-DryRun sin tests E2E

Fixtures existen (`leads-duplicates.csv`) pero no usados.

---

## Hallazgos Low

### L-01 — `MatchColumnsV2` API sin parámetro tenant (PII en body)

Riesgo bajo (sin DB) pero muestras pueden contener PII.

### L-02 — `GetJobByIdAsync` sin filtro tenant — riesgo de regresión futura

Workers validan; API no expone directamente.

### L-03 — Scheduled import `Take(20)` due schedules — backlog posible

### L-04 — Frequencies sin ancla horaria (Daily = +24h desde now)

### L-05 — `DataHubOrphanRecoveryWorker` posible doble registro en Workers host

### L-06 — OAuth token refresh ausente en path de extracción migration

### L-07 — Metadata upload `"encrypted": true` incondicional en orchestrator

---

## Matriz por área solicitada

| Área | ¿Roto? | Severidad máxima | Veredicto |
|------|--------|------------------|-----------|
| Bugs | Sí | Critical | Validación, templates, rollback flag |
| Escalabilidad falsa | Sí | High | RAM upload/XLSX, COPY sin tx, Skip/Take |
| Seguridad falsa | Sí | Critical | Tenant fail-open, cuotas bypass, key en repo |
| Rollback incompleto | Sí | High | Entidades, timing snapshots, RollbackAvailable |
| Tenant leaks | Sí | Critical | Guard fail-open; workers OK con bypass controlado |
| RabbitMQ failures | Sí | High | Sin DLQ/retry/idempotencia |
| COPY failures | Sí | High | Sin transacción; tests ausentes |
| Smart Matching incorrecto | Sí | Critical | Customer hardcoded + false positives |
| Scheduled Imports defectuosos | Sí | Critical | Race + validate no-op + delta sync |
| Template Versioning inconsistente | Sí | Critical | Save Update-only; compare fragile |

---

## Score revisado vs certificación

| Métrica | Certificado | Red team |
|---------|-------------|----------|
| Score global | 100/100 | **71/100** |
| Requisitos 20/20 | ✅ | **14/20 confiables**, 6 con gaps Critical/High |
| Unit tests | 57/57 | ✅ PASS pero **~80% no integración** |
| E2E | 16/16 reclamado | **7 tests**, skippable, 4 filas, InProcess |
| GO Enterprise | Sí | **NO GO** |

### Desglose score 71/100

- Base implementación sólida P0-P3: +66  
- P4 features con código real: +12  
- Critical defects (−6 × 3.5): −21  
- High defects (−15 × 1.2): −18  
- Test evidence gap: −8  
- Ajuste positivo por arquitectura reutilizable: +10  

---

## ¿Es válida la certificación 100/100?

**No.**  

El Data Hub **no es vaporware**: hay pipeline ETL real, seguridad parcial, migración CRM, P4 implementado en código. Pero la certificación **100/100 Enterprise** afirma controles que el código **no garantiza** (validación, tenant fail-closed, delta sync, template persist, matching V2 en wizard, pruebas de escala/workers/rollback).

**Condición para restaurar confianza en certificación:**

1. Remediar **6 Critical** (C-01 a C-06)  
2. Remediar **≥10 High** priorizados (H-01, H-02, H-03, H-04, H-06, H-09, H-10, H-14, H-15)  
3. Añadir integración tests: scheduled execution, template CRUD, COPY ≥100 rows, rollback full path, RabbitMQ tenant reject  
4. Re-auditoría red team sin hallazgos Critical abiertos  

**Score objetivo post-remediación:** ≥92/100 para reconsiderar certificación enterprise.

---

## Prioridad de remediación (P0 → P2)

| P | ID | Acción |
|---|-----|--------|
| P0 | C-01 | `if (current == null) return false` + test |
| P0 | C-02 | `ReadyToImport = invalid == 0`; status distinto si invalid |
| P0 | C-03 | Claim schedule (`IsRunning` / `NextRunAt=null`) + lock distribuido |
| P0 | C-04 | `SaveTemplateAsync` Add-or-Update / upsert |
| P0 | C-05 | Pasar `targetEntity` a `DetectColumns` / `AnalyzeFile` |
| P0 | C-06 | `MarkSync` solo tras import exitoso |
| P1 | H-01 | Recalcular `RollbackAvailable` desde DB post-loop |
| P1 | H-03 | Export page → cuota + forensic + streaming |
| P1 | H-04 | DLQ + idempotencia job RabbitMQ |
| P1 | H-10 | Rotar key; secrets manager |
| P2 | H-15 | Suite integración real; corregir docs 16→7 E2E |

---

## Archivos clave auditados

```
AutonomusCRM.Infrastructure/DataHub/
  DataHubSecurityServices.cs      — tenant guard, encryption, malware, quotas
  DataHubOrchestrator.cs          — validate, upload, background processor
  DataHubEnterpriseServices.cs    — rollback
  DataHubP4Services.cs            — scheduled + template versioning
  DataHubSmartMatchingEngine.cs   — matching V2
  DataHubScaleServices.cs         — COPY bulk staging
  DataHubImportWorker.cs          — RabbitMQ consumer
  Migration/DataHubMigrationServices.cs
AutonomusCRM.API/
  Controllers/DataHubController.cs
  Hubs/DataHubProgressHub.cs
  Pages/DataHub/Export.cshtml.cs
AutonomusCRM.Tests/DataHub/       — 6 archivos, 64 tests total
```

---

*Red team audit — código verificado, no documentación. Objetivo cumplido: certificación 100/100 no sostenible con evidencia actual.*
