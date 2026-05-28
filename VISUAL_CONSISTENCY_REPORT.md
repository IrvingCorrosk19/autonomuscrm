# VISUAL_CONSISTENCY_REPORT

## Criterios de consistencia establecidos

- Spacing base unificado (`gutter`, padding de card, separación vertical).
- Tipografía y jerarquía de títulos estandarizada.
- Componentes reutilizables con mismos radios/sombras.
- Estados visuales de botones/campos homogéneos.
- Badges/pills unificados (forma cápsula y legibilidad).

## Vistas verificadas

- `Index`, `Leads`, `Customers`, `Deals`, `Users`, `Workflows`, `Policies`, `Audit`, `Settings`, `Agents`, `Login`.

## Riesgo residual

Persisten inline styles en vistas legacy; no rompen, pero conviene migrarlos progresivamente a clases utilitarias en siguientes iteraciones.
