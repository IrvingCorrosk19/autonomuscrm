# COMPONENT LIBRARY — AutonomusFlow Design System

**Versión:** 1.0 · **Estado:** Especificación (sin implementación Razor)  
**Tokens:** Ver [WORLD_CLASS_DESIGN_SYSTEM.md](WORLD_CLASS_DESIGN_SYSTEM.md)  
**Convención partials:** `Pages/Shared/Flow/{ComponentName}.cshtml`

---

## 1. Layouts

### 1.1 `FlowAppShell`

**Reemplaza:** `_Layout.cshtml` + AdminLTE `wrapper`

| Zona | Spec |
|------|------|
| Structure | Sidebar + main + optional right drawer slot |
| Min height | 100vh |
| Background | `--flow-bg-canvas` |
| Max content | 1440px centered (prop `ExecutiveMode`) o fluid |

**Slots:** `Sidebar`, `TopBar`, `Banner`, `Body`, `Drawer`

**Responsive:** Sidebar → overlay drawer <1024px; swipe close.

**Anti-pattern eliminado:** `body.hold-transition.sidebar-mini` AdminLTE.

---

### 1.2 `FlowAuthLayout`

**Reemplaza:** Login layout null + `login-page`

| Elemento | Spec |
|----------|------|
| Split | 50% brand panel / 50% form (desktop) |
| Brand panel | Gradiente indigo→slate; testimonial o métrica ABOS |
| Form | Card 400px; SSO buttons arriba |
| Mobile | Stack; brand compacto |

**Prod:** Sin tabla demo. **Dev:** Collapse «Demo accounts».

---

### 1.3 `FlowSplitView`

**Uso:** Trust Studio, Customer 360

| Variant | Columns |
|---------|---------|
| `25-50-25` | Queue / Detail / Context |
| `30-70` | List / Detail |
| `60-40` | Main / Sidebar |

Collapsible panels con drag handle (fase 5).

---

## 2. Sidebars

### 2.1 `FlowSidebar`

| Prop | Valor |
|------|-------|
| Width expanded | 256px |
| Width collapsed | 72px |
| BG | `#0F172A` (slate 900) |
| Active item | `--flow-brand-primary-subtle` on dark = indigo tint border-left 3px |
| Sections | Label 11px uppercase muted |

**Items:** icon 20px Lucide + label + optional badge count (Trust pending).

**Footer:** Workspace switcher (multi-tenant futuro); Help; Collapse toggle.

**Reemplaza:** `main-sidebar sidebar-dark-primary`

---

### 2.2 `FlowSidebarItem`

States: `default`, `hover`, `active`, `disabled`

Badge: `FlowBadge variant="warning"` count.

---

## 3. Headers

### 3.1 `FlowPageHeader`

**Reemplaza:** `_PageHeader`, `content-header`, `topbar`

```
[Kicker 11px]
[Title 24px semibold]
[Subtitle 14px muted]                    [ActionGroup]
```

| Regla | Detalle |
|-------|---------|
| Max actions | 3 visibles + «Más» dropdown |
| Primary | 1 solo `FlowButton variant="primary"` |
| Breadcrumb | Opcional encima kicker |

---

### 3.2 `FlowTopBar`

Sticky bajo shell; altura 56px.

| Zona | Contenido |
|------|-----------|
| Left | Breadcrumb o context title mobile |
| Center | `FlowGlobalSearch` trigger ⌘K |
| Right | Comms pill, Notifications, Avatar menu |

**Elimina:** navbar AdminLTE `main-header`.

---

### 3.3 `FlowCommandBar` (sub-header)

Para páginas operativas: filtros activos + density toggle + export.

Altura 48px; `--flow-bg-subtle`.

---

## 4. Cards

### 4.1 `FlowCard`

| Prop | Default |
|------|---------|
| Padding | 20px |
| Radius | `--flow-radius-lg` |
| Shadow | `--flow-shadow-sm` |
| Border | 1px `--flow-border-default` |

**Slots:** `Header`, `Body`, `Footer`

**Variants:** `elevated`, `outline`, `ghost`

---

### 4.2 `FlowMetricCard`

**Reemplaza:** AdminLTE `small-box`, `.stat` gradient

```
┌─border-left 3px semantic──┐
│ LABEL 12px uppercase       │
│ VALUE 28px semibold        │
│ HINT 13px + delta chip     │
│ optional sparkline 64×24   │
└────────────────────────────┘
```

| Variant | Border color |
|---------|--------------|
| `neutral` | `--flow-border-strong` |
| `success` | `--flow-success` |
| `warning` | `--flow-warning` |
| `danger` | `--flow-danger` |
| `ai` | `--flow-brand-accent` |

**Regla:** Máximo 4 por fila desktop; 2 mobile.

---

### 4.3 `FlowDecisionCard`

**Uso:** Trust queue, Command feed

