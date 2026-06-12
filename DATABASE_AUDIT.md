# DATABASE_AUDIT — AutonomusCRM / AutonomusFlow

**Fecha:** 2026-06-12  
**Motor:** PostgreSQL 18 (local)  
**Base de datos:** `autonomuscrm` @ `localhost:5432`  
**Cadena detectada en:** `AutonomusCRM.API/appsettings.Development.json`

---

## 1. Resumen ejecutivo

| Área | Estado | Nota |
|------|--------|------|
| PostgreSQL local | Saludable | Datos dev pequeños (~2 MB total) |
| Índices existentes | Bueno | Migraciones Phase18 + DatabasePerformanceIndexes previas |
| Índices faltantes | Corregido | Migración `QueryPathCompositeIndexes` aplicada |
| EF Core patrones | Mejorable → Mejorado | Hot path Revenue KPI optimizado |
| N+1 críticos UI | Bajo riesgo | Repositorios paginados en listas |
| Carga masiva en memoria | Riesgo medio | Varios engines analytics aún cargan tenant completo |

---

## 2. Respaldo realizado

| Artefacto | Ubicación |
|-----------|-----------|
| Backup completo (custom) | `DatabaseBackups/20260612_044741/autonomuscrm_full.backup` |
| Esquema SQL | `DatabaseBackups/20260612_044741/schema.sql` |
| Índices (pre-data) | `DatabaseBackups/20260612_044741/indexes.sql` |
| Auditoría pg_stat | `DatabaseBackups/20260612_044741/audit_stats.txt` |
| EXPLAIN post-optimización | `DatabaseBackups/20260612_044741/explain_after.txt` |

---

## 3. Tablas por tamaño (local)

| Tabla | Tamaño | Filas vivas | Dead tuples | Último ANALYZE |
|-------|--------|-------------|-------------|----------------|
| DomainEvents | 184 kB | 21 | 12 | 2026-06-03 |
| Deals | 112 kB | 5 | 5 | 2026-06-03 |
| WorkflowTasks | 112 kB | 14 | 5 | 2026-06-03 |
| Users | 96 kB | 33 | 33 | auto 2026-06-06 |
| Leads | 80 kB | 16 | 10 | 2026-06-03 |
| Customers | 80 kB | 5 | 10 | 2026-06-03 |

**Observación:** En dev el planner usa seq scan (tablas <100 filas). Los índices nuevos benefician crecimiento a miles/millones de filas.

---

## 4. Índices — hallazgos

### 4.1 Ya existían (migraciones previas)

- `IX_Deals_TenantId_Status_Stage`
- `IX_Customers_TenantId_Status`
- `IX_WorkflowTasks_TenantId_RelatedEntityId_Status`
- `IX_AiDecisionAudits_TenantId_CustomerId_CreatedAt`
- `IX_DomainEvents_TenantId_OccurredOn`
- Cobertura fuerte en BusinessMemory, Customer360 paths

### 4.2 Añadidos en esta optimización

| Índice | Tabla | Motivo |
|--------|-------|--------|
| `IX_Leads_TenantId_CreatedAt` | Leads | Listados `ORDER BY CreatedAt DESC` |
| `IX_Leads_TenantId_AssignedToUserId` | Leads | Asignación / territorio |
| `IX_Deals_TenantId_CreatedAt` | Deals | Pipeline reciente |
| `IX_WorkflowTasks_TenantId_Status_CreatedAt` | WorkflowTasks | CS tickets / tareas por estado |

### 4.3 FK sin índice dedicado

Auditoría `audit-postgres.sql`: sin FK huérfanas críticas en esquema actual (EF gesta índices en columnas tenant).

### 4.4 Índices potencialmente redundantes (monitorear en prod)

- `IX_Leads_TenantId` subsumido parcialmente por compuestos `(TenantId, Status)`, `(TenantId, CreatedAt)` — **no eliminados** (sin datos de uso en prod).

---

## 5. Entity Framework — hallazgos

| Patrón | Ubicación | Severidad |
|--------|-----------|-----------|
| `GetByTenantIdAsync().ToList()` carga tenant entero | RevenueForecastEngine, SalesPerformanceEngine, CustomerJourneyEngine, SmartAssignmentEngine, etc. | Media |
| **Corregido:** Revenue KPI carga deals+leads completos | `RevenueKpiService` | Alta → Resuelto |
| **Corregido:** 4× COUNT en Lead summary | `LeadRepository.GetListSummaryAsync` | Media → Resuelto |
| **Corregido:** Forecast en memoria (open deals) | `DealRepository.GetListSummaryAsync` | Media → Resuelto |
| **Corregido:** 4× COUNT Users summary | `UserRepository.GetListSummaryAsync` | Baja → Resuelto |
| `AsNoTracking` en repositorios base | `Repository<T>`, Lead/Deal repos | OK |
| Paginación en listas UI | `SearchPagedAsync` Leads/Deals/Users | OK |
| Includes explícitos | Bajo uso — mayoría proyecciones | OK |

---

## 6. Consultas críticas — EXPLAIN (local)

Leads por tenant + orden:

- **Execution Time:** ~0.07–0.13 ms (16 filas totales en tabla)
- Plan: Seq Scan (esperado en tablas minúsculas)

En producción con >10k filas/tenant se espera **Index Scan** en `IX_Leads_TenantId_CreatedAt`.

---

## 7. Mantenimiento PostgreSQL

| Acción | Ejecutado |
|--------|-----------|
| `VACUUM ANALYZE` | Sí (post-migración) |
| `REINDEX` | No requerido (sin bloat significativo) |

Dead tuples en Users (33) — normal tras seeds; autovacuum activo.

---

## 8. Riesgos pendientes

1. **Engines analíticos** que aún materializan colecciones completas por tenant — aceptable en MVP, revisar antes de >50k deals.
2. **Integration tests** requieren `INTEGRATION_TEST_CONNECTION_STRING` o Docker Testcontainers.
3. **Connection pooling:** NpgsqlDataSource singleton en `DependencyInjection.cs` — correcto para concurrencia.

---

## 9. Métricas de referencia

| Métrica | Valor local |
|---------|-------------|
| Tablas | 46 |
| Migraciones EF | 18+ |
| Índices nuevos | 4 |
| Tamaño DB | ~2 MB |

---

*Auditoría generada como parte del programa de optimización AutonomusCRM — ver `DATABASE_OPTIMIZATION_PLAN.md`.*
