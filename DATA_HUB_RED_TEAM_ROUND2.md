# DATA HUB — RED TEAM AUDIT ROUND 2

**Fecha:** 2026-05-28  
**Método:** Código fuente + ejecución de tests + Postgres real  
**Documentos ignorados:** `DATA_HUB_MASTER_TRACKER.md`, `DATA_HUB_100_CERTIFICATION.md`, `DATA_HUB_RED_TEAM_REMEDIATION_REPORT.md`

---

## Ejecución verificable (esta sesión)

```text
dotnet build                              → PASS (0 errors)
dotnet test --filter FullyQualifiedName~DataHub → 77 PASS / 7 FAIL / 84 total
dotnet test --filter Category=DataHubRemediation → 3 PASS / 0 FAIL (Postgres)
```

**7 fallos:** todos en `DataHubE2ELocalValidationTests` — `WebApplicationFactory` no levanta host:

```text
System.InvalidOperationException: The entry point exited without ever building an IHost.
```

---

## Respuestas ejecutivas

| Pregunta | Respuesta |
|----------|-----------|
| **¿Existe algún Critical abierto?** | **No** — Los 6 Critical originales (fail-open tenant, validate siempre ready, race schedule, template UPDATE-only, Customer hardcode, MarkSync pre-import) están corregidos en código. |
| **¿Existe algún High abierto?** | **Sí — 6** (R2-H-01..R2-H-06, evidencia abajo). |
| **¿La remediación cerró los hallazgos?** | **Parcialmente.** Critical core cerrado en código. Varios High mejorados pero con bypasses, locks in-process, y tests insuficientes. |
| **¿Certificación 100/100 válida?** | **No.** E2E rotos, rollback/RabbitMQ sin prueba real, High abiertos. |

**Score estimado Round 2: 82/100** — mejora vs auditoría previa (71/100), **NO GO** para 100/100.

---

## Validación de las 13 áreas

### 1. Tenant Guard — CERRADO

**Evidencia código** (`DataHubSecurityServices.cs` L33):

```csharp
if (current == null) return false;
```

**Evidencia API:** `DataHubController` — `IsSameTenant(tenantId)` en cada endpoint.  
**Evidencia test:** `DataHubTenantGuardTests.IsSameTenant_DeniesWhenTenantClaimMissing_FailClosed` — **PASS**.

| Severidad residual | — |

---

### 2. Validation Gate — PARCIAL (High reabierto)

**Path principal cerrado** (`DataHubOrchestrator.ValidateAsync` L458-469):

- `ready = invalid == 0`
- Status `ValidationFailed` si hay inválidos
- DTO retorna `ready` (no hardcoded `true`)
- `StartImportAsync` rechaza `ValidationFailed`
- Scheduled import aborta si `!validation.ReadyToImport` (`DataHubP4Services.cs` L173-175)

**Bypass verificado** (`RetryFailedRowsAsync` L639-649):

```csharp
foreach (var row in rows.Where(r => r.Status == Failed))
    row.Status = Valid;  // sin re-ejecutar ValidateAsync
job.Status = ReadyToImport;
return await StartImportAsync(...);
```

Expuesto en API: `POST /api/datahub/jobs/{id}/retry`.

| Hallazgo | **R2-H-01** — Retry bypass validation gate |
| Severidad | **High** |

**Test débil:** `ValidationResult_ReadyToImportFalseWhenInvalidRowsExist` es aritmética tautológica; no invoca orchestrator.

---

### 3. Scheduled Lease — CERRADO

**Evidencia código:** `TryClaimScheduledImportAsync` — UPDATE SQL atómico con `IsRunning`, `RunningLeaseUntil`, `ActiveRunId`. Claim antes de ejecutar; release en `finally`.

**Evidencia test Postgres:** `TryClaimScheduledImport_AllowsOnlyOneConcurrentClaim` — 16 hilos concurrentes, **exactamente 1 claim** — **PASS**.

| Severidad residual | — |

---

### 4. Template Persistence — CERRADO

**Evidencia código** (`DataHubRepository.SaveTemplateAsync` L194-198): add-or-update con `AnyAsync`.

**Evidencia test Postgres:** `SaveTemplateAsync_InsertsNewTemplate` — fila persistida — **PASS**.

| Severidad residual | **Low** — version numbering sin tx serializable (race en versiones) |

---

### 5. Smart Matching — CERRADO

**Evidencia código:** `DetectColumns(targetEntity, ...)` → `MatchColumn(targetEntity, col, samples)` — sin hardcode `"Customer"`.

