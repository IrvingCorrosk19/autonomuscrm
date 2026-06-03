# WORLD CLASS DESIGN SYSTEM — AutonomusFlow

**Versión:** 1.0 · **Estado:** Diseño (sin implementación)  
**Fuentes:** `AUTONOMUSFLOW_UI_UX_AUDIT.md`, `AUTONOMUSFLOW_MASTER_CONTEXT.md` v0.9  
**Objetivo:** Reemplazar AdminLTE/Bootstrap como lenguaje visual; soportar ABOS y percepción $5,000+/mes.

---

## 1. Filosofía de diseño

### 1.1 Manifiesto

AutonomusFlow no es un CRM con IA añadida. Es un **sistema operativo de negocio autónomo** donde la interfaz debe transmitir tres verdades en cada pixel:

1. **La máquina ya está trabajando** (agentes, decisiones, outcomes).
2. **El humano supervisa, no opera** (Trust, no formularios infinitos).
3. **El dinero es la métrica hero** (revenue impact, pipeline, churn prevented — no «registros actualizados»).

La UI actual contradice esto: parece **panel de administración** (AdminLTE, tablas, tokens en pantalla) mientras el backend ejecuta ciclos autónomos reales (MASTER_CONTEXT: Worker Up, Outcome Fabric, 6 agentes).

### 1.2 Posicionamiento visual

| No somos | Somos |
|---------|--------|
| AdminLTE / Bootstrap template | Sistema propio **Flow** |
| CRM record-centric | **Outcome-centric** |
| Formularios y tablas por defecto | **Decision surfaces** + registros bajo demanda |
| Dashboard de KPIs genéricos | **Command narrative** (qué hizo la IA, qué falta aprobar, cuánto $) |
| Demo con datos falsos | **Truthful UI** — vacío honesto > mock |

### 1.3 Referentes (qué tomamos, qué rechazamos)

| Referente | Tomamos | Rechazamos |
|-----------|---------|------------|
| **Stripe** | Densidad baja, precisión tipográfica, confianza en billing | Paleta solo blanco (necesitamos modo oscuro ops) |
| **Linear** | Velocidad percibida, keyboard-first, un solo sistema | Oscuridad total (CEO demo en luz) |
| **Attio** | Relaciones, espacio, CRM «nuevo» | Minimalismo sin capa enterprise |
| **Notion** | Bloques, jerarquía calmada | Falta de «ops urgency» |
| **HubSpot** | Onboarding, claridad comercial | Naranja masivo, look marketing |
| **Salesforce** | Densidad enterprise, objetos | Lightning visual clutter |
| **Vercel** | Negro/blanco, precisión dev | Estética solo developer |

**Síntesis Flow:** *Linear × Stripe × Attio* en capa shell + *Salesforce density* solo en tablas operativas bajo toggle «compact».

---

## 2. Principios

### 2.1 Principios de producto (5)

| # | Principio | Implicación UI |
|---|-----------|----------------|
| P1 | **Truth over theater** | Prohibido KPI hardcodeado (auditoría: Agents 1,247, Dashboard mock). Empty = CTA real. |
| P2 | **One surface, one story** | Cada pantalla responde: ¿qué pasa? ¿qué riesgo? ¿qué $? ¿qué acción? |
| P3 | **Supervision, not data entry** | CRM existe; no es home. Home = Command. |
| P4 | **Progressive disclosure** | Tokens OAuth, GUIDs, JSON → capas «Avanzado», nunca default. |
| P5 | **Enterprise calm** | Sin gritos visuales; urgencia = semántica (color + motion), no rojo everywhere. |

### 2.2 UX Principles (10)

