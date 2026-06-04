# ABOS Action Engine — Phase 1 Report

**Fecha:** 2026-05-28  
**Alcance:** Convertir insights en acciones ejecutables sin nuevos motores, agentes ni arquitecturas.

---

## Resumen

Phase 1 conecta cada insight visible con **Tasks**, **Deals**, **Communications** y **Trust** mediante un partial reutilizable y handlers POST en la página existente `FlowActions`.

---

## 1. Qué acciones se agregaron

### Cliente en riesgo (`risk`)

| Acción | Integración |
|--------|-------------|
| Crear tarea | `IOperationalTaskService` → tipo `Retention` |
| Enviar email | `ICommunicationDeliveryService` (formulario inline) |
| Programar llamada | Tarea tipo `Call` |
| Crear oportunidad de retención | `/Deals/Create` pre-rellenado |
| Solicitar aprobación | `IAiTrustService` (cola existente) o tarea `TrustApproval` |

### Cliente listo para expansión (`expansion`)

| Acción | Integración |
|--------|-------------|
| Crear oportunidad | `/Deals/Create` pre-rellenado |
| Generar propuesta | Tarea tipo `Proposal` |
| Programar reunión | Tarea tipo `Meeting` |
| Enviar email | Comms inline |

### Renovación próxima (`renewal`)

| Acción | Integración |
|--------|-------------|
| Crear tarea | Tipo `Renewal` |
| Crear renovación | Deal pre-rellenado |
| Contactar cliente | Email inline |
| Solicitar aprobación | Trust Studio |

### Revenue en riesgo (`revenue_at_risk`)

| Acción | Integración |
|--------|-------------|
| Ver clientes afectados | `/Customer360` |
| Crear plan | Tarea tenant-level `RecoveryPlan` |
| Asignar responsable | `/Tasks` |

---

## 2. Qué pantallas fueron modificadas

| Pantalla | Cambio |
|----------|--------|
| **Flow Command** (`Index.cshtml`) | CTAs en riesgo, expansión y renovaciones |
| **Executive** (`Executive.cshtml`) | CTAs en NBA + acciones agregadas de revenue en riesgo |
| **Revenue OS** (`Revenue.cshtml`) | CTAs por insight + barra en métrica “En riesgo” |
| **Customer 360 Directory** (`Customer360.cshtml`) | Acciones compactas por tarjeta |
| **Customer 360 Enterprise** (`Detail.cshtml`) | Barra de acciones según health/journey |
| **Trust Studio** (`TrustInbox.cshtml`) | Acciones de cliente junto a aprobar/rechazar |
| **Deals/Create** | Query params `customerId`, `title`, `amount` |
| **_FlowPageHeader** | Flash messages de acción |

### Archivos nuevos (wiring, no arquitectura)

- `Pages/Shared/Flow/_FlowInsightActions.cshtml` — partial de acciones
- `Pages/FlowActions.cshtml(.cs)` — POST handlers
- `Infrastructure/FlowInsightTypes.cs` — mapeo insight → tipo

### Ajuste de datos

- `RevenueInsightDto` — campo opcional `CustomerId` para enlazar acciones

---

## 3. Qué flujo se volvió ejecutable

| Flujo | Antes | Después |
|-------|-------|---------|
| Insight riesgo → acción | Solo lectura | Tarea / email / llamada / deal retención / Trust |
| Insight expansión → acción | Solo lectura | Deal / propuesta / reunión / email |
| Insight renovación → acción | Solo lectura | Tarea / deal renovación / contacto / Trust |
| Revenue en riesgo → acción | Solo métrica | Plan + navegación a clientes + Tasks |
| NBA Executive → acción | Lista pasiva | 5 CTAs por recomendación |
| Trust + cliente → acción | Solo aprobar/rechazar | + acciones operativas sobre el cliente |

**Problema resuelto:** *“Veo el insight pero no sé qué hacer.”* → cada fila de insight incluye botones con efecto inmediato o enlace pre-rellenado.

---

## 4. Qué sigue sin acción

| Área | Motivo |
|------|--------|
| **Insights sin `CustomerId`** (deals NBA) | Solo enlaces genéricos; no hay deal-level action bar aún |
| **Generar propuesta PDF** | No existe generador de documentos; se crea tarea manual |
| **Asignar responsable inline** | Redirige a `/Tasks`; no hay picker inline |
| **WhatsApp / llamada VoIP** | Comms soporta API; UI solo email inline |
| **Aprobación Trust desde cero** | Requiere `auditId` existente; sin audit → tarea de escalamiento |
| **Memory / Graph insights** | Fuera de alcance Phase 1 |
| **Leads / legacy CRM pages** | Sin partial de acciones |
| **Acciones autónomas post-aprobación** | Sigue el flujo Trust existente (no duplicado) |

---

## 5. Impacto esperado en UX

| Dimensión | Impacto estimado |
|-----------|------------------|
| **Accionabilidad** | +15–20 pts humano (insight → CTA visible) |
| **Time-to-action** | De ~5 clics/navegación manual → 1 clic |
| **Confianza ejecutiva** | CEO ve NBA con botones, no solo texto |
| **CS / Sales** | Customer360 deja de ser “dashboard pasivo” |
| **Revenue OS** | Métrica en riesgo con plan de respuesta |
| **Trust Studio** | Puente operativo cliente ↔ aprobación |

**Riesgo UX:** densidad de botones en listas compactas — mitigado con variante `--compact` y forms email en `<details>`.

---

## Verificación

```bash
dotnet build   # 0 errors
```

**Prueba manual sugerida (CEO_DEMO):**

1. `/` → cliente en riesgo → **Crear tarea** → verificar en `/Tasks`
2. `/executive` → NBA → **Programar reunión**
3. `/revenue` → insight churn → **Enviar email**
4. `/customers/{id}/360` → barra según health
5. `/TrustInbox` → item con cliente → acciones secundarias

---

## Principio rector

> No se creó un “Action Engine” nuevo. Se cablearon servicios existentes (`Tasks`, `Deals`, `Comms`, `Trust`) a la capa de presentación Flow.