| Elemento | Spec |
|----------|------|
| Header | Type chip + time ago |
| Body | 2 líneas max; truncate |
| Footer | Confidence pill + impact $ |
| States | `pending`, `approved`, `rejected`, `sla-critical` (pulse border) |

---

### 4.4 `FlowAgentCard`

**Uso:** Workforce

| Elemento | Spec |
|----------|------|
| Avatar | Icono agente + status dot |
| Name | Sales Agent, etc. |
| Bar | Actividad 24h proporcional |
| Metrics | actions count · $ impact |
| Status | Running / Idle / Error |

---

## 5. Tables

### 5.1 `FlowDataTable`

**Reemplaza:** `table table-hover` Bootstrap

| Feature | Spec |
|---------|------|
| Header | Sticky; 12px uppercase; sort icons |
| Row height | 48px comfortable / 40px compact |
| Hover | `--flow-bg-subtle` |
| Selected | `--flow-brand-primary-subtle` |
| Cell | `FlowEntityCell` para nombre+meta |
| Actions | Icon buttons right; kebab menu |
| Empty | `FlowEmptyState` |
| Loading | `FlowTableSkeleton` |
| Mobile | Card collapse con `data-label` (mantener patrón site.js) |
| Keyboard | Row focus; arrows (mantener mejora existente) |

---

### 5.2 `FlowEntityCell`

```
[Avatar 32px]  Primary name 14px semibold
               Secondary meta 13px muted
```

**Gap vs Attio:** paridad humanización.

---

### 5.3 `FlowBulkBar`

Aparece cuando selección >0; sticky bottom o top table.

`3 seleccionados · [Acción masiva ▾] · Cancelar`

---

## 6. Forms

### 6.1 `FlowInput` / `FlowSelect` / `FlowTextarea`

| State | Style |
|-------|-------|
| Default | 40px height; border `--flow-border-default` |
| Focus | ring 2px primary |
| Error | border danger + message 12px below |
| Disabled | opacity 0.6 |

**Reemplaza:** `form-control`

---

### 6.2 `FlowSearchField`

Icon left; clear button; debounce 300ms.

---

### 6.3 `FlowEntityPicker`