**Evidencia tests:** `DetectColumns_UsesTargetEntity_NotHardcodedCustomer` (Deal/Lead/User/Customer/User) — **PASS**.  
`SmartMatching_AccountId_NotMappedToCompany` — **PASS**.

| Severidad residual | **Low** — perfil Date → `TargetField = null`; edge cases phone/ID numérico |

---

### 6. Delta Sync — PARCIAL (High reabierto)

**C-06 original cerrado:** `MarkSync` ya no se llama en `StartMigrationAsync`. Sync solo vía `MigrationSyncCompleter` cuando `job.Status == Completed`.

**Scheduled path:** quality gate → `TryCompleteMigrationSyncAsync` — correcto.

**Gap verificado:** path manual/wizard — `ProcessJobAsync` L618-619 sincroniza en `Completed` **sin** exigir `ValidateMigrationQualityAsync`. Import puede completar con owners rotos si validation rules no los cubren; delta avanza igual.

| Hallazgo | **R2-H-02** — Manual delta sync sin quality gate obligatorio |
| Severidad | **High** |

**HubSpot delta:** Search API server-side (`lastmodifieddate GT`) — OK.  
**Pipedrive delta:** `sort=update_time DESC` + early pagination stop — mejora, no filtro server-side nativo — **Medium**.

---

### 7. Rollback — PARCIAL (High reabierto)

**Código presente:** snapshots incrementales, `RollbackAvailable` desde DB, delete Created, restore Customer/Lead/Deal parcial.

**Gaps verificados:**

- `RestoreEntityAsync` — **sin case User** (L149-158)
- Deal restore: Title/Amount only
- **0 tests PASS** de rollback exitoso en DB
- `E2E_CrossTenant_RollbackForbidden` existe pero **FAIL** (host no levanta)

| Hallazgo | **R2-H-03** — Rollback sin evidencia de ejecución real |
| Severidad | **High** |

---

### 8. RabbitMQ — PARCIAL (High reabierto)

**Código presente** (`DataHubImportWorker.cs`): DLQ queue, retry headers, poison → DLQ, skip Completed jobs, processing lock, tenant mismatch → DLQ.

**Gaps verificados:**

- **0 tests** Data Hub contra RabbitMQ/Testcontainer
- Fallback silencioso a cola in-process si publish falla (`DataHubImportDispatcher` L71)
- Republish manual (ack + republish) vs dead-letter exchange

| Hallazgo | **R2-H-04** — Resiliencia RabbitMQ no verificada operacionalmente |
| Severidad | **High** |

---

### 9. COPY — PARCIAL

**Código:** staging en `ExecuteInTransactionAsync`; delete staging en catch.

**Evidencia test Postgres:** `BulkInsertRowsCopyAsync_PersistsStagingRows` — 120 filas — **PASS**.

**Gap:** no hay test que falle en chunk N y verifique rollback de chunks previos. COPY vía Npgsql crudo — participación en tx EF **no demostrada**.

| Severidad residual | **Medium** (R2-M-03) |

---

### 10. SignalR — CERRADO

**Evidencia código** (`DataHubProgressHub.cs`):

```csharp
[Authorize(Policy = AuthorizationPolicies.RequireManager)]
// SubscribeJob: IsSameTenant + GetJobAsync(tenantId, jobId)
```

| Severidad residual | **Low** — Manager del tenant ve cualquier job (sin ownership por usuario) |

---

### 11. Export Streaming — CERRADO (con reserva performance)

**Evidencia:** `Export.cshtml.cs` y `DataHubController.Export` — quota + forensic + `ExportToStreamAsync`.  
**XLSX:** Open XML `OpenXmlWriter` — test `XlsxExportStreaming_WritesValidWorkbookWithoutFullBuffer` (251 filas) — **PASS**.

**Gap:** `StreamEntityRowsAsync` usa `Skip(offset).Take(batch)` — O(n²) en tablas grandes.

| Severidad residual | **Medium** (R2-M-04) |

---

### 12. Encryption — PARCIAL

**H-10 cerrado en repo:** `appsettings.json` → `"EncryptionKeys": {}` (sin claves committed).

**Upload:** temp file en disco (`DataHubOrchestrator` L173+) — no MemoryStream completo en upload path.

**Gaps:**

- `EncryptToFileAsync` — `ms.ToArray()` bufferiza plaintext completo
- `DecryptToBytesAsync` — `File.ReadAllBytesAsync` carga ciphertext completo

