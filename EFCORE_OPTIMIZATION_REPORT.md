# EFCORE_OPTIMIZATION_REPORT — AutonomusCRM

**Fecha:** 2026-06-12  
**Stack:** EF Core 9 + Npgsql + PostgreSQL 18

---

## 1. Configuración EF (existente — validada)

| Setting | Ubicación | Valor |
|---------|-----------|-------|
| DbContext | `AddDbContextFactory<ApplicationDbContext>` | Scoped via factory |
| Provider | `UsePlatformNpgsql` | Retry 5×, timeout 30s |
| Global filters | `ApplicationDbContext` | Tenant isolation |
| Dynamic JSON | `NpgsqlConnection.GlobalTypeMapper` | Program.cs |
| Tracking lecturas | `Repository<T>.GetAll/Find` | `AsNoTracking` ✅ |

---

## 2. N+1 eliminados / reducidos

| # | Antes | Después | Impacto |
|---|-------|---------|---------|
| 1 | RevenueKpi: 2× full tenant load | Agregados SQL | **Crítico** |
| 2 | Lead summary: 4 COUNT | 1 GROUP BY | Alto |
| 3 | User summary: 4 COUNT | 1 GROUP BY | Medio |
| 4 | Deal forecast: ToList open deals | 3 SumAsync | Alto |

---

## 3. Includes — auditoría

| Área | Include usage | Veredicto |
|------|---------------|-----------|
| Leads/Deals/Customers list | Sin Include — proyección directa | OK |
| Customer Success OS | `AsNoTracking` + `Take(50)` | OK |
| Revenue OS Service | Queries directas `_db.Deals` | OK |
| User search con roles | `Roles.Any()` en filtro — no Include | OK |

**No se detectaron** cadenas Include profundas en rutas HTTP principales.

---

## 4. ToList prematuros — pendientes (backlog)

Servicios que aún cargan tenant completo (no modificados — fuera hot path UI inmediato):

| Servicio | Método | Recomendación |
|----------|--------|---------------|
| `RevenueForecastEngine` | `GetByTenantIdAsync().ToList()` | Agregar `GetForecastInputsAsync` SQL |
| `SalesPerformanceEngine` | deals + users full | Proyección por usuario |
| `CustomerJourneyEngine` | leads+deals+customers | Pipeline batch job |
| `SmartAssignmentEngine` | users+leads+deals | Filtrar unassigned en SQL |
| `RevenuePredictionModelService` | deals full | ML snapshot table |
| `ExpansionRevenueEngine` | deals full | Filter open + expansion flag |

---

## 5. Optimizaciones aplicadas — detalle código

### 5.1 `DealRepository.GetRevenueKpiAggregatesAsync`

```csharp
// COUNT/SUM en servidor — sin materializar Deal entities completas
wonCount, lostCount, revenueClosed, lostRevenue, openWeighted
// Ciclo venta: solo { CreatedAt, ClosedAt } para ClosedWon
```

### 5.2 `LeadRepository.GetConversionStatsAsync`

```csharp
// Un GROUP BY → Total, Qualified, ConversionPercent
```

### 5.3 `LeadRepository.GetListSummaryAsync`

```csharp
// GroupBy(1) → Total, Qualified, Newly, HighScore, AvgScore en una query
```

### 5.4 `DealRepository.GetListSummaryAsync`

```csharp
// IQueryable open deals → SumAsync por horizonte 30/60/90 en DB
```

---

## 6. Técnicas NO aplicadas (evaluadas)

| Técnica | Razón |
|---------|-------|
| Compiled Queries | Beneficio marginal vs mantenimiento; revisar si perf regresión |
| Split Queries | Sin Includes multi-collection detectados |
| Disable tracking global | Escrituras requieren tracking en updates |
| Raw SQL / Dapper | No necesario tras agregados LINQ traducibles |

---

## 7. Paginación — estado

| Página | Implementación | Estado |
|--------|----------------|--------|
| Leads | `SearchPagedAsync` + `RepositoryPaging` | ✅ |
| Deals | `SearchPagedAsync` | ✅ |
| Users | `SearchPagedAsync` | ✅ |
| Tasks / CS | `Take(50)` + filtros | ✅ |
| Audit | Paginado en página Razor | Verificar en carga alta |

---

## 8. Pruebas

| Suite | Resultado |
|-------|-----------|
| Unit tests (194, sin Integration/E2E) | **PASS** |
| Build solución | **PASS** |
| Migración EF | **Aplicada** |

---

## 9. Archivos modificados

- `AutonomusCRM.Application/Common/Interfaces/IDealRepository.cs`
- `AutonomusCRM.Application/Common/Interfaces/ILeadRepository.cs`
- `AutonomusCRM.Application/Common/Interfaces/IUserRepository.cs`
- `AutonomusCRM.Infrastructure/Persistence/Repositories/DealRepository.cs`
- `AutonomusCRM.Infrastructure/Persistence/Repositories/LeadRepository.cs`
- `AutonomusCRM.Infrastructure/Persistence/Repositories/UserRepository.cs`
- `AutonomusCRM.Infrastructure/Revenue/RevenueKpiService.cs`
- `AutonomusCRM.Infrastructure/Persistence/ApplicationDbContext.cs`
- `AutonomusCRM.Infrastructure/Persistence/Migrations/20260612094959_QueryPathCompositeIndexes.cs`

---

*EF Core hot paths optimizados — ver `FINAL_DATABASE_STATUS.md` para estado general.*
