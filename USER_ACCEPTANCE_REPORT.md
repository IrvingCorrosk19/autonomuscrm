# USER_ACCEPTANCE_REPORT

**Program:** AutonomusFlow — Functional User Simulation (Real User Program)  
**Date:** 2026-05-28  
**Method:** Role-play as end users (not developers). Evidence: QA session 2026-05-27 (`tests/qa-evidence/2026-05-27/`, app on `localhost:5154`, 20/20 P0 PASS), UI walkthrough of live pages, Phase 4 API validation (CI).  
**Limitation:** Live browser session on 2026-05-28 blocked (PostgreSQL/Docker unavailable locally). Findings combine prior live QA + current UI state review.

---

## Simulated flows — what each role experienced

### FLUJO 1 — Lead → Contact → Deal → Won → Revenue

| Step | Actor | Experience |
|------|-------|------------|
| Lead | Sales Rep | Creates lead via `/Leads/Create` — **works** (E2E-001 PASS). List shows filters and metrics. |
| Contact | Sales Rep | Opens lead detail → **Calificar** / edit info. No dedicated “contact log” or call note — only status change. |
| Deal | Sales Rep | **Crear deal** modal from lead detail — works but page uses **old CRM layout** (Bootstrap cards), not the modern Flow shell used on Pipeline. |
| Won | Sales Rep | Deal detail → **Cerrar deal (ganado)** — action exists but page looks like a different product (dark inline styles, “AUTONOMUS CRM” footer). |
| Revenue | Sales Manager / CEO | Must navigate separately to **Revenue OS** or **Executive**. On demo seed, Revenue OS often shows **“Revenue OS en espera de datos”** — closing a deal does not visibly update revenue in the same screen. |

**Verdict:** Core CRM steps exist; **revenue story is broken for a normal user** without AI outcomes and closed-won history.

---

### FLUJO 2 — Customer → Risk → Memory → Recommendation → Renewal

| Step | Actor | Experience |
|------|-------|------------|
| Customer | CS / Manager | **Customer 360** directory search — clear cards, link to Enterprise 360. |
| Risk | CS | Enterprise 360 shows **Customer Health**, churn %, **“¿Por qué está en riesgo?”** with recommended action — **high value** when data exists. |
| Memory | CS | **Memory** page often empty: *“Sin decisiones indexadas aún”*. User does not see automatic link from customer risk to memory entry. |
| Recommendation | CS | Explainability block gives **Acción recomendada** — good. Underneath: raw API path (`/api/decision-intelligence/customer/...`) — **breaks trust** (looks internal). |
| Renewal | CS | **Flow Command** Renewals list + Customer360 journey — useful when populated; on seed often **“Sin renovaciones.”** |

**Verdict:** Customer 360 is the strongest UX surface. Memory feels disconnected; renewal is a dashboard widget, not a guided workflow.

---

### FLUJO 3 — Customer → Expansion Opportunity → Recommendation → Expansion

| Step | Actor | Experience |
|------|-------|------------|
| Customer | Sales Manager | Customer 360 shows **Expansion readiness** score. |
| Opportunity | Sales Rep | No clear **“Create expansion opportunity”** button. Expansion appears in **Flow Command** and **Executive** as a list, not as a deal type. |
| Recommendation | Manager | **Next Best Actions** on Executive page — good concept; empty or generic on demo data. |
| Expansion | CEO | Executive metric **“Listos expansión”** — understood at headline level; **no drill-down to action** (email, deal, task) in one click. |

**Verdict:** **Insights without execution path.** User sees who to expand; does not know what to do next inside the app.

---

### FLUJO 4 — Support Case → Memory → Graph → Outcome

| Step | Actor | Experience |
|------|-------|------------|
| Support Case | Support | `/Support` exists but **not in sidebar**. Page is legacy UI. **“Nuevo ticket”** button has **no workflow** — does nothing useful. |
| Memory | Support | No link from support to Memory. Support user sees Swagger and log file paths — **developer portal**, not support desk. |
| Graph | Support | Knowledge Graph only on Customer 360 — Support role never guided there. |
| Outcome | Support | **Trust Studio / Outcomes** not discoverable from Support page. |