**Test:** `DataHubFileEncryptionTests.EncryptDecrypt_RoundTrip_PreservesContent` — **PASS** (archivo pequeño).

| Severidad residual | **Medium** (R2-M-01) |

---

### 13. Malware Scan — PARCIAL

**Código** (`HeuristicMalwareScanner.ScanAsync` L120-127):

- Copia stream completo a `MemoryStream` → RAM = tamaño archivo
- Heurística script solo primeros **8192 bytes**

**Tests:** EICAR unit — **PASS**. E2E Eicar — **FAIL** (host).

| Severidad residual | **Medium** (R2-M-02) |

---

## Hallazgos adicionales (Round 2)

| ID | Hallazgo | Severidad | Evidencia |
|----|----------|-----------|-----------|
| **R2-H-05** | Job lock in-process only (`ConcurrentDictionary`) | **High** | `DataHubJobProcessingLock.cs` — multi-instancia API → duplicate job processing |
| **R2-H-06** | Orphan recovery → `ReadyToImport` sin re-validar | **High** | `DataHubImportWorker.cs` L284-290 |

---

## Clasificación consolidada

### Critical — 0 abiertos

Ningún defecto verificado reabre C-01..C-06 al mismo nivel de impacto original.

### High — 6 abiertos

R2-H-01, R2-H-02, R2-H-03, R2-H-04, R2-H-05, R2-H-06

### Medium — 4

| ID | Descripción |
|----|-------------|
| R2-M-01 | Encrypt/decrypt bufferizan archivo completo |
| R2-M-02 | Malware heurístico 8KB + buffer RAM |
| R2-M-03 | COPY tx rollback no probado con fallo mid-import |
| R2-M-04 | Export pagination O(n²) |

### Low — 3

| ID | Descripción |
|----|-------------|
| R2-L-01 | Template version race |
| R2-L-02 | SignalR sin ownership por usuario |
| R2-L-03 | Smart matching Date → null field |

---

## ¿Remediación cerró hallazgos originales?

| ID original | Código | Test real | Veredicto |
|-------------|--------|-----------|-----------|
| C-01 Tenant fail-open | Corregido | Unit PASS | **Cerrado** |
| C-02 Validate bypass | Path principal OK | Tautología only | **Parcial** — R2-H-01 |
| C-03 Schedule race | Lease atómico | Postgres 3/3 PASS | **Cerrado** |
| C-04 Template INSERT | Add-or-update | Postgres PASS | **Cerrado** |
| C-05 Customer hardcode | targetEntity | Unit PASS | **Cerrado** |
| C-06 MarkSync pre-import | Post-Completed sync | Sin test E2E sync | **Cerrado** — R2-H-02 en quality manual |
| H-01..H-15 | Mixto | Cobertura estrecha | **Parcial** |

---

## Evidencia de tests — tabla honesta

| Área | Test que demuestra fix | Resultado |
|------|------------------------|-----------|
| Tenant Guard | `IsSameTenant_DeniesWhenTenantClaimMissing_FailClosed` | PASS |
| Schedule lease | `TryClaimScheduledImport_AllowsOnlyOneConcurrentClaim` | PASS (Postgres) |
| Template INSERT | `SaveTemplateAsync_InsertsNewTemplate` | PASS (Postgres) |
| COPY insert | `BulkInsertRowsCopyAsync_PersistsStagingRows` | PASS (Postgres) |
| Smart Matching | `DetectColumns_UsesTargetEntity_*` | PASS |
| XLSX streaming | `XlsxExportStreaming_WritesValidWorkbookWithoutFullBuffer` | PASS |
| Validation gate | — | **Sin test orchestrator** |
| Rollback execution | — | **Sin test** |
| RabbitMQ worker | — | **Sin test** |
| E2E full flow | `E2E_FullLeadImportFlow_Passes` | **FAIL** (7/7 E2E) |
| Malware E2E | `E2E_EicarUpload_Blocked` | **FAIL** |

---

## Conclusión

La remediación **elimina los Critical originales en el código de producción**. Permanece deuda **High** verificable en bypasses de validación/sync, locks multi-instancia, rollback sin prueba, y RabbitMQ sin verificación operacional.

**Certificación 100/100: NO VÁLIDA** con la evidencia actual.

**Próximo paso mínimo para re-evaluar:** cerrar R2-H-01..H-06 con tests de integración que demuestren el fix (no tautologías ni solo compilación).

---

*Generado exclusivamente a partir de código fuente y ejecución de tests — 2026-05-28.*
