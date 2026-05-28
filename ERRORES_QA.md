# ERRORES_QA — Defectos Fase 2

---

## DEF-001

- **Título:** Viewer accede a `/Leads/Create` (GET)
- **Severidad:** Alta
- **Prioridad:** P0
- **Riesgo asociado:** R15 — escalación privilegios comercial
- **Caso QA:** SEC-V-01
- **Archivo:** `AutonomusCRM.API/Middleware/CommercialWriteAuthorizationMiddleware.cs`
- **Línea:** 23-36 (solo POST)
- **Root cause:** Middleware no bloqueaba GET a páginas Create/Edit para Viewer/Support.
- **Reproducibilidad:** 100%
- **Impacto negocio:** Usuario solo lectura podía abrir formulario de alta.
- **Fix aplicado:** Bloqueo GET en rutas `/Create` y `/Edit` bajo prefijos comerciales.
- **Estado:** CLOSED (retest PASS)

---

## DEF-002

- **Título:** API permite `tenantId` distinto al del JWT en query
- **Severidad:** Crítica
- **Prioridad:** P0
- **Riesgo asociado:** B15 / TEN-004
- **Caso QA:** TEN-004
- **Archivo:** `AutonomusCRM.API/Controllers/LeadsController.cs` (sin validación claim)
- **Línea:** 44-50
- **Root cause:** Handlers aceptaban `tenantId` de query sin comparar con claim.
- **Reproducibilidad:** 100%
- **Impacto negocio:** Potencial lectura cross-tenant vía API.
- **Fix aplicado:** `ApiTenantValidationMiddleware.cs` — 403 si query/body `tenantId` ≠ claim.
- **Estado:** CLOSED (retest PASS HTTP 403)

---

## DEF-003

- **Título:** Auditoría UI con datos ficticios
- **Severidad:** Media
- **Prioridad:** P0 (trazabilidad)
- **Riesgo asociado:** B06 / R-AUD
- **Caso QA:** TRZ-001
- **Archivo:** `AutonomusCRM.API/Pages/Audit.cshtml`
- **Línea:** 39-74, 137-148
- **Root cause:** KPIs hardcodeados y fila demo `CustomerRiskUpdated`.
- **Reproducibilidad:** 100%
- **Impacto negocio:** Decisiones operativas sobre métricas falsas.
- **Fix aplicado:** Stats desde `IEventStore`; eliminada fila demo; distribución por tipo real.
- **Estado:** CLOSED

---

## DEF-004

- **Título:** EventStore no deserializaba eventos
- **Severidad:** Alta
- **Prioridad:** P0
- **Riesgo asociado:** B06
- **Caso QA:** TRZ-001, E2E-AUD-*
- **Archivo:** `AutonomusCRM.Infrastructure/Persistence/EventStore/EventStore.cs`
- **Línea:** 101-117
- **Root cause:** `DeserializeEvents` vacío (TODO).
- **Reproducibilidad:** 100%
- **Impacto negocio:** Pantalla auditoría sin eventos reales.
- **Fix aplicado:** `DomainEventTypeRegistry` + `PersistedDomainEvent` fallback; `CountByTenantAsync`.
- **Estado:** CLOSED (eventos visibles; tipado fuerte pendiente — ver DEF-006)

---

## DEF-005

- **Título:** SameTenantHandler no comparaba tenant solicitado
- **Severidad:** Alta
- **Prioridad:** P1
- **Riesgo asociado:** B15
- **Caso QA:** TEN-003
- **Archivo:** `AutonomusCRM.Application/Authorization/Handlers/SameTenantHandler.cs`
- **Línea:** 19-24
- **Root cause:** Solo verificaba existencia de claim.
- **Reproducibilidad:** N/A en UI; API cubierta por DEF-002.
- **Fix aplicado:** Comparación `tenantId` query/route vs claim; middleware API.
- **Estado:** CLOSED (defensa en profundidad)

---

## DEF-006 (OPEN — no bloqueante piloto 1 tenant)

- **Título:** Deserialización tipada de eventos de dominio falla (envelope)
- **Severidad:** Baja
- **Prioridad:** P2
- **Riesgo asociado:** B06
- **Caso QA:** TRZ-001 (funcional OK con envelope)
- **Archivo:** `DomainEventTypeRegistry.cs` / eventos sin ctor parameterless
- **Root cause:** `System.Text.Json` no rehidrata constructores con parámetros.
- **Impacto:** Auditoría muestra tipo correcto vía `PersistedDomainEvent.EventType`; detalle JSON en envelope.
- **Fix recomendado:** `[JsonConstructor]` en eventos o DTO de proyección dedicado.
- **Estado:** OPEN

---

## DEF-007 (OPEN — conocido pre-Fase 2)

- **Título:** `GET /api/leads/{id}` stub sin tenant check
- **Severidad:** Media
- **Prioridad:** P2
- **Caso QA:** API-003 catálogo
- **Archivo:** `LeadsController.cs` L53-57
- **Estado:** OPEN — no bloquea piloto UI-first
