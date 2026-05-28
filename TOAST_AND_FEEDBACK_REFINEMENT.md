# TOAST_AND_FEEDBACK_REFINEMENT

## `crmUi.toast()` refinado
- Soporta `options.durationMs`.
- Soporta accion contextual (`actionText` + `onAction`) para retry.
- Anima entrada/salida (`is-entering`, `is-leaving`) con transiciones suaves.

## Nuevo helper
- `crmUi.trackOperation(name, promise, options)` para feedback async operacional.
