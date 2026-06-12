# PHASE2_PERFORMANCE_REPORT — AutonomusCRM

**Fecha:** 2026-06-12  
**Alcance:** Engines analytics enterprise + índices Fase 2

---

## 1. Metodología

Comparación **patrón anterior vs patrón optimizado** por request lógico (un tenant). Métricas:

- Filas materializadas en CLR
- Round-trips SQL estimados
- Complejidad algorítmica en memoria

Datos de volumen dev: ~5 deals, ~16 leads, ~5 customers, ~33 users. Proyecciones a escala basadas en complejidad O(n) vs O(1)/O(users).

---

## 2. Por engine — antes / después

### RevenueForecastEngine

| Métrica | Antes | Después |
|---------|-------|---------|
| Filas cargadas | `COUNT(deals)` | 0 filas completas |
| Round-trips | 1 (full scan) | 2 (`win rate` + 4 buckets horizon) |
| Memoria | O(deals) | O(horizons) = 4 |

### SalesPerformanceEngine

| Métrica | Antes | Después |
|---------|-------|---------|
| Filas cargadas | deals + users | users (Id, Email) + filas GROUP BY rep |
| Round-trips | 1 + 1 + **N quotas** | 4 fijos |
| Memoria | O(deals × users) loop | O(users + reps con deals) |

**Mejora crítica:** eliminación de N+1 en `GetActiveForUserAsync` por rep.

### CustomerJourneyEngine

| Métrica | Antes | Después |
|---------|-------|---------|
| Filas cargadas | leads + deals + customers + health | 0 entidades completas |
| Round-trips | 4+ (incl. health full) | 4 (`count`, customer counts, deal metrics, avg health) |
| Memoria | O(leads+deals+customers+health) | O(1) métricas escalares + health promedio |

### SmartAssignmentEngine

| Métrica | Antes | Después |
|---------|-------|---------|
| Filas cargadas | users + leads + deals | users (summary) + 2 diccionarios load |
| Round-trips | 3 full scans | 3 agregados |
| Memoria | O(users + leads + deals) | O(users) |

### RevenuePredictionModelService

| Métrica | Antes | Después |
|---------|-------|---------|
| Filas cargadas | deals (duplicado con forecast) | 0 deals |
| Round-trips | 1 + forecast | 2 + forecast |
| Features ML | `Sum(open)` en memoria | `GetOpenPipelineAmountSumAsync` |

### ExpansionRevenueEngine

| Métrica | Antes | Después |
|---------|-------|---------|
| Filas cargadas | health + customers + deals | health DTOs + proyecciones + dict won |
| Round-trips | 3 full scans | 3 acotados |
| Join won por customer | LINQ en memoria | `GROUP BY CustomerId` en SQL |

### CustomerHealthEngine (soporte)

| Métrica | Antes | Después |
|---------|-------|---------|
| `CalculateAllAsync` | 3× tenant completo | proyecciones + 2 GROUP BY |
| `CalculateHealthAsync` | tenant deals/tasks | 2 scoped aggregates |
| Filas por customer | O(deals + tasks) | O(1) agregados |

---

## 3. Proyección a 1M filas simuladas (por tabla)

| Tabla | Escenario | Antes (memoria) | Después (memoria) | SQL |
|-------|-----------|-----------------|-------------------|-----|
| Deals 1M | Forecast | ~1M objetos Deal | ~4 decimales | Index `TenantId, Status` + filter open |
| Deals 1M | Leaderboard | ~1M + loop users | ~users filas agregadas | `IX_Deals_TenantId_AssignedToUserId` |
| Leads 1M | Assignment load | ~1M | ~users ints | `IX_Leads_TenantId_AssignedToUserId` |
| Customers 100k | Journey metadata | ~100k dict scan | 4× COUNT SQL | jsonb `?` + status index |
| DomainEvents 5M | Audit por mes | full scan risk | partition prune | RANGE `OccurredOn` (futuro) |

**Regla:** Ningún engine objetivo materializa O(tenant_rows) en heap CLR post-Fase 2.

---

## 4. Búsqueda ILike — impacto esperado

Consultas en `LeadRepository`, `CustomerRepository`, `DealRepository`, `UserRepository`:

```sql
WHERE "Name" ILIKE '%term%' OR "Email" ILIKE '%term%' ...
```

| Sin trigram | Con GIN trigram |
|-------------|-----------------|
| Seq scan O(n) | Bitmap Index Scan O(log n) típico |
| CPU alto en n>100k | Latencia estable <100ms objetivo |

Validar en prod con `EXPLAIN (ANALYZE)` y `pg_trgm.similarity_threshold` si se adopta `%term%` corto.

---

## 5. EF Core — patrones aplicados

- `AsNoTracking()` en todas las lecturas analytics
- `Select` → DTOs (`ActiveUserSummary`, `CustomerHealthProjection`, etc.)
- `GroupBy` + `Sum`/`Count`/`Average` traducidos a SQL
- `AverageAsync` en ciclo de ventas (reemplaza `ToList` + LINQ)
- Metadata jsonb: operador PostgreSQL `?` vía `SqlQueryRaw` (`PostgresJsonbQuery`)

---

## 6. Tests y compilación

```
dotnet build                          → OK
dotnet test (210 unit tests)          → PASS
dotnet ef database update             → Phase2 migration applied
```

Scripts: `ops/database/phase2-scalability-explain.sql`, `ops/database/audit-postgres.sql`

---

## 7. Próximos hot paths (Fase 3 sugerida)

1. `SalesProductivityService` — mismo patrón load-by-user
2. `CustomerKpiService` — consolidar con `GetJourneyCustomerCountsAsync`
3. `WinLossAnalyticsService` — GROUP BY `LossReason` en SQL
4. Materialized view opcional: `mv_tenant_deal_kpis` refresh cada 15 min para dashboards
