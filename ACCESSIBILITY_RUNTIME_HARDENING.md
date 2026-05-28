# ACCESSIBILITY_RUNTIME_HARDENING

## Validado / reforzado Fase 11
| Criterio | Implementación |
|----------|----------------|
| Navegación runtime | `role="navigation"`, `aria-label`, `aria-current="page"` en módulo activo |
| Teclado tablas | ↑/↓ entre filas con foco visible |
| Modales | Focus trap, Escape, Tab ciclo |
| Motion | `prefers-reduced-motion` desactiva page-enter |
| Toasts | `role="status"`, `aria-live="polite"` |
| Resume link | Texto descriptivo + `title` |

## Long-session
Densidad y barras sticky reducen scroll repetitivo (menor fatiga motor).

## Overlays
`crm-overlay-modal` compatible con lectores si títulos y labels en markup existente se mantienen.

## Pendiente QA manual
- [ ] Solo teclado: recorrer runtime bar + sidebar + tabla
- [ ] Contraste barra runtime en zoom 200%
- [ ] Screen reader anuncia módulo activo al cambiar página

## Base
ACCESSIBILITY_LONG_SESSION_UX, ACCESSIBILITY_DAILY_USAGE.
