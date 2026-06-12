# DATABASE_OPTIMIZATION_PLAN — Acciones realizadas

**Proyecto:** AutonomusCRM / AutonomusFlow  
**Fecha:** 2026-06-12

---

## Fase 0 — Respaldo (obligatorio)

- [x] `pg_dump` formato custom → `DatabaseBackups/20260612_044741/`
- [x] Export esquema + sección índices
- [x] Snapshot `pg_stat_user_tables` → `audit_stats.txt`

---

## Fase 1 — PostgreSQL

| Acción | Estado | Detalle |
|--------|--------|---------|
| Crear índices compuestos rutas calientes | ✅ | Migración `20260612094959_QueryPathCompositeIndexes` |
| `VACUUM ANALYZE` global | ✅ | Post-migración |
| `REINDEX` | ⏭️ | No necesario en dev |
| Índices parciales | ⏭️ | Evaluar en prod (`Status = 'Open'`) |
| Ajuste parámetros PG | ⏭️ | Fuera de alcance app (DBA prod) |

### Migración aplicada

```
IX_Leads_TenantId_CreatedAt
IX_Leads_TenantId_AssignedToUserId
IX_Deals_TenantId_CreatedAt
IX_WorkflowTasks_TenantId_Status_CreatedAt
```

---

## Fase 2 — Entity Framework Core

| Acción | Archivo | Cambio |
|--------|---------|--------|
| Agregación SQL Revenue KPI | `RevenueKpiService.cs` | Elimina carga completa deals/leads |
| `GetRevenueKpiAggregatesAsync` | `DealRepository.cs` | SUM/COUNT en DB |
| `GetConversionStatsAsync` | `LeadRepository.cs` | Un round-trip GROUP BY |
| Summary leads 1 query | `LeadRepository.GetListSummaryAsync` | 4 COUNT → 1 agregado |
| Forecast deals en DB | `DealRepository.GetListSummaryAsync` | Sin `ToList` de open deals |
| Summary users 1 query | `UserRepository.GetListSummaryAsync` | 4 COUNT → 1 agregado |
| `CountActiveByTenantAsync` | `UserRepository.cs` | COUNT filtrado |

### Interfaces extendidas (sin cambio funcional UI)

- `IDealRepository` → `DealRevenueKpiAggregates`
- `ILeadRepository` → `LeadConversionStats`
- `IUserRepository` → `CountActiveByTenantAsync`

---

## Fase 3 — No modificado (por restricción)

- Lógica de negocio / reglas RBAC
- UI / UX
- Eliminación tablas o datos
- Engines analíticos secundarios (backlog Fase 4)

---

## Fase 4 — Backlog recomendado (prod)

1. Proyecciones SQL en `RevenueForecastEngine`, `SalesPerformanceEngine`
2. `CompiledQuery` para búsquedas ILike frecuentes
3. `AsSplitQuery` solo si aparecen cartesian explosions (no detectadas)
4. Índice GIN trigram en `Leads.Name` / `Customers.Name` si búsqueda textual >100ms
5. Particionado `DomainEvents` por `OccurredOn` (>10M filas)
6. Persistir progreso University en DB (fuera de scope DB perf)

---

## Verificación

| Check | Resultado |
|-------|-----------|
| `dotnet build` | ✅ 0 errores |
| Tests unitarios core (194) | ✅ PASS |
| `dotnet ef database update` | ✅ Migración aplicada |

---

*Plan ejecutado — métricas en `DATABASE_PERFORMANCE_REPORT.md`.*
