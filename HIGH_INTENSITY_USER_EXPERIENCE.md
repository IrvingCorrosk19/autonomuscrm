# HIGH_INTENSITY_USER_EXPERIENCE

## Perfil
Usuarios con sesiones largas (4–8 h): ventas, ops, CS, soporte.

## Refinamientos Fase 11
| Área | Mejora |
|------|--------|
| Tablas | Densidad compact/comfortable **persistida** entre sesiones |
| Navegación | Barra runtime + sidebar activo en subpáginas |
| Multitarea | Retorno rápido al módulo anterior sin perder ruta |
| Fatiga visual | Transiciones mínimas; `prefers-reduced-motion` respetado |
| Dashboard | Ops bar sticky bajo barra runtime |

## Comodidad sostenida
- Menos reclicks al sidebar para saltos frecuentes Leads ↔ Pipeline ↔ Workflows.
- Estados `active` / `aria-current` en módulo actual de la barra runtime.

## Validación recomendada
1. Sesión simulada: 20+ cambios de módulo.
2. Verificar densidad compact tras recargar.
3. Verificar “Continuar” tras Leads → Deals → Agents.
