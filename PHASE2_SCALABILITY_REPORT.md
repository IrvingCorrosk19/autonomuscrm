# PHASE2_SCALABILITY_REPORT — AutonomusCRM

**Fecha:** 2026-06-12  
**Objetivo:** Preparación enterprise multi-tenant a escala (simulación 1M+ filas)

---

## 1. Modelo de crecimiento por tabla

| Tabla | Crecimiento esperado | Cuello de botella histórico | Mitigación Fase 2 |
|-------|---------------------|----------------------------|-------------------|
| **Deals** | Alto (pipeline) | Forecast + leaderboard cargaban todo | Agregados SQL + `IX_Deals_TenantId_AssignedToUserId` |
| **Leads** | Alto (marketing) | Smart assignment full scan | Load GROUP BY user + trigram search |
| **Customers** | Medio | Journey + expansion metadata | COUNT jsonb + proyecciones |
| **WorkflowTasks** | Alto (automation) | Health engine all tasks | GROUP BY customer (direct + deal join) |
| **DomainEvents** | Muy alto (append-only) | Tabla más grande en prod típico | Particionamiento RANGE (plan) |
| **Users** | Bajo | Loop N quotas | Batch quotas + summary projection |

---

## 2. Simulación 1M registros — análisis teórico

### 2.1 Deals (1 000 000 filas / tenant grande)

**Operación:** `GetRepPerformanceAggregatesAsync`

```sql
SELECT "AssignedToUserId", SUM(...), COUNT(*) ...
FROM "Deals"
WHERE "TenantId" = @t AND "AssignedToUserId" IS NOT NULL
GROUP BY "AssignedToUserId"
```

| Aspecto | Estimación |
|---------|------------|
| Plan esperado | Index Scan `IX_Deals_TenantId_AssignedToUserId` + HashAggregate |
| Filas leídas | ~1M (index-only parcial si covering) |
| Filas en app | ~reps activos (50–500) |
| RAM app | <1 MB |
| Tiempo objetivo | 200–800 ms (SSD, shared_buffers adecuado) |

**Operación:** `GetForecastHorizonsAsync` (4 horizons)

- 4 queries con filter `Status = Open` + rango `ExpectedCloseDate`
- Alternativa Fase 3: single query con 4 conditional aggregates (reduce a 1 RTT)

### 2.2 Leads (1 000 000 filas)

**Búsqueda:** `ILike '%acme%'` sin índice → ~2–5 s  
**Con GIN trigram** → objetivo <200 ms (depende selectividad)

**Assignment load:** GROUP BY `AssignedToUserId` → O(leads) scan, O(users) output

### 2.3 Customers (100 000 filas)

**Journey metadata counts:** 4× `COUNT` con `Metadata ? 'key'`  
- Índice GIN en jsonb **no** creado (overhead en writes)  
- A escala: considerar `GENERATED STORED` flags o columnas normalizadas `OnboardingStartedAt`

### 2.4 DomainEvents (5 000 000+ filas)

**Estado actual:** heap + `IX_DomainEvents_TenantId_OccurredOn`

**Plan de particionamiento recomendado:**

```sql
-- Ejemplo conceptual (requiere ventana de mantenimiento)
CREATE TABLE "DomainEvents_partitioned" (
    LIKE "DomainEvents" INCLUDING ALL
) PARTITION BY RANGE ("OccurredOn");

CREATE TABLE "DomainEvents_2026_06"
    PARTITION OF "DomainEvents_partitioned"
    FOR VALUES FROM ('2026-06-01') TO ('2026-07-01');
```

| Beneficio | Detalle |
|-----------|---------|
| Pruning | Queries con `OccurredOn >= @since` leen 1–3 particiones |
| Retention | `DROP PARTITION` mensual vs `DELETE` masivo |
| Vacuum | Por partición, menor bloat global |

**Trigger de implementación:** >500k eventos/tenant o SLA auditoría <500ms degradado.

---

## 3. Límites de conexión y round-trips

| Endpoint lógico | Round-trips post-Fase 2 | Techo recomendado |
|-----------------|-------------------------|-------------------|
| Revenue forecast | 5 | OK hasta 10 |
| Sales leaderboard | 4 | OK |
| Customer journey | 4 | OK |
| Smart assign | 3 | OK |
| ML revenue forecast | 7 | Considerar cache 60s |
| Expansion detect | 3 + health batch | Cache health 5 min |

**Connection pooling:** Npgsql pool default; para workers paralelos usar `MaxPoolSize` ≥ 100 en prod.

---

## 4. Memoria CLR — presupuesto por request

| Request | Heap máximo estimado (1M deals tenant) |
|---------|----------------------------------------|
| Antes Fase 2 | 200–500 MB (serialización Deal entities) |
| Después Fase 2 | <5 MB (DTOs + dictionaries) |

Evita presión Gen2 GC en API pods con requests concurrentes.

---

## 5. Horizontal scaling

| Componente | Escalabilidad | Notas |
|------------|---------------|-------|
| API (read analytics) | Stateless × N replicas | Sin sticky sessions |
| PostgreSQL | Vertical + read replica | Agregados en primary; réplica para reporting |
| Redis cache (futuro) | Leaderboard / forecast TTL | Invalidar en `DealClosed` event |
| Workers | Por tenant shard | Colas RabbitMQ ya resilientes |

---

## 6. Checklist operativo pre-prod

- [ ] `ANALYZE` post-migración Fase 2
- [ ] `EXPLAIN ANALYZE` en staging con seed ≥100k deals (`ops/database/phase2-scalability-explain.sql`)
- [ ] Monitorear `pg_stat_user_indexes` para uso trigram
- [ ] Alerta si `DomainEvents` > 100k filas/tenant
- [ ] Plan de particionamiento documentado en runbook DBA

---

## 7. Resumen

Fase 2 elimina el patrón anti-escala **“cargar tenant entero → LINQ en memoria”** en los 6 engines críticos y su dependencia `CustomerHealthEngine`. La base de datos puede crecer a **millones de filas por tenant** sin crecimiento lineal de memoria en la aplicación. El siguiente límite real será **I/O PostgreSQL** en agregados full-tenant — mitigable con índices compuestos, particiones en eventos, y cache de lectura en dashboards.
