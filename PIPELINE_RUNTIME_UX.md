# PIPELINE_RUNTIME_UX

## Objetivo
Continuidad en progresión de leads, pipeline y deals sin reescribir páginas CRM.

## Soporte Fase 11
- Acceso Pipeline desde barra runtime en cualquier pantalla.
- Sidebar activo en `/Deals/*`.
- “Continuar” restaura última ruta (ej. `/Deals/Details?id=…`).
- `crmUi.trackOperation` disponible para acciones async con toast + retry.

## Flujo operativo esperado
1. Dashboard → revisar KPIs.
2. Runtime bar → Leads o Pipeline.
3. Trabajo en detalle → cambio a Workflows/Agents.
4. “Continuar” → vuelta al deal/lead sin buscar en historial.

## Próximos incrementos (fuera de alcance crítico)
- Badges de etapa en barra runtime (requiere datos server-side).
- Shortcuts de etapa en tabla Deals (keyboard).

## Base documental
PIPELINE_AND_CRM_FLOW_UX, DASHBOARD_OPERATIONS_EXPERIENCE.
