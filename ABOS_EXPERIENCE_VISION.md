# ABOS EXPERIENCE VISION — AutonomusFlow

**Versión:** 1.0 · **Estado:** Visión de producto (sin implementación)  
**Pregunta central:** ¿Cómo se ve el primer Autonomous Business Operating System del mundo?

---

## 1. Definición de categoría (qué es ABOS en UI)

Un **ABOS** no se navega como CRM. Se **supervisa** como sala de control:

```
Detectar → Decidir → Actuar → Medir $ → Aprender → (loop)
```

La UI actual invierte el peso: 70% pantallas CRUD (Leads, Deals, Customers) y 30% ABOS (Trust, Command) con estética AdminLTE (auditoría). La visión invierte **peso y estética**:

| Hoy | ABOS |
|-----|------|
| Home = Dashboard CRM KPIs | Home = **Flow Command** |
| IA = página más del menú | IA = **estado ambiental** del producto |
| Trust = cola de cards | Trust = **sala de decisiones** |
| Customer360 = cards texto | Customer360 = **grafo + timeline + $** |
| Revenue = footer small-box | Revenue = **Revenue OS** dedicado |
| Agentes = stats fake | Workforce = **panel de trabajadores autónomos** |

**Backend ya soporta** (v0.9 MASTER_CONTEXT): Outcome Fabric, 6 agentes, Command Center service, Trust policy, merge identity, CDP stream. La visión **expone** capacidades existentes con narrativa visual correcta.

---

## 2. Arquitectura de información (IA global)

### 2.1 Navegación primaria (sidebar)

```
FLOW (logo)
─────────────────
⌘K Buscar...

COMMAND          ← default landing post-login
Trust            (badge count)
Workforce
─────────────────
REVENUE
  Pipeline
  Forecast
  Win/Loss
─────────────────
CUSTOMERS
  Directory
  Customer 360
  Success
─────────────────
COMMERCE
  Leads
  Deals
─────────────────
PLATFORM
  Integrations
  Voice
  Comms
─────────────────
ADMIN
  Users
  Policies
  Audit
  Settings
  Billing
```

**Cambios vs hoy:** «Autonomus CRM» → **AutonomusFlow**; PRINCIPAL/AUTONOMÍA fusionados en narrativa; **Billing** visible; Revenue y CS explícitos; Dashboard `/` redirige a Command.

### 2.2 Rutas canónicas

| Vista visión | Ruta | Reemplaza |
|--------------|------|-----------|
| Flow Command | `/` | Index + AiCommandCenter |
| Trust Studio | `/trust` | TrustInbox |
| Workforce | `/workforce` | Agents |
| Revenue OS | `/revenue` | (disperso) |
| Customer 360 | `/customers/{id}/360` | Customer360 list |
| Integrations Hub | `/integrations` | Integrations |
| Billing | `/billing` | (no existe) |

**Eliminar de producción:** `/Dashboard` mock (auditoría: datos ficticios).

---

## 3. Home — Flow Command (pantalla más importante)

### 3.1 Layout (desktop 1440px)

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Flow Command                    [Modo: Supervisado ▾]  [⌘K]  [Avatar]   │
├──────────────────────────────────────────────────────────────────────────┤
│ HERO STRIP (full width)                                                    │
│  AutonomusFlow protegió $284,000 este mes · 12 decisiones pendientes     │
│  [Revisar ahora → Trust]                                                   │
├───────────────────────────────┬──────────────────────────────────────────┤
│ LEFT 60%                      │ RIGHT 40%                                 │
│ ┌─ Revenue at risk ─────────┐ │ ┌─ Autonomous Workforce ────────────────┐ │
│ │ sparkline + 3 accounts    │ │ │ 6 agent cards (live status)           │ │
│ └───────────────────────────┘ │ │ Sales ● Renewal ● Churn ● ...         │ │
│ ┌─ Expansion targets ───────┐ │ └───────────────────────────────────────┘ │
│ └───────────────────────────┘ │ ┌─ Live decision feed ──────────────────┐ │
│ ┌─ Renewals 90d ────────────┐ │ │ DECISION · hace 2m · Conf 0.82       │ │
│ └───────────────────────────┘ │ │ ALERT · churn risk Acme              │ │
│                               │ └───────────────────────────────────────┘ │
├───────────────────────────────┴──────────────────────────────────────────┤
│ BOTTOM: Pipeline snapshot (kanban mini) · NBA top 5 · Outcome fabric %     │
└──────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Principios UX Command

- **Un número hero** — revenue impact IA período (dato real `RevenueGeneratedByAi7d` + contexto).
- **Un CTA primario** — «Revisar decisiones» si pending > 0.
- **Cero small-box** de 6 colores.
- **Feed** estilo Linear issues, no list-group Bootstrap.
- **Workforce** muestra agentes reales (nombres del código: Sales, Renewal, Churn, Expansion, Customer, Operations) con estado ● activo / ○ idle / ⚠ error.

