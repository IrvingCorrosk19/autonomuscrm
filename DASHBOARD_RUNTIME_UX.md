# DASHBOARD_RUNTIME_UX

## Alcance
`Index.cshtml` como hub operacional principal.

## Optimizaciones runtime
1. **KPI scanning** — layout existente + densidad compact opcional global.
2. **Sticky quick-actions** — `crm-ops-bar-card crm-sticky-runtime` bajo barra runtime global.
3. **Densidad** — toggle heredado de dashboard toolbar; persistencia global en `localStorage`.
4. **Continuidad contextual** — barra runtime + ops bar local (Leads, Pipeline, Workflows, Agents, Support, onboarding).
5. **Priorización** — quick start y onboarding adoption cards (fases 8–9) preservados.

## Ergonomía real
- Doble capa sticky en desktop: runtime (z-index 1015) + ops dashboard (1010).
- En móvil sticky desactivado para evitar solapamiento con AdminLTE navbar.

## QA dashboard
- [ ] KPIs legibles en compact y comfortable
- [ ] Ops bar visible tras scroll 2+ pantallas
- [ ] Enlaces ops + runtime no duplican foco trap
