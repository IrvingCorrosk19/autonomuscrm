# FRONTEND_REFACTOR_GUIDE

## Estrategia incremental

1. En cada vista: reemplazar inline styles por `crm-*`.
2. Sustituir bloques ad-hoc por shared partials.
3. Unificar formularios con `crm-form-grid`.
4. Mantener AdminLTE como base de layout y navegación.

## Política
Refactor sin alterar contratos backend ni rutas.