### 3.3 Comparación referentes

| Referente | Qué hacen | Cómo superamos |
|-----------|-----------|----------------|
| Salesforce | Einstein en registro | Command **central** cross-tenant narrative |
| HubSpot | Dashboard marketing | Outcome $ como hero, no visits |
| Linear | Issue feed limpio | Mismo feed pattern + **$ impact** por ítem |
| Stripe | Métricas claras | Misma claridad + **acción autónoma** ligada |

---

## 4. AI Command Center → fusionado en Flow Command

**Decisión de visión:** No mantener Command Center como página secundaria con small-box (auditoría 46 UI). Es **el home**. Sub-rutas:

- `/command/decisions` — historial filtros
- `/command/outcomes` — Outcome Fabric incompleto (dato real v0.9)
- `/command/playbooks` — estados playbook

**Narrativa CEO:** «Esta es la consola donde la empresa se opera sola.»

---

## 5. Trust — Trust Studio

### 5.1 Metáfora

No «inbox de email». Es **sala de control de riesgo** tipo trading compliance + Stripe Radar.

### 5.2 Layout decisión (3 columnas)

```
┌─────────────────────────────────────────────────────────────────┐
│ Trust Studio · 8 pendientes · SLA: 2 críticos                    │
├──────────────┬────────────────────────────┬─────────────────────┤
│ QUEUE 25%    │ DECISION DETAIL 50%        │ CONTEXT 25%         │
│ filtros      │ Tipo: PreventChurn       │ Cliente: Acme Corp  │
│ lista cards  │ Score: 82 · Riesgo: Alto │ Deal: $45k renew    │
│ SLA badges   │ Explicación humana       │ Evidence timeline   │
│              │ Impacto: +$45k / -$12k   │ Policy threshold 70 │
│              │ [Simular] [Aprobar] [✗]  │ Similar past outcomes│
└──────────────┴────────────────────────────┴─────────────────────┘
```

### 5.3 Capacidades backend a surfear (v0.9)

- `ITenantTrustPolicyService` — slider umbral en panel lateral, no form inline
- `ITrustSlaService` — cola ordenada por severidad
- `IOutcomeFabricService` — preview outcome si aprueba
- Explainability desde `Evidence` dict — JSON colapsado, humanizado arriba

### 5.4 vs competencia

| vs | Ventaja Flow |
|----|--------------|
| Salesforce Einstein | HITL explícito + rollback visible |
| HubSpot | Menos marketing, más **governance** |
| Stripe Radar | Misma seriedad + **acciones CRM** ejecutables |

---

## 6. Customer 360 — Data Cloud Experience

### 6.1 Visión

**Attio meets Palantir lite** — una entidad, todas las señales, un veredicto.

### 6.2 Layout

```
┌─ Header: Acme Corp · Health 72 · Churn 34% · LTV $120k ─────────────┐
├─ Tabs: Overview | Timeline | Revenue | Product | AI | Integrations ─┤
├─ Main ────────────────────────┬─ Graph sidebar ─────────────────────┤
│ Timeline eventos (CDP stream) │ Relaciones: deals, contacts, usage  │
│ NBA actual + últimas 3 IA     │ Duplicate merge CTA si aplica       │
│ Comms log                   │                                     │
└───────────────────────────────┴─────────────────────────────────────┘
```

### 6.3 Datos reales disponibles (no inventar)

- `ICustomer360Service` — API existente
- `IIdentityResolutionService` + merge — duplicados con CTA
- `ICdpEventStreamService` — timeline
- Churn risk ML — cuando ≥25 muestras (MASTER_CONTEXT)

### 6.4 Gap a cerrar en UI

Hoy: grid cards texto (auditoría 45 UI). Visión: **timeline + health ring + sparkline LTV**.

---

## 7. Revenue OS

### 7.1 Por qué existe como módulo

Auditoría: Revenue UI 45 — «métricas dispersas». ABOS promete **autonomous revenue**; debe tener **hogar visual**.

### 7.2 Pantallas

| Pantalla | Contenido |
|----------|-----------|
| **Overview** | Pipeline ponderado, forecast, win rate, coverage — charts |
| **At Risk** | Deals estancados + IA recommendation |
| **Outcomes** | Win/loss atribuido a IA (Outcome Fabric) |
| **Quotas** | `SalesQuota` entity — tabla + progreso |

### 7.3 Hero metrics

- Pipeline abierto / ponderado (Index ya calcula — surfear)
- Revenue closed IA-attributed
- Win rate trend 90d

---

## 8. Autonomous Workforce

### 8.1 Reemplaza Agents.cshtml

**Problema actual:** KPIs inventados 1,247 / 7/7 (auditoría — destruye ABOS).

### 8.2 Visión Workforce Panel

