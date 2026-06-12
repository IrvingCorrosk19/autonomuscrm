# DATABASE_PERFORMANCE_REPORT — ANTES vs DESPUÉS

**Fecha:** 2026-06-12  
**Entorno medición:** PostgreSQL 18 local, DB `autonomuscrm` (datos dev)

---

## 1. Metodología

| Aspecto | Descripción |
|---------|-------------|
| Herramienta SQL | `EXPLAIN (ANALYZE, BUFFERS)` |
| Herramienta app | Análisis estático + refactor repositorios |
| Umbral objetivo | <100 ms local, <300 ms prod estimada |
| Limitación | Tablas dev muy pequeñas — mejoras de **escala** documentadas |

---

## 2. Revenue KPI Dashboard (`RevenueKpiService`)

| Métrica | ANTES | DESPUÉS | Mejora |
|---------|-------|---------|--------|
| Filas materializadas (deals) | Todas del tenant | 0 (solo agregados) | ~100% menos I/O |
| Filas materializadas (leads) | Todas del tenant | 0 (GROUP BY) | ~100% menos I/O |
| Round-trips DB estimados | 3 + forecast + coverage | 5 agregados paralelos | Menos payload |
| Memoria heap (estimada 10k deals) | ~2–5 MB/deals + leads | <10 KB DTO | **>99%** |
| Tiempo local (16 leads, 5 deals) | <5 ms | <3 ms | Marginal en dev |

**Proyección producción (50k deals / tenant):**

| | ANTES | DESPUÉS |
|---|-------|---------|
| Tiempo estimado | 800–2000 ms | 15–80 ms |
| Lecturas buffer | Full table | Index + aggregate |

---

## 3. Lead list summary (`GetListSummaryAsync`)

| Métrica | ANTES | DESPUÉS |
|---------|-------|---------|
| Queries COUNT | 4 secuenciales | 1 agregado |
| Round-trips | 4 | 1 |
| Mejora latencia red | — | **~75%** menos viajes |

---

## 4. Deal forecast summary (`GetListSummaryAsync`)

| Métrica | ANTES | DESPUÉS |
|---------|-------|---------|
| Open deals cargados | Todos a memoria | 3× `SumAsync` en DB |
| Memoria (1k open deals) | ~500 KB | O(1) |
| CPU cliente | LINQ Where/Sum | PostgreSQL aggregate |

---

## 5. User list summary

| Métrica | ANTES | DESPUÉS |
|---------|-------|---------|
| COUNT queries | 4 | 1 |
| Mejora | — | **75%** menos round-trips |

---

## 6. PostgreSQL — consultas representativas

### Leads — listado paginado (EXPLAIN post-índice)

```
Execution Time: 0.072 ms (dev, 11 filas filtradas)
Plan: Seq Scan → Sort (tabla pequeña)
```

Con **>10k filas** y índice `IX_Leads_TenantId_CreatedAt`:

- Plan esperado: `Index Scan Backward` + `Limit`
- Objetivo prod: <20 ms p95

### Leads — agregado conversión

```
Execution Time: 0.045 ms (dev)
```

---

## 7. CPU y concurrencia

| Componente | Estado |
|------------|--------|
| Npgsql connection pool | DataSource singleton ✅ |
| Retry on failure | 5× / 15s max ✅ |
| Command timeout | 30s ✅ |
| Tracking en lecturas | `AsNoTracking` en repos ✅ |

---

## 8. Lecturas de disco (estimación escala)

Escenario: **1 tenant, 100k leads, 50k deals**

| Operación | Lecturas ANTES | Lecturas DESPUÉS |
|-----------|----------------|------------------|
| Revenue KPI snapshot | ~150k páginas | ~10–50 páginas |
| Lead summary (filtro) | 4 × scan | 1 × scan/index |
| Pipeline page 1 (20 rows) | Paginado OK | Sin cambio (ya optimizado) |

---

## 9. Resumen porcentual

| Área | Mejora estimada (escala) |
|------|--------------------------|
| Revenue KPI memoria | **>99%** |
| Revenue KPI I/O | **>95%** |
| Summary endpoints round-trips | **75%** |
| Índices rutas calientes | Preparado para **10–50×** datos |
| Tiempo local medido | Plano (datos mínimos) |

**Mejora global ponderada hot paths:** **~85–95%** en escenarios enterprise (proyección).

---

*Mediciones raw: `DatabaseBackups/20260612_044741/explain_after.txt`*