1. **30-second comprehension** — CEO entiende categoría sin leer manual.
2. **F-pattern for executives** — Hero $ → 3 riesgos → 1 acción pendiente.
3. **Z-pattern for operators** — Filtro → tabla → bulk → detalle drawer.
4. **Keyboard parity** — ⌘K global; Trust: A/R/E approve/reject/expand.
5. **No dead controls** — Eliminar search/chips decorativos (auditoría Settings/Agents).
6. **Consistent wayfinding** — Un header system; cero `content-header` vs `topbar` dual.
7. **Mobile = approve + alert** — Trust y notificaciones primero; no CRM completo en móvil v1.
8. **Locale ES primero** — Labels producto en español; términos marca en inglés donde estándar (Command Center).
9. **Error = recovery** — Comms simulación: banner + ruta a Settings, no solo alert Bootstrap.
10. **Accessibility = ship criterion** — WCAG 2.2 AA en componentes core antes de nuevas features.

### 2.3 Visual Principles (10)

1. **Restrained palette** — Máx. 1 acento + neutros; sin arcoíris AdminLTE small-box.
2. **Type carries hierarchy** — Tamaño y peso, no solo color.
3. **8px spatial rhythm** — Todo múltiplo de 8 (ver Spacing).
4. **Elevation = meaning** — 3 niveles de sombra máximo en light mode.
5. **Borders hairline** — `#E5E7EB` family, no `dee2e6` Bootstrap.
6. **Icons semantic** — Lucide o Phosphor custom set; Font Awesome solo transición.
7. **Charts > numbers in rows** — Sparklines en Command; tablas para drill-down.
8. **Avatar + entity** — Humanizar leads/customers (gap vs Attio).
9. **Motion with purpose** — 150–250ms; reduced-motion obligatorio.
10. **Dark mode ops** — Opcional fase 3; light = demo CEO, dark = NOC.

---

## 3. Design Tokens

### 3.1 Naming convention

```
--flow-{category}-{property}-{variant?}
```

### 3.2 Color System

#### Brand

| Token | Light | Uso |
|-------|-------|-----|
| `--flow-brand-primary` | `#4F46E5` (Indigo 600) | Acción primaria, marca — **no** `#007bff` Bootstrap |
| `--flow-brand-primary-hover` | `#4338CA` | Hover |
| `--flow-brand-primary-subtle` | `#EEF2FF` | Backgrounds selección |
| `--flow-brand-accent` | `#0D9488` (Teal 600) | IA / autonomía / «machine active» |
| `--flow-brand-accent-subtle` | `#CCFBF1` | Badges IA |

**Rationale:** Indigo = confianza enterprise (Stripe-adjacent sin copiar). Teal = «sistema vivo» distinto de HubSpot naranja y SF azul saturado.

#### Neutrals (light)

| Token | Hex | Uso |
|-------|-----|-----|
| `--flow-bg-canvas` | `#F8FAFC` | App background |
| `--flow-bg-surface` | `#FFFFFF` | Cards |
| `--flow-bg-surface-raised` | `#FFFFFF` | Modals |
| `--flow-bg-subtle` | `#F1F5F9` | Table header, sidebar hover |
| `--flow-border-default` | `#E2E8F0` | Borders |
| `--flow-border-strong` | `#CBD5E1` | Inputs focus ring base |
| `--flow-text-primary` | `#0F172A` | Headlines |
| `--flow-text-secondary` | `#475569` | Body |
| `--flow-text-muted` | `#94A3B8` | Meta, timestamps |
| `--flow-text-inverse` | `#F8FAFC` | On dark sidebar |

#### Semantic

| Token | Hex | Uso |
|-------|-----|-----|
| `--flow-success` | `#059669` | Won, approved, live comms |
| `--flow-warning` | `#D97706` | Pending HITL, SLA |
| `--flow-danger` | `#DC2626` | Churn, reject, failed |
| `--flow-info` | `#2563EB` | Informational only |

**Regla:** Prohibido `bg-info`, `bg-warning` small-box AdminLTE. KPIs usan superficie blanca + borde izquierdo semántico 3px.

#### Dark (fase 3)

| Token | Hex |
|-------|-----|
| `--flow-bg-canvas-dark` | `#0B0F19` |
| `--flow-bg-surface-dark` | `#111827` |
| `--flow-text-primary-dark` | `#F1F5F9` |

### 3.3 Typography System

