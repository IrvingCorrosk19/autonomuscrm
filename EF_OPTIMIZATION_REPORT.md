# EF OPTIMIZATION REPORT — AutonomusCRM

**Fecha:** 2026-06-04  
**DbContext:** `ApplicationDbContext`  
**ORM:** Entity Framework Core 9 + Npgsql  
**Conexión:** `appsettings.Development.json` → PostgreSQL local

---

## Resumen ejecutivo

El acceso a datos es **mayormente explícito** (repositorios + servicios con `AsNoTracking`). **No hay lazy loading** configurado. El principal problema de percepción “loop” en localhost es **OpenTelemetry + EF instrumentation** imprimiendo cada SQL en consola, combinado con **múltiples consultas por página** (especialmente Customer 360 y memoria semántica).

---

## DbContext y tenancy

| Aspecto | Estado |
|---------|--------|
| Global query filters | ✅ `HasQueryFilter` en 40+ entidades (`TenantId`) |
| `ICurrentTenantAccessor` | ✅ Inyectado en `ApplicationDbContext` |
| Bypass filter | ✅ `BypassTenantFilter` para seed/admin |
| FK en modelo | ❌ No se generan constraints PostgreSQL |

---

## Patrones detectados

### AsNoTracking — ✅ Bien usado en lecturas

Servicios que leen dashboards sin mutación:

- `Customer360EnterpriseService` — todas las queries `AsNoTracking`
- `ExecutiveOsService`, `RevenueOsService`
- `DecisionIntelligenceEngine`, `AbosOutcomeLearningService`

### Include() — ⚠️ Uso limitado

Búsqueda en `AutonomusCRM.Infrastructure`: **pocos o ningún `.Include()`** en servicios principales.  
Estrategia actual: **múltiples queries pequeñas** en lugar de joins EF → más round-trips, menos cartesian explosion.

### Lazy Loading — ✅ Deshabilitado

No se observa `UseLazyLoadingProxies` en `Program.cs` / DI.

### ToList() prematuro — ⚠️ Varios casos

| Ubicación | Patrón | Impacto |
|-----------|--------|---------|
| `NextBestActionEngine.GetForTenantAsync` | Loop 30 customers × `DecideForCustomerAsync` | **N+1 lógico** (30+ queries/decisiones) |
| `Customer360EnterpriseService` | 8–10 queries secuenciales por vista | **Multi-query page** (no N+1 clásico, pero alto QPS) |
| `SemanticMemoryService.SearchAsync` | `GetByTenantAsync(100)` + rank CPU | 1 query amplia + CPU |
| `CustomerSuccessOsService` | Múltiples engines `.ToList()` en memoria | Aceptable si engines cachean |

---

## Consultas ineficientes (prioridad)

### P0 — Customer 360 Enterprise (`GetEnterpriseViewAsync`)

**Archivo:** `Customer360EnterpriseService.cs`

Por cada visita a `/customers/{id}/360`:

1. `_c360.GetAsync` (perfil agregado)
2. `Customers` por Id
3. `Deals` (take 20)
4. `AiDecisionAudits` (take 15)
5. `AutonomousPlaybookStates`
6. `AiApprovalRequests` JOIN audits
7. `CustomerCommunicationLogs` (take 25)
8. `VoiceCallLogs` (take 15)
9. `_churn.PredictAsync`
10. `_knowledgeGraph.GetCustomerGraphAsync`
11. `_csOs.GetCustomerPanelAsync`

**Más** en `Detail.cshtml.cs`:

12. `DecisionIntelligenceEngine.AnalyzeCustomerDecisionAsync`
13. `AbosOutcomeLearningService.GetCustomerLearningAsync`

**Total estimado:** **12–15 round-trips SQL** por carga de página.

**Recomendación (código — fuera de alcance SQL):** vista materializada o single stored procedure / Dapper read model; mínimo: paralelizar con `Task.WhenAll` donde no haya dependencia.

---

### P0 — Memoria semántica (`SemanticMemoryService.SearchAsync`)

**Archivo:** `SemanticMemoryService.cs` líneas 57–68

