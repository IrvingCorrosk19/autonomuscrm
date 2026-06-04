# DATABASE ASSESSMENT — AutonomusCRM / AutonomusFlow

**Fecha:** 2026-06-04  
**Motor:** PostgreSQL 18.0 (local)  
**Base de datos:** `autonomuscrm`  
**Conexión descubierta:** `AutonomusCRM.API/appsettings.Development.json`  
`Host=localhost;Port=5432;Database=autonomuscrm;Username=postgres`  
**Método:** Solo lectura (`psql`, `pg_catalog`, `pg_stat_*`) — sin cambios aplicados.

---

## Información General

| Métrica | Valor |
|---------|-------|
| Versión PostgreSQL | **18.0** (x86_64-windows, msvc) |
| Tamaño total BD | **11 MB** |
| Tablas (`public`) | **46** |
| Índices (`public`) | **126** (~2,74 índices/tabla) |
| Vistas | **0** |
| Funciones | **0** |
| Procedimientos | **0** |
| Triggers | **0** |
| Extensiones | `plpgsql` (por defecto); **`pg_stat_statements` NO instalada** |
| Foreign Keys (DB) | **0** (relaciones solo en EF, sin FK físicas) |
| Tablas sin PK | **0** |
| Migraciones EF aplicadas | **9** filas en `__EFMigrationsHistory` |

---

## Contexto de carga (desarrollo)

Base de datos **pequeña** (demo/seed CEO_DEMO). Las estadísticas `pg_stat_*` reflejan sesión de desarrollo activa (p. ej. **30.477 seq_scan** en `MemoryEmbeddings` con 30 filas — búsqueda semántica en memoria + OpenTelemetry en consola).

---

## Top 50 Tablas Más Grandes

*(46 tablas totales — listado completo)*

| # | Tabla | Registros (est.) | Tamaño total | % del total |
|---|-------|------------------|--------------|-------------|
| 1 | DomainEvents | 0 | 160 kB | 6,99% |
| 2 | BusinessMemories | 30 | 112 kB | 4,90% |
| 3 | MemoryEmbeddings | 30 | 112 kB | 4,90% |
| 4 | Deals | 45 | 112 kB | 4,90% |
| 5 | Customers | 50 | 96 kB | 4,20% |
| 6 | CustomerMemoryProfiles | 35 | 80 kB | 3,50% |
| 7 | CustomerContracts | 8 | 80 kB | 3,50% |
| 8 | Policies | 0 | 80 kB | 3,50% |
| 9 | Users | 5 | 64 kB | 2,80% |
| 10 | Workflows | 0 | 64 kB | 2,80% |
| 11 | CustomerAnalyticsSnapshots | 10 | 64 kB | 2,80% |
| 12 | BusinessMemoryRelationships | 40 | 64 kB | 2,80% |
| 13 | Leads | 0 | 64 kB | 2,80% |
| 14 | BusinessMemoryEvents | 30 | 64 kB | 2,80% |
| 15 | AiDecisionAudits | 12 | 64 kB | 2,80% |
| 16 | WorkflowTasks | 20 | 64 kB | 2,80% |
| 17–46 | *(resto ABOS/Memory/ML/ops)* | 0–40 | 24–48 kB | 1–2% c/u |

**Observación:** El **índice ocupa 60–80%** del tamaño en tablas vacías o casi vacías (overhead de diseño EF).

---

## Análisis por Tabla (CRM + ABOS)

### Leyenda columnas candidatas

- **TenantId** — filtro global en casi todas las tablas (índice presente).
- **CustomerId** — joins C360, churn, audits.
- **CreatedAt / OccurredAt** — ORDER BY frecuente sin índice compuesto en algunos casos.

---

### Módulo CRM Core

#### `Customers` (Clientes)
| Atributo | Detalle |
|----------|---------|
| Registros | ~50 |
| Tamaño | 96 kB (16 kB heap / 80 kB índices) |
| PK | `Id` (uuid) |
| FK DB | Ninguna |
| Índices | `PK_Customers`, `IX_Customers_TenantId`, `IX_Customers_TenantId_Email` |
| JSON | `Metadata` (jsonb) |
| Columnas críticas | `TenantId`, `Email`, `Status`, `RiskScore` |
| Candidatos índice | `IX (TenantId, Status)` si listados por estado; `IX (TenantId, Name)` para búsqueda |

#### `Leads`
| Atributo | Detalle |
|----------|---------|
| Registros | 0 |
| Tamaño | 64 kB |
| PK | `Id` |
| Índices | `PK_Leads`, `IX_Leads_TenantId`, `IX_Leads_TenantId_Status` |
| JSON | `Metadata` (jsonb) |