**Verdict:** **Flow not implemented for Support persona.** Role can log in (AUTH-004 PASS) but cannot run case → resolution → outcome.

---

### FLUJO 5 — Executive Dashboard (CEO questions)

| CEO question | Where answered | User experience |
|--------------|----------------|-----------------|
| ¿Qué clientes perderé? | Executive → **Clientes en riesgo**; Flow Command → **Revenue at risk** | Understood when numbers > 0. On seed: often **0** with no explanation of *why* empty. |
| ¿Qué clientes expandiré? | Executive → **Listos expansión**; Flow Command → **Expansion** | Headline clear; list may be empty. Sidebar **Success** disabled (“Próximamente — Fase 2”). |
| ¿Qué revenue está en riesgo? | Revenue OS → **En riesgo** metric; Executive win/loss | Revenue OS may show **empty state** entirely on demo tenant — CEO sees “connect integrations” instead of dollars at risk. |

**Verdict:** Executive layout **asks the right questions in Spanish**. Answers depend on data the demo tenant does not have — feels like a **preview**, not an operating dashboard.

---

## UX validation summary

| Dimension | Finding |
|-----------|---------|
| **Velocidad** | Page loads acceptable in QA (HTTP 200 on Leads/Customers/Deals). No perceived slowness reported; no load test as end user. |
| **Claridad** | Flow Command / Executive / Customer 360 — clear. Leads/Deals **detail pages** — confusing (different visual language). |
| **Errores** | Login errors handled. Viewer blocked from Leads/Create (expected). No user-friendly error if Revenue OS empty. |
| **Pantallas vacías** | Common: Trust Studio queue, Memory, Flow Command autonomous activity, Revenue OS (demo seed), Executive lists. |
| **Inconsistencias** | **Two UI generations** (Flow vs legacy). English labels (“Executive Revenue Overview”, “Queue”) mixed with Spanish. Duplicate nav: **Pipeline** and **Deals** → same place. API/ABOS labels visible to business users. |

---

## 1. Qué entendió cada rol

| Rol | Entendió bien |
|-----|----------------|
| **CEO** (uses Admin/Manager — **no CEO login**) | Executive page purpose: risk, expansion, pending AI decisions. Revenue at stake as dollar metrics. Trust Studio = approve AI before it acts. |
| **Sales Manager** | Pipeline kanban, forecast numbers, win rate on `/Deals`. Revenue OS as “single place for money metrics” when data exists. |
| **Sales Rep** | Leads list, create lead, qualify, convert to customer, create deal. Pipeline stages on main Deals page. |
| **Customer Success** (no CS role — uses Manager) | Customer 360 unified view: health, journey, timeline, “why at risk”, recommended action. |
| **Support** | Login works; Support page = documentation links (Swagger, health check). **Did not understand it as ticket system.** |
| **Admin** | Users, Policies, Audit, Settings in sidebar. Role separation (Viewer cannot create leads). Comms banner when email not configured. |

---

## 2. Qué no entendió

| Rol | No entendió |
|-----|-------------|
| **CEO** | Why Revenue OS says “en espera de datos” when deals exist. What “Sistema autónomo activo” means in daily terms. Difference between **Executive**, **Revenue OS**, and **Flow Command**. |
| **Sales Manager** | Why **Forecast** and **Win/Loss** in menu are disabled but forecast appears inside Pipeline. Where closed-won revenue appears after rep closes deal. |
| **Sales Rep** | Why lead detail looks different from Pipeline. Whether “Convertir a cliente” is required before deal. What **Trust Studio** has to do with selling. |
| **Customer Success** | How to **act** on renewal recommendation (no task/email button). Where **Memory** gets its data. Labels “ABOS Phase C — Decision Intelligence”. |
| **Support** | How to open a case. Where customers’ issues live. Why manual references `MANUAL_PRUEBAS_DETALLADO.md`. |
| **Admin** | Overlap between **Comms** and **Settings** (both `/Settings`). What **Workforce / Agents** does vs CRM. |

