# DATABASE OPTIMIZATION PLAN — AutonomusFlow Production

**Fecha:** 2026-06-04  
**Alcance:** PostgreSQL + EF Core — **solo plan, sin ejecución**  
**BD auditada:** `autonomuscrm` @ localhost (11 MB, dev seed)

---

## Impacto Alto

### H1 — Habilitar observabilidad SQL real (pg_stat_statements)

| Campo | Detalle |
|-------|---------|
| Problema | No hay visibilidad de top queries en prod |
| Causa | Extensión no instalada |
| Solución | `shared_preload_libraries = 'pg_stat_statements'` + `CREATE EXTENSION` |
| Riesgo | Bajo — reinicio PostgreSQL |
| Impacto | Identificar top 20 queries CPU/IO antes de más índices |

---

### H2 — Índice compuesto auditoría IA (`AiDecisionAudits`)

| Campo | Detalle |
|-------|---------|
| Problema | `ORDER BY CreatedAt DESC` + filtro `TenantId, CustomerId` en C360/Trust |
| Causa | Solo existe `IX_TenantId_CustomerId` sin columna temporal |
| Solución | `CREATE INDEX ... ON "AiDecisionAudits" ("TenantId", "CustomerId", "CreatedAt" DESC)` |
| Riesgo | Bajo — índice adicional ~MB en prod |
| Impacto | 30–60% menos latencia en timeline Trust/C360 |

---

### H3 — Índice `AiApprovalRequests.AuditId`

| Campo | Detalle |
|-------|---------|
| Problema | JOIN approval ↔ audit sin índice en FK lógica |
| Causa | EF no creó FK ni índice en `AuditId` |
| Solución | `CREATE INDEX ON "AiApprovalRequests" ("AuditId")` |
| Riesgo | Bajo |
| Impacto | Join C360 approvals instantáneo |

---

### H4 — Memoria semántica: pgvector o servicio externo

| Campo | Detalle |
|-------|---------|
| Problema | 30k+ seq scans; carga 100 embeddings/tenant en RAM |
| Causa | Vectores en jsonb + similitud en aplicación |
| Solución | Columna `vector(1536)` + índice HNSW; o Pinecone/pgvector |
| Riesgo | Medio — migración datos + cambio EF |
| Impacto | **Crítico** para escala ABOS/memory |

---

### H5 — Retención y partición `DomainEvents`

| Campo | Detalle |
|-------|---------|
| Problema | Tabla append-only con 6 índices — crecimiento explosivo |
| Causa | Event sourcing sin archival |
| Solución | Partición mensual por `OccurredOn` + job archival >90d |
| Riesgo | Medio — operación DBA |
| Impacto | Evita degradación IO en 6–12 meses prod |

---

### H6 — Reducir ruido OTel en desarrollo

| Campo | Detalle |
|-------|---------|
| Problema | Consola inundada con SQL → “loop” percibido |
| Causa | `EnableConsoleExporter: true` + `SetDbStatementForText` |
| Solución | `OpenTelemetry:EnableConsoleExporter: false` en Development |
| Riesgo | Ninguno |
| Impacto | DX inmediato |

---

## Impacto Medio

### M1 — Consolidar queries Customer 360 (EF)

| Problema | 12–15 queries/página |
| Solución | Paralelización + read model SQL |
| Riesgo | Medio (código) |
| Impacto | 40% menos latencia p95 página |

---

### M2 — Índice `WorkflowTasks` para Customer Success

| Problema | Filtros `TenantId + RelatedEntityId + Status` |
| Solución | Índice compuesto (ver script) |
| Impacto | Panel CS más rápido |

---

### M3 — Revisar índices redundantes post-carga real

| Problema | 126 índices, muchos idx_scan=0 en dev |
| Solución | Tras 14d prod, `DROP INDEX` solo índices con 0 uso y duplicados |
| Riesgo | Medio si se elimina índice usado en reportes batch |

---

### M4 — FK físicas opcionales (integridad)

| Problema | 0 FK en PostgreSQL |
| Solución | Añadir FK en tablas core: Deals→Customers, Users→Tenants |
| Riesgo | Alto si datos huérfanos existen — validar antes |
| Impacto | Integridad + planner mejor en joins |

---

### M5 — GIN en jsonb consultados

| Tablas | `Customers.Metadata`, `AiDecisionAudits.Evidence` |
| Solución | `CREATE INDEX ... USING gin ("Metadata" jsonb_path_ops)` solo si hay queries JSON |
| Riesgo | Índice grande |

---

### M6 — NBA engine: batch en lugar de loop 30×

| Problema | N+1 decisiones por cliente |
| Solución | Precalcular churn/at-risk en una query |
| Impacto | Executive/Flow Command |

---

## Impacto Bajo

### L1 — ANALYZE programado

| Solución | `cron` diario `ANALYZE` en tablas hot |
| Impacto | Estadísticas frescas |

---

### L2 — `autovacuum` tuning tablas append-only

| Tablas | DomainEvents, CommunicationLogs, CdpStreamEvents |
| Impacto | Menos bloat |

---

### L3 — Comprimir índices huérfanos en tablas vacías (dev)

| Nota | En prod con datos, no aplicar hasta medición |

---

### L4 — Documentar contraseñas / connection pooling

| Solución | PgBouncer transaction mode + pool size = `(cores*2)+effective_spindle` |

---

### L5 — Índice `BusinessKnowledgeGraphEdges` para grafo cliente

| Problema | OR en source/target |
| Solución | Dos índices: `(TenantId, SourceType, SourceId)`, `(TenantId, TargetType, TargetId)` — **ya parcialmente cubierto**; validar plan con `EXPLAIN` |

---

## Matriz CRM → prioridad

| Módulo | Riesgo crecimiento | Prioridad plan |
|--------|-------------------|----------------|
| DomainEvents | 🔴 Alto | H5 |
| MemoryEmbeddings | 🔴 Alto | H4 |
| AiDecisionAudits | 🟡 Medio | H2, H3 |
| CustomerCommunicationLogs | 🟡 Medio | H5, L2 |
| Customers/Deals | 🟢 Bajo | M3, L1 |
| Leads (vacío) | 🟢 | — |

---

## Orden de ejecución recomendado (prod)

1. H1 pg_stat_statements  
2. H6 OTel dev  
3. H2 + H3 índices (script SQL — ventana baja carga)  
4. L1 ANALYZE  
5. Medir 7 días  
6. H4 pgvector (proyecto)  
7. H5 partición DomainEvents  
8. M1/M6 cambios EF  

---

## Criterios de éxito post-optimización

| KPI | Objetivo |
|-----|----------|
| p95 Customer360 | < 800 ms |
| seq_scan MemoryEmbeddings | < 5% del total scans |
| Tamaño DomainEvents | Crecimiento acotado con retención |
| Índices no usados | < 10% del total tras 30d |

*Generado — Fase 5 Plan de Optimización Enterprise.*