#### `Deals` (Oportunidades)
| Atributo | Detalle |
|----------|---------|
| Registros | ~45 |
| Tamaño | 112 kB |
| PK | `Id` |
| Índices | `PK_Deals`, `IX_Deals_TenantId`, `IX_Deals_TenantId_CustomerId`, `IX_Deals_TenantId_Status` |
| JSON | `Metadata` (jsonb) |
| Uso EF | Revenue OS, Executive, C360 — filtros por `TenantId`, `CustomerId`, `Status`, `Stage` |

#### `Users`
| Atributo | Detalle |
|----------|---------|
| Registros | ~5 |
| PK | `Id` |
| Índices | `PK_Users`, `IX_Users_TenantId`, `IX_Users_TenantId_Email` (unique) |
| JSON | `Roles`, `Claims` (jsonb) |
| Seq scans | 78 (stats dev — login repetido) |

#### `WorkflowTasks` (Tareas / CS tickets)
| Atributo | Detalle |
|----------|---------|
| Registros | ~20 |
| PK | `Id` |
| Índices | `PK_WorkflowTasks`, `IX_WorkflowTasks_TenantId` |
| Candidato | `IX (TenantId, RelatedEntityId, Status)` para Customer Success OS |

#### `Workflows` (Automatización / “campañas”)
| Registros | 0 | Índices redundantes potenciales: `IX_Workflows_TenantId` + `IX_Workflows_TenantId_IsActive` |

#### `Policies`
| Registros | 0 | Índices: TenantId, Name, IsActive |

---

### Facturación y contratos

#### `CustomerContracts`
| Registros | 8 | Índices: `TenantId`, `TenantId+CustomerId`, `TenantId+RenewalDate` |

#### `TenantBillingAccounts`
| Registros | 0 | Stripe fields (text), unique `TenantId` |

#### `SalesQuotas`
| Registros | 0 | Quotas por tenant/usuario/período |

---

### Auditoría, logs, eventos

#### `DomainEvents` (Event store)
| Atributo | Detalle |
|----------|---------|
| Registros | 0 |
| Tamaño | **160 kB** (mayor tabla por índices) |
| PK | `Id` |
| Índices | `TenantId`, `EventType`, `OccurredOn`, `CorrelationId`, `AggregateId` |
| JSON | `EventData` (jsonb) — **crecimiento explosivo en prod** |
| Riesgo | 5 índices + sin partición/retención |

#### `AiDecisionAudits` (Auditoría IA / Trust)
| Registros | ~12 |
| Índices | `TenantId`, `TenantId+CustomerId` |
| JSON | `Evidence` (jsonb) |
| Candidato | **`(TenantId, CustomerId, CreatedAt DESC)`** — ORDER BY en C360/Executive |

#### `AiApprovalRequests`
| Registros | ~8 |
| Join | `AuditId` → `AiDecisionAudits` (sin índice en `AuditId`) |

#### `FailedEventMessages` (DLQ)
| Registros | 0 | Índices: `TenantId`, `MessageId` (unique), `FailedAt` |

#### `CustomerCommunicationLogs` / `VoiceCallLogs` / `CdpStreamEvents`
| Riesgo prod | Alto volumen append-only; `Payload`/`Variables` jsonb |

---

### ABOS / Memory / Intelligence

#### `MemoryEmbeddings`
| Atributo | Detalle |
|----------|---------|
| Registros | ~30 |
| Seq scans | **30.477** (hot path desarrollo) |
| JSON | `EmbeddingVector` (jsonb) — **pesado** |
| Índices | `TenantId+SourceType+SourceId`, `TenantId+RelevanceScore` |
| Causa scans | `SemanticMemoryService.SearchAsync` carga hasta 100 filas/tenant y rankea en CPU |

#### `BusinessMemories` + subtablas (`Events`, `Facts`, `Outcomes`, …)
| Registros | 0–40 | Muchas tablas 24–64 kB con 0 filas — overhead migraciones |

#### `BusinessKnowledgeGraphEdges`
| Registros | ~40 |
| Query C360 | OR en `SourceId`/`TargetId` — difícil para btree simple |

---

### Integraciones y ML

| Tabla | Uso | Estado local |
|-------|-----|--------------|
| `TenantIntegrations` | OAuth tokens (text) | 0 filas |
| `MlFeatureSnapshots` / `MlModelVersions` / `MlPipelineRuns` | ML ops | 0 filas |
| `TimeSeriesMetrics` | Métricas | 0 filas |

---

### Resumen índices por tabla (muestra CRM)

| Tabla | Índices |
|-------|---------|
| Customers | 3 |
| Deals | 4 |
| AiDecisionAudits | 3 |
| DomainEvents | 6 |
| MemoryEmbeddings | 3 |
| WorkflowTasks | 2 |

