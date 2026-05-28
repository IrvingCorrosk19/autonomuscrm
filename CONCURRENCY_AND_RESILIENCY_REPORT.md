# CONCURRENCY_AND_RESILIENCY_REPORT

**Fecha:** 2026-05-27

---

## Implementación

| Entidad | Mecanismo | Archivo |
|---------|-----------|---------|
| Deal | `Version` int + `IsConcurrencyToken()` EF | `Deal.cs`, `ApplicationDbContext.cs` |
| Update stage | `ExpectedVersion` opcional en command | `UpdateDealStageCommand.cs` |

Migración: `Phase3_DealVersion_WorkflowTasks`

---

## Comportamiento esperado

1. Usuario A carga deal `Version=3`.
2. Usuario B actualiza deal → `Version=4`.
3. Usuario A envía update con `ExpectedVersion=3` → handler retorna `false` (conflicto lógico pre-save).

EF además lanza `DbUpdateConcurrencyException` si dos saves concurrentes omiten el check.

---

## Pruebas

| Caso | Estado |
|------|--------|
| CONC-DEAL automatizado API | **SKIP** — endpoint REST stage no expuesto aún |
| Regresión P0/P3 | PASS (sin regresión) |

---

## Resiliencia

| Área | Estado |
|------|--------|
| PostgreSQL retry startup | Migrate en `InitializeDatabaseAsync` |
| RabbitMQ reconnect | No implementado (conexión única en ctor) — **mejora futura** |
| Event bus fallback | InMemory si no hay RabbitMQ config |

---

## Recomendaciones

1. Exponer `PUT /api/deals/{id}/stage?tenantId=` con body `{ stage, expectedVersion }`.
2. Propagar `Version` en formulario Deals/Edit (hidden field).
3. Extender patrón a Lead/Customer si edición concurrente es frecuente.
