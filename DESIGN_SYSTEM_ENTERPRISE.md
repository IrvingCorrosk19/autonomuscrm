# DESIGN_SYSTEM_ENTERPRISE

Fecha: 2026-05-27

## Objetivo
Consolidar la UI actual en un Design System enterprise reusable sobre AdminLTE, sin cambiar backend ni framework.

## Entregables implementados
- Tokens visuales ampliados en `site.css`.
- Utilidades `crm-*` para layout, cards, forms, table y estados.
- Componentes shared: `_CrmEmptyState`, `_CrmLoadingSkeleton`, `_CrmToastContainer`.
- API JS `window.crmUi` para toast/loading.

## Principios
- Reutilización sobre excepción.
- Deuda visual incremental, no ruptura.
- Mobile-first en tablas y acciones.
