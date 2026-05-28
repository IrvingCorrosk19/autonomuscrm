# DESIGN_SYSTEM_RUNTIME_GOVERNANCE

## Mandatos definitivos Fase 11
1. **crm-* obligatorio** — nuevos componentes runtime: `crm-runtime-bar`, `crm-runtime-links`, `crm-sticky-runtime`, `crm-density-compact`.
2. **Runtime-first UX** — continuidad client-side antes que peticiones extra.
3. **Responsive-first** — sticky desactivado &lt;768px en barras runtime/ops.
4. **Accessibility-first** — ARIA en navegación runtime y tablas.
5. **Operational continuity first** — `crm_runtime_last`, `ModuleActive`, resume link.
6. **Enterprise consistency first** — partials en `Pages/Shared/`, lógica en `site.js` / `site.css`.

## Prohibido
- Sustituir AdminLTE.
- CSS inline en páginas nuevas.
- Frameworks UI adicionales.

## Archivos fuente de verdad
- `AutonomusCRM.API/wwwroot/css/site.css`
- `AutonomusCRM.API/wwwroot/js/site.js`
- `Pages/Shared/_Layout.cshtml`, `_CrmRuntimeBar.cshtml`

## Base
DESIGN_SYSTEM_OPERATIONS_GOVERNANCE, CRM_COMPONENT_LIBRARY.