```
┌─ Workforce ─────────────────────────────────────────────────────┐
│ 6 agentes · 3 activos ahora · $12.4k impacto 24h                │
├─────────────────────────────────────────────────────────────────┤
│ [Sales Agent]      ████████░░ 8 acciones · $4.2k · ● Running   │
│ [Renewal Agent]    ████░░░░░░ 3 acciones · $2.1k · ○ Idle      │
│ ...                                                             │
├─ Click agent → drawer: últimas decisiones, playbook states ─────┤
└─────────────────────────────────────────────────────────────────┘
```

**Fuente datos:** `AiDecisionAudits` por `AgentName`, `AutonomousPlaybookStates`, métricas Command Center v0.9 — **solo datos reales**.

### 8.3 Narrativa

«No son bots en config — son **empleados digitales** con workload y revenue.»

---

## 9. Módulos satélite (visión resumida)

### 9.1 CRM (Leads, Deals, Customers)

- Mantener funcionalidad; **degradar prominencia** en nav.
- Estética Flow: `FlowPageHeader`, tablas `FlowDataTable`, drawers detalle (no navegación full page pesada).
- Pipeline kanban unificado Index/Deals.

### 9.2 Integrations Hub

- Cards con **logo + status + last sync + health**.
- OAuth primario; manual en accordion «Avanzado (solo admins)».
- Conflictos: badge + panel (sync conflict service v0.9).

### 9.3 Voice

- Zero GUID fields — pickers búsqueda entidad.
- Call row → drawer transcript status + AI summary cuando exista.
- Integración Twilio: estado «Connected» visible.

### 9.4 Billing (nuevo)

- Stripe-grade: plan actual, uso vs límites (`PlanLimitService`), facturas, upgrade CTA.
- Percepción $5k: **monetización visible = producto real**.

### 9.5 Settings & Admin

- Settings: form sections claras, sin search fake.
- Audit: mantener datos reales; estética Flow.
- Login enterprise: SSO buttons, sin tenant ID, sin demo table prod.

---

## 10. Customer Success (módulo visión)

No existe hoy (auditoría 40). Visión:

- `/success` — health distribution, churn predicted, renewals 90d, playbooks activos
- Conecta `CustomerHealthEngine`, `ChurnRiskEngine`, `RenewalEngine` (MASTER_CONTEXT Fase 13)

---

## 11. Estados emocionales por persona

| Persona | Entrada | Debe sentir |
|---------|---------|-------------|
| **CEO** | Flow Command | «Mi empresa se opera sola; yo apruebo lo crítico.» |
| **CRO** | Revenue OS | «Pipeline y forecast bajo control; IA empuja deals.» |
| **VP CS** | Success + 360 | «Veo churn antes; playbooks corriendo.» |
| **Ops** | Trust + Workforce | «Puedo auditar y revertir.» |
| **IT** | Integrations + Settings | «OAuth, SCIM, sin pegar tokens en post-its.» |

---

## 12. Evolución de marca en experiencia

| Fase | Nombre UI | Categoría comunicada |
|------|-----------|---------------------|
| **Ahora** | Autonomus CRM | CRM + extras |
| **Fase 1 rebuild** | AutonomusFlow | Enterprise AI Platform |
| **Fase 2** | AutonomusFlow ABOS | Autonomous Business OS |
| **Fase 3** | Flow (short) | Categoría propia como «Stripe» |

---

## 13. Lo que hace único al primer ABOS (moat visual)

1. **Outcome Fabric visible** — cadena decision→$ en UI (backend v0.9 ya la tiene).
2. **Trust Studio** — HITL como producto, no feature checkbox.
3. **Workforce con revenue** — agentes con P&L, no chatbots.
4. **Command-first** — categoría nueva vs CRM dashboard.
5. **Truthful enterprise** — cero mock en pantallas estratégicas.

---

## 14. Anti-patrones ABOS (nunca más)

- AdminLTE small-box rainbow
- Dashboard mock `/Dashboard`
- Passwords demo en login prod
- «MVP» en copy usuario
- Tokens OAuth en pantalla principal
- Footer «Event-Driven · Multi-tenant»
- Dos familias header (content-header vs topbar)
- KPIs sin fuente datos visible

---

## 15. North Star screenshot (descripción para Figma)

Una sola imagen para pitch deck:

**Flow Command, light mode, 1440px.** Hero: «$284k protected this month.» Tres columnas riesgo/expansión/renewal con nombres reales. Derecha: 6 agentes con barras actividad. Abajo: feed decisiones con badges confianza. Sidebar minimal indigo accent. Cero azul Bootstrap. Tipografía Inter. Espacio blanco generoso. Badge «12 pending» en Trust.

Esa imagen **es** la categoría ABOS.

---

*Visión alineada con capacidades backend documentadas v0.9. Implementación secuenciada en UI_REBUILD_MASTERPLAN.md.*