---

## Tablas sin Primary Key

**Ninguna** — las 46 tablas tienen PK (uuid o `MigrationId`).

---

## Foreign Keys sin índice

**N/A a nivel PostgreSQL** — la aplicación **no crea FK constraints** en migraciones EF. La integridad es lógica (aplicación).  
**Riesgo:** joins y cascadas no optimizados por el planner; deletes huérfanos posibles.

**Joins lógicos sin índice dedicado detectados (recomendación):**

| Tabla | Columna join | Uso |
|-------|--------------|-----|
| AiApprovalRequests | `AuditId` | JOIN en `Customer360EnterpriseService` |
| Deals | `CustomerId` | Parcialmente cubierto por `IX_Deals_TenantId_CustomerId` |

---

## Sequential Scans destacados

| Tabla | seq_scan | idx_scan | rows | Nota |
|-------|----------|----------|------|------|
| **MemoryEmbeddings** | 30.477 | 1.761 | 30 | Búsqueda semántica full-tenant + dev OTel |
| Users | 78 | 0 | 5 | Login / seed |
| __EFMigrationsHistory | 27 | 0 | 9 | Startup |
| Tenants | 25 | 29 | 1 | Resolución tenant |

En producción con datos, monitorear `DomainEvents`, `CustomerCommunicationLogs`, `ProductUsageEvents`.

---

## Índices no utilizados (muestra — `idx_scan = 0`)

Tras reset de stats o tablas vacías, muchos índices muestran 0 uso (normal en dev):

- `IX_Workflows_TenantId`, `IX_Workflows_TenantId_IsActive`
- `IX_DomainEvents_*` (tabla vacía)
- `IX_Policies_*`, `IX_Leads_*`
- PK/IX en tablas sin datos

**No eliminar en prod** sin 7–14 días de `pg_stat_user_indexes` en carga real.

---

## Índices redundantes (candidatos)

| Tabla | Índices | Análisis |
|-------|---------|----------|
| Workflows | `IX_TenantId` + `IX_TenantId_IsActive` | Compuesto puede cubrir filtros solo-TenantId |
| Customers | `IX_TenantId` + `IX_TenantId_Email` | Email compuesto suele cubrir tenant-only |
| Policies | 3 índices tenant | Revisar solapamiento |

---

## Columnas JSONB / TEXT pesadas

| Tipo | Cantidad columnas | Tablas críticas |
|------|-------------------|-----------------|
| jsonb | 28+ | `Evidence`, `Metadata`, `EventData`, `EmbeddingVector`, `Settings`, `Workflows.Actions` |
| text | 90+ | Audit trails, tokens integración, MFA |

**Riesgo prod:** `DomainEvents.EventData`, `MemoryEmbeddings.EmbeddingVector`, `AiDecisionAudits.Evidence`.

---

## pg_stat_statements

**No disponible** — extensión no instalada.  
Para Fase 2 prod: `CREATE EXTENSION pg_stat_statements;` + `shared_preload_libraries`.

---

## Mapeo módulos CRM solicitados

| Módulo solicitado | Tabla(s) AutonomusFlow | Estado local |
|-------------------|------------------------|------------|
| Usuarios | `Users` | 5 |
| Clientes | `Customers`, `CustomerMemoryProfiles` | 50 / 35 |
| Leads | `Leads` | 0 |
| Oportunidades | `Deals` | 45 |
| Actividades / Tareas | `WorkflowTasks` | 20 |
| Seguimientos | `CustomerCommunicationLogs`, `VoiceCallLogs` | 0 |
| Campañas | `Workflows` (automation) | 0 |
| Productos | *(no tabla dedicada)* | — |
| Facturación | `TenantBillingAccounts`, `CustomerContracts` | 0 / 8 |
| Reportes | `CustomerAnalyticsSnapshots`, `TimeSeriesMetrics` | 10 / 0 |
| Auditoría | `AiDecisionAudits`, `DomainEvents`, `FailedEventMessages` | 12 / 0 / 0 |
| Logs | `CdpStreamEvents`, comms, voice | 0 |
| Notificaciones | Comms + workers (no tabla push dedicada) | — |

---

## Conclusión de inventario

| Fortaleza | Debilidad |
|-----------|-----------|
| PK en todas las tablas | 0 FK físicas |
| TenantId indexado masivamente | 126 índices — overhead en tablas vacías |
| jsonb para flexibilidad | Crecimiento y seq scan en embeddings |
| Migraciones EF ordenadas | Sin vistas/materialized views para reporting |
| Aislamiento tenant (query filters) | Sin partición en event store / logs |

*Generado — Fase 1 Auditoría PostgreSQL Enterprise.*