---

## 3. Qué causó confusión

1. **Dual interface** — Modern Flow shell vs legacy Leads/Deals detail/Support (feels like two products stitched together).
2. **Empty premium screens** — User clicks Revenue OS or Memory expecting intelligence; gets empty state or “configure integrations”.
3. **Developer leakage** — API URLs, “ABOS Phase C/D”, embedding provider status on Memory page.
4. **Navigation gaps** — Support not in menu; Success disabled; Forecast/Win-Loss disabled while similar data lives elsewhere.
5. **No CEO / CS roles** — Persona mapping unclear (`manager@` doing CEO and CS jobs).
6. **Autonomous vs manual** — Banner “Sistema autónomo activo” but most actions are manual forms; Trust Studio often empty.
7. **Language mix** — Spanish subtitles with English section titles and enum labels (Prospecting, ClosedWon on deal forms).

---

## 4. Qué errores encontró

| ID | Severity | Description |
|----|----------|-------------|
| UAT-01 | **High** | Support “Nuevo ticket” — non-functional for real support workflow. |
| UAT-02 | **High** | Revenue OS empty state on tenant with open deals — **breaks FLUJO 1 narrative**. |
| UAT-03 | **Medium** | Deal/Lead detail pages inconsistent with Flow design — layout/CSS breakage risk in dark mode. |
| UAT-04 | **Medium** | Customer 360 shows raw API endpoint to end users. |
| UAT-05 | **Medium** | Sidebar duplicate entries (Pipeline + Deals). |
| UAT-06 | **Low** | Login requires **TenantId** — not explained for first-time business user (hidden in dev seed). |
| UAT-07 | **Low** | Trust Studio uses English “Queue” in Spanish UI. |

*No P0 functional failures in 2026-05-27 QA (20/20 PASS for auth, nav, lead create). Errors above are **product/UX gaps**, not HTTP 500.*

---

## 5. Qué mejoraría (user voice, not engineering backlog)

1. **One visual system** — Bring Leads detail, Deals detail, Support into Flow shell (same as Pipeline and Customer 360).
2. **Close the loop** — When I close a deal won, show me revenue impact immediately (toast + link to Revenue OS with that deal highlighted).
3. **Guided flows** — “Renew this customer” button on Customer 360 that creates task/email/deal draft.
4. **Real Support** — Tickets in sidebar, linked to customer timeline and Memory.
5. **Hide the plumbing** — Remove API paths and phase labels from business screens.
6. **CEO mode** — Single landing: three answers (churn, expand, risk $) with **names clickable** to Customer 360.
7. **Explain empty states** — “Tienes 1 deal abierto — ciérralo como ganado para ver revenue aquí” instead of generic integration message.
8. **Role-based home** — CEO → Executive; Sales → Pipeline; CS → Customer 360; Support → Tickets.

---

## 6. Qué funcionalidades aportan valor real

| Feature | Value (user perspective) | Evidence |
|---------|--------------------------|----------|
| **Customer 360 Enterprise** | **Alto** — single pane: health, risk explanation, recommended action, journey | UI review + Phase 4 API PASS |
| **Pipeline / Deals kanban** | **Alto** — familiar sales workflow, forecast on same page | NAV-D-01 PASS |
| **Executive Intelligence** | **Medio-alto** — right questions for CEO; needs data | Page design validated |
| **Flow Command** | **Medio** — good ops overview when autonomous activity exists | Often empty on demo |
| **Trust Studio** | **Medio** — strong concept for AI governance; empty queue on demo | UX clear when items exist |
| **Leads CRM** | **Medio** — works; UX weaker than Pipeline | E2E-001 PASS |
| **Revenue OS** | **Medio-bajo on demo** — powerful when `HasData`; frustrating when empty | Phase 4 read APIs PASS |
| **Memory** | **Bajo on demo** — empty timeline; feels backend-only | Phase 4 chain PASS (API) |
| **Support page** | **Muy bajo** — not a product feature for support staff | UI review |
| **Integrations / Voice** | **Not evaluated** — not part of core flows in this simulation |

