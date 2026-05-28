# FORM_RUNTIME_EXPERIENCE

## Estado
Formularios enterprise en Agents, Settings, Users, Workflows (migrados) mantienen:
- Overlays `crm-overlay-modal` con focus trap y Escape.
- Validación visual vía clases `crm-*` y feedback flash/toast.

## Runtime Fase 11
- Modales observados por `MutationObserver` al abrir (`display: flex`).
- `crmUi.trackOperation` para confianza en guardados async.
- Densidad compact reduce altura de cards en formularios largos.

## Mobile operacional
- Modales scroll-safe (`modal-dialog-scrollable`).
- Labels en tablas relacionadas en vistas mixtas tabla+form.

## Pendiente
Formularios Leads/Customers/Create con inline styles: migración incremental futura.

## Referencia
FORM_OPERATIONS_UX, ADVANCED_FORM_EXPERIENCE_FINAL.