```csharp
var candidates = await _repo.GetByTenantAsync(tenantId, Math.Max(take * 5, 100), cancellationToken);
// RankAndMap en memoria — cosine sobre jsonb vectors
```

- Provoca **seq scan** masivo en `MemoryEmbeddings` (stats: 30k+).
- No usa pgvector ni índice ANN.
- Llamado desde: NBA, GraphReasoning, Customer360 explainability, ABOS learning.

**Recomendación prod:** extensión `vector` + índice IVFFlat/HNSW; o servicio embeddings externo.

---

### P1 — Next Best Action por tenant

**Archivo:** `NextBestActionEngine.cs`

```csharp
foreach (var c in customers.Take(30))
    await GetForCustomerAsync(...) // → DecideForCustomerAsync cada uno
```

Cadena: `AutonomousRevenueDecisionEngine` → `DecisionIntelligenceEngine` → múltiples engines.

**Impacto:** Executive dashboard y Flow Command bajo carga.

---

### P1 — Approval join sin índice `AuditId`

**Archivo:** `Customer360EnterpriseService.cs` líneas 73–78

```csharp
_db.AiApprovalRequests.AsNoTracking()
    .Join(_db.AiDecisionAudits.Where(a => a.CustomerId == customerId), ...)
```

Falta índice en `AiApprovalRequests.AuditId` (ver `OPTIMIZATION_SCRIPT.sql`).

---

### P2 — Repositorios sin proyección

**Archivo:** `CustomerRepository`, `DealRepository`, etc.

`GetByTenantIdAsync` → `ToListAsync()` materializa entidad completa incluyendo `Metadata` jsonb.

**Recomendación:** DTO projections `.Select()` para listados.

---

### P2 — Business Memory timeline

**Archivo:** `SemanticMemoryRepository.GetTimelineAsync`

4 queries secuenciales (observations, decisions, outcomes, learnings) + merge en memoria.

---

## OpenTelemetry / consola (“loop” percibido)

**Archivo:** `PlatformExtensions.cs`

```csharp
var enableConsole = configuration.GetValue("OpenTelemetry:EnableConsoleExporter", true);
o.SetDbStatementForText = true; // EF instrumentation
```

En **Development**, cada query EF se imprime en consola → apariencia de loop infinito al navegar C360.

**Mitigación sin cambiar negocio:**

```json
"OpenTelemetry": {
  "EnableConsoleExporter": false
}
```

en `appsettings.Development.json` (o variable de entorno).

---

## Includes excesivos

**No detectados** en rutas críticas — el problema es **falta de agregación**, no over-eager loading.

---

## Materializaciones innecesarias

| Caso | Archivo | Nota |
|------|---------|------|
| `GetByTenantIdAsync().ToList()` | Repositories | OK para <1000 filas |
| Churn sobre todos los clientes | Varios engines | Revisar límites en prod |

---

## EF Migrations vs BD

- **15 migraciones** en código; **9 aplicadas** en instancia local auditada.
- Modelo alineado con índices declarados en `ApplicationDbContext.OnModelCreating`.

---

## Recomendaciones EF (sin implementar)

| # | Acción | Tipo | Riesgo |
|---|--------|------|--------|
| 1 | Desactivar OTel console exporter en dev | Config | Ninguno |
| 2 | `Task.WhenAll` en C360 queries independientes | Código | Bajo |
| 3 | Read model / vista SQL para C360 | Arquitectura | Medio |
| 4 | pgvector para `MemoryEmbeddings` | Infra + EF | Medio |
| 5 | Batch NBA: una query customers at-risk | Código | Medio |
| 6 | Proyecciones `.Select` en listados | Código | Bajo |
| 7 | Habilitar `pg_stat_statements` | DBA | Ninguno |

---

## Conclusión EF

| Área | Score interno |
|------|---------------|
| Tracking discipline | 82 |
| Query shaping | 58 |
| N+1 avoidance | 52 |
| Observability noise | 40 (dev) |
| Tenancy safety | 88 |

*Generado — Fase 4 Entity Framework Analysis.*