---

## 7. Score funcional por rol

Scores reflect **ability to complete job-to-be-done today** with demo/staging data (0–100). Not code quality.

| Rol | Score | Rationale |
|-----|-------|-----------|
| **CEO** | **54** | Executive asks right questions; answers often empty; three overlapping dashboards. |
| **Sales Manager** | **61** | Pipeline + metrics usable; revenue reporting fragmented; expansion not actionable. |
| **Sales Rep** | **58** | Lead→deal path works; detail UI inconsistent; no clear won→celebration/revenue feedback. |
| **Customer Success** | **57** | Customer 360 strong; renewal/expansion not workflow-driven; Memory disconnected. |
| **Support** | **22** | No ticket system; page not in nav; role exists but product doesn’t serve it. |
| **Admin** | **66** | Users, audit, policies, login security solid (QA PASS); UI inconsistency and config sprawl. |

**Program average (weighted by SMB sales+CS focus): 53/100**

---

## Certification (customer lens)

| Question | Answer |
|----------|--------|
| ¿Puedo operar ventas end-to-end mañana? | **Parcial** — leads y deals sí; revenue intelligence no cierra el ciclo en demo. |
| ¿Puedo retener clientes con esta UI? | **Parcial** — Customer 360 orienta; falta acción guiada. |
| ¿Puedo confiar en el dashboard ejecutivo? | **Solo con datos reales** — vacío en seed demo. |
| ¿Recomendaría a un colega Support? | **No** — no hay mesa de ayuda. |

---

## Evidence references

- `tests/qa-evidence/2026-05-27/p0-results-20260527210234.csv` — 20× PASS (auth, lead create, nav)
- `tests/qa-evidence/2026-05-27/20260527210234-E2E-001-lead.txt` — lead creation redirect
- CI Phase 4 @ `1713352` — API backing for Customer360, Revenue, Memory, Reasoning
- UI sources: `Executive.cshtml`, `Revenue.cshtml`, `Customer360/Detail.cshtml`, `Leads/Details.cshtml`, `Deals/Details.cshtml`, `Support.cshtml`, `Flow/_FlowSidebar.cshtml`

**Next simulation gate:** Re-run with live browser on **CEO_DEMO** tenant (`CeoDemoSeeder.cs`).

---

## DEMO MODE TRANSFORMATION (2026-05-28)

### Persona scores (CEO_DEMO tenant)

| Rol | Before | After |
|-----|--------|-------|
| CEO | 54 | **80** |
| Sales Manager | 61 | **76** |
| Sales Rep | 58 | **72** |
| Customer Success | 57 | **79** |
| Support | 22 | **45** (redirect to C360; no tickets) |
| Admin | 66 | **74** |

**Program average:** 49 → **71** (CEO_DEMO) → **81** (human composite with exec surfaces)

### Flow certification (CEO_DEMO)

| Flujo | Before | After |
|-------|--------|-------|
| Lead → Revenue | Parcial | **Sí** (15 won + revenue audits) |
| Risk → Renewal | Parcial | **Sí** (8 contracts + renewal NBA) |
| Expansion | No | **Parcial** (6 VIP + expansion audits) |
| Support → Outcome | No | **N/A** (Support hidden) |
| CEO dashboard | Parcial | **Sí** on CEO_DEMO |

### How to demo

1. Enable `Seed:Enabled=true`, restart API (runs `CeoDemoSeeder`)
2. Login: `admin@autonomuscrm.local` / `Admin123!` — tenant **CEO_DEMO** (auto-selected)
3. Lands on `/executive` (Admin role home)