| Rol | Familia | Size | Weight | Line-height |
|-----|---------|------|--------|-------------|
| Display | **Inter** (variable) | 32px | 600 | 1.2 |
| H1 Page | Inter | 24px | 600 | 1.25 |
| H2 Section | Inter | 18px | 600 | 1.3 |
| H3 Card | Inter | 15px | 600 | 1.35 |
| Body | Inter | 14px | 400 | 1.5 |
| Body small | Inter | 13px | 400 | 1.45 |
| Caption | Inter | 12px | 500 | 1.4 |
| Mono | **JetBrains Mono** | 13px | 400 | 1.5 |

**Kicker:** 11px, 600, letter-spacing `0.08em`, uppercase, `--flow-text-muted`.

**Reemplaza:** Source Sans Pro (auditoría: genérico AdminLTE).

### 3.4 Spacing System

Base unit: **8px**

| Token | Value |
|-------|-------|
| `--flow-space-1` | 4px |
| `--flow-space-2` | 8px |
| `--flow-space-3` | 12px |
| `--flow-space-4` | 16px |
| `--flow-space-5` | 24px |
| `--flow-space-6` | 32px |
| `--flow-space-7` | 40px |
| `--flow-space-8` | 48px |
| `--flow-space-9` | 64px |

**Page gutter:** 24px desktop, 16px mobile.  
**Card padding:** 20px (no AdminLTE 1.15rem inconsistente).

### 3.5 Border Radius

| Token | Value | Uso |
|-------|-------|-----|
| `--flow-radius-sm` | 6px | Chips, badges |
| `--flow-radius-md` | 8px | Inputs, buttons |
| `--flow-radius-lg` | 12px | Cards |
| `--flow-radius-xl` | 16px | Modals, command palette |
| `--flow-radius-full` | 9999px | Avatars, pills |

**Eliminar:** `--crm-radius: 0.35rem` (~5.6px) mezclado con 0.6rem.

### 3.6 Shadows

| Token | Value | Uso |
|-------|-------|-----|
| `--flow-shadow-xs` | `0 1px 2px rgba(15,23,42,0.04)` | Inputs |
| `--flow-shadow-sm` | `0 1px 3px rgba(15,23,42,0.06), 0 1px 2px rgba(15,23,42,0.04)` | Cards |
| `--flow-shadow-md` | `0 4px 12px rgba(15,23,42,0.08)` | Dropdowns, drawer |
| `--flow-shadow-lg` | `0 12px 32px rgba(15,23,42,0.12)` | Modals, command palette |

**No** sombras azules AdminLTE `rgba(0,123,255,0.22)` en nav active.

### 3.7 Layout grid

- **Sidebar:** 256px fijo desktop; icon-only 72px collapsed.
- **Content max-width:** 1440px centered en vistas executive; fluid en tablas.
- **12-column grid** con gap 24px para dashboards.

### 3.8 Z-index scale

| Layer | z-index |
|-------|---------|
| Base | 0 |
| Sticky header | 100 |
| Sidebar | 200 |
| Drawer | 300 |
| Modal | 400 |
| Toast | 500 |
| Command palette | 600 |

---

## 4. Motion System

### 4.1 Timing

| Token | Duration | Easing |
|-------|----------|--------|
| `--flow-motion-fast` | 120ms | `cubic-bezier(0.4, 0, 0.2, 1)` |
| `--flow-motion-base` | 200ms | same |
| `--flow-motion-slow` | 320ms | same |
| `--flow-motion-spring` | 400ms | `cubic-bezier(0.34, 1.56, 0.64, 1)` (solo modals) |

### 4.2 Patterns

| Patrón | Uso |
|--------|-----|
| **Fade + translate Y 4px** | Page enter (reemplaza `crm-page-enter` 280ms) |
| **Skeleton shimmer** | Loading tablas Command, 360 |
| **Stagger 40ms** | Lista decisiones Trust |
| **Pulse once** | Nueva decisión HITL (máx 2 pulses) |
| **No motion** | `prefers-reduced-motion: reduce` → instant |

### 4.3 Prohibido