**Reemplaza:** GUID text inputs (Voice — auditoría #14)

Modal search: Customers/Leads/Deals con `FlowDataTable` mini.

---

### 6.4 `FlowFilterPanel`

**Evolución:** `crm-filter-card`

Horizontal chips + «Más filtros» drawer.

Applied filters shown as `FlowChip` removable.

---

## 7. Filters

### 7.1 `FlowChip`

| Variant | Uso |
|---------|-----|
| `filter` | Applied filter |
| `status` | Semantic |
| `ai` | IA-generated tag |

Removable × on filter chips.

---

### 7.2 `FlowFilterDrawer`

Right drawer 400px; form filters; Apply / Reset.

---

## 8. Search

### 8.1 `FlowGlobalSearch` (⌘K)

**Reemplaza:** topbar search decorativo (auditoría #19, #20)

| Section | Items |
|---------|-------|
| Quick actions | New lead, Trust inbox, Revenue |
| Entities | Customers, Deals, Leads (API search) |
| Settings | Navigate |

Palette: 560px wide; centered; `FlowShadow-lg`

Keyboard: ↑↓ navigate; Enter open; Esc close.

---

## 9. Empty States

### 9.1 `FlowEmptyState`

**Evolución:** `_CrmEmptyState.cshtml`

| Elemento | Spec |
|----------|------|
| Illustration | 120px SVG monocromo |
| Title | 18px semibold |
| Message | 14px muted; max 360px |
| CTA | `FlowButton` primary |

**Variants por módulo:** `no-trust-pending`, `no-customers`, `no-integrations`

---

## 10. Skeletons

### 10.1 `FlowSkeleton`

Base shimmer `--flow-bg-subtle` → `#E2E8F0` animation 1.2s.

**Evolución:** `_CrmLoadingSkeleton.cshtml`

---

### 10.2 `FlowTableSkeleton`

5 rows; cell widths variados; circle en columna entidad.

---

### 10.3 `FlowCardSkeleton`

Metric card shape; chart placeholder rect.

---

## 11. Charts

### 11.1 `FlowSparkline`

64×24; sin ejes; semantic color; tooltip on hover.

**Uso:** MetricCard, Command hero.

---

### 11.2 `FlowLineChart` / `FlowBarChart`

Altura 200–280; grid sutil; Inter axis 11px.

**Paleta chart:** primary, accent, muted series — máx 4 series.

**Librería recomendada:** Apache ECharts o Chart.js (decisión implementación).

---

### 11.3 `FlowPipelineMini`

Kanban horizontal scroll; etapas colapsables; deal cards 200px.

**Unifica:** Index pipeline + Deals (auditoría #33).

---

## 12. Dialogs

### 12.1 `FlowModal`

| Size | Width |
|------|-------|
| `sm` | 400px |
| `md` | 560px |
| `lg` | 720px |
| `full` | 90vw |

Overlay rgba(15,23,42,0.5); focus trap; Esc close.

**Reemplaza:** Bootstrap modal.

---

### 12.2 `FlowConfirmDialog`

Danger / warning / info; title + body + Cancel + Confirm.

**Uso:** Trust reject, bulk delete.

---

## 13. Toasts

### 13.1 `FlowToast`

**Evolución:** `_CrmToastContainer`

| Prop | Spec |
|------|------|
| Position | top-right |
| Duration | 4s default; sticky error |
| Variants | success, error, warning, info |
| Action | optional link button |

`aria-live="polite"` (mantener).

---

## 14. Drawers

### 14.1 `FlowDrawer`

| Prop | Spec |
|------|------|
| Width | 480px default; 720px `wide` |
| Side | right (default); left Trust context |
| Overlay | click close |
| Header | title + close |
| Footer | actions sticky |

**Uso:** Deal detail, Customer detail, Agent detail, Call detail.

**Beneficio UX:** Reduce navegación full page (CRM polish fase 4).

---

## 15. Command Palette

### 15.1 `FlowCommandPalette`

Ver §8.1. Componente más importante post-shell para percepción Linear-grade.

**Fases:**
- v1: navegación (fase 1)
- v2: entidades + acciones (fase 5)

---

## 16. Componentes ABOS específicos

### 16.1 `FlowOutcomeChain`

Visualiza: Decision → Execution → Business Outcome → Revenue → Learning

```
[●]──[●]──[○]──[○]──[○]
Decision  Exec   Outcome Rev  Learn
```

Estados desde `IOutcomeFabricService` — backend v0.9.

---

### 16.2 `FlowTrustActions`

Barra fija: Simular | Aprobar (primary) | Rechazar | Rollback (admin)

Shortcuts: A R S

---

### 16.3 `FlowCommsBanner`

**Evolución:** alert comms en layout

| State | UI |
|-------|-----|
| Simulated | amber banner + link Settings |
| Live | subtle green dot «Comms activas» en topbar only |

No muro alert en cada página (auditoría #25).

---

### 16.4 `FlowIntegrationCard`

Logo 40px + name + status dot + last sync + [Connect OAuth] + accordion Advanced

---

### 16.5 `FlowBillingPlanCard`

Stripe-like: plan name, price, features checklist, usage meters, upgrade CTA.

---

## 17. Catálogo de badges y pills

### `FlowBadge`

| Variant | BG |
|---------|-----|
| `neutral` | slate 100 |
| `success` | green 50 |
| `warning` | amber 50 |
| `danger` | red 50 |
| `ai` | teal 50 |

11px font; 600 weight; pill radius.

---

## 18. Botones

### `FlowButton`

| Variant | Uso |
|---------|-----|
| `primary` | 1 por vista |
| `secondary` | outline |
| `ghost` | tertiary |
| `danger` | reject |
| `ai` | accent teal — acciones IA |

Sizes: `sm` 32px, `md` 40px, `lg` 44px

Icon button 36×36 min touch mobile.

---

## 19. Mapeo legado → Flow

| Legado | Flow component |
|--------|----------------|
| `_Layout.cshtml` | `FlowAppShell` |
| `_PageHeader.cshtml` | `FlowPageHeader` |
| `_CrmEmptyState.cshtml` | `FlowEmptyState` |
| `_CrmLoadingSkeleton.cshtml` | `FlowSkeleton` |
| `_CrmToastContainer.cshtml` | `FlowToast` |
| `_CrmRuntimeBar.cshtml` | Integrar en `FlowTopBar` o eliminar |
| `small-box` | `FlowMetricCard` |
| `crm-filter-card` | `FlowFilterPanel` |
| `topbar` | **Eliminar** |
| `stats` / `.stat` | `FlowMetricCard` |
| `list-group` | `FlowDecisionCard` list |

---

## 20. Storybook structure (fase 5)

```
Flow/
  Foundations/Colors
  Foundations/Typography
  Layout/Shell
  Layout/PageHeader
  Data/MetricCard
  Data/DataTable
  Data/Charts
  ABOS/DecisionCard
  ABOS/OutcomeChain
  ABOS/TrustActions
  Feedback/EmptyState
  Feedback/Toast
  Overlay/Modal
  Overlay/Drawer
  Navigation/CommandPalette
```

---

## 21. Prioridad implementación componentes

| Prioridad | Componentes |
|-----------|-------------|
| P0 | Shell, PageHeader, Button, Card, MetricCard, EmptyState |
| P1 | DataTable, DecisionCard, Drawer, Toast, GlobalSearch |
| P2 | TrustActions, OutcomeChain, AgentCard, Charts |
| P3 | EntityPicker, BillingPlanCard, FilterDrawer |

---

*Biblioteca de especificación. Partials Razor se crearán en fase de implementación según UI_REBUILD_MASTERPLAN.md.*