- Bounce exagerado Bootstrap
- Parallax decorativo
- Animaciones en KPI numbers (confunde verdad de datos)

---

## 5. Accessibility

### 5.1 Target

**WCAG 2.2 Level AA** en shell + Command + Trust + Billing.

### 5.2 Requisitos no negociables

| Área | Estándar |
|------|----------|
| Contraste texto | ≥ 4.5:1 body; ≥ 3:1 large |
| Focus | `:focus-visible` ring 2px `--flow-brand-primary` offset 2px |
| Touch targets | ≥ 44×44px en móvil Trust actions |
| Skip link | «Saltar al contenido» en layout |
| Tables | `scope`, captions, `data-label` mobile (mantener mejora existente) |
| Forms | Labels explícitos; errores `aria-invalid` + `aria-describedby` |
| Live regions | Toasts `aria-live="polite"`; Trust new `assertive` |
| Color | Nunca solo color para estado (icono + texto) |

### 5.3 Auditoría gap actual

- Sin `focus-visible` sistemático (UI_UX_AUDIT §4)
- Login sin skip link
- Badges Bootstrap contraste marginal

---

## 6. Iconografía e ilustración

- **Icon set:** Lucide 24px stroke 1.75 — consistente, moderno.
- **Logo:** Wordmark «Autonomus**Flow**» — Flow en `--flow-brand-accent`; sin «CRM».
- **Empty states:** SVG line-art monocromo + 1 línea copy + CTA (reutilizar concepto `_CrmEmptyState` con nueva estética).
- **Integrations:** Logos oficiales HubSpot/SF/Google (marca reconocible = confianza).

---

## 7. Densidad modes

| Mode | Uso | Row height | Font |
|------|-----|------------|------|
| **Executive** | Command, CEO home | N/A cards | 14–15px |
| **Comfortable** | Default tables | 48px | 14px |
| **Compact** | Ops power users | 40px | 13px |

Persistencia: `localStorage` key `flow_table_density` (evolución de `crm_table_density`).

---

## 8. Content & voice

| Contexto | Tono |
|----------|------|
| CEO / Executive | Outcomes, $, riesgo, confianza |
| Operator | Acción directa, sin jerga event-bus |
| Developer | Solo en Settings → Avanzado |
| IA | «Recomendación», «Confianza», «Impacto estimado» — nunca «magic» |

**Prohibido en UI producción:** «MVP», «Tenant ID» en login enterprise, «Event-Driven · Multi-tenant» en footer.

---

## 9. Migración desde legado

| Legado (auditoría) | Reemplazo Flow |
|--------------------|----------------|
| AdminLTE 3.2 CDN | CSS bundle propio `flow.css` |
| Bootstrap 4.6 grid | CSS grid + flex utilities mínimas propias |
| `small-box` | `FlowMetricCard` |
| `content-header` | `FlowPageHeader` |
| `topbar` custom | Eliminado — unificado |
| `badge badge-*` | `FlowBadge` |
| Font Awesome | Lucide |
| jQuery widgets | Vanilla + Alpine opcional (decisión fase 1) |

---

## 10. Governance del design system

| Artefacto | Owner | Cadencia |
|-----------|-------|----------|
| Tokens | Design Systems Lead | Versionado semver en `flow-tokens.css` |
| Figma library | Product Design | Paridad 1:1 con Razor partials |
| Component RFC | UX Architect | Antes de nuevo partial |
| Visual QA | CXO | Cada release; regresión vs CEO 30s test |
| A11y audit | QA | Por fase en MASTERPLAN |

---

## 11. Métricas de éxito del design system

| Métrica | Baseline (auditoría) | Target |
|---------|---------------------|--------|
| UI score | 52 | 90+ |
| UX score | 58 | 90+ |
| Consistencia headers | 42 | 95 |
| Pantallas con mock data | ≥3 | 0 |
| WCAG AA páginas críticas | ~48 | 90 |
| Tiempo comprensión CEO (test 5 users) | N/A | <30s a narrativa ABOS |

---

*Documento de diseño. No implica cambios en repositorio hasta fase de implementación aprobada.*
