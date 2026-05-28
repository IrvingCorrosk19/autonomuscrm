# RESPONSIVE_ISSUES_REPORT

## Problemas detectados

- Tablas con columnas extensas en `Leads`, `Customers`, `Deals`, `Users`.
- Densidad alta en mobile (headers y acciones apretadas).
- `stats` legacy podían quebrar en anchos pequeños.
- Modales personalizados sin patrón único de scroll en dispositivos bajos.
- Botones de acciones en headers se montaban en breakpoints intermedios.

## Severidad

- Alta: tablas y formularios en móvil/tablet.
- Media: spacing/jerarquía en cards y topbars.
- Baja: microinconsistencias de badges/chips.

## Correcciones aplicadas

- Tabla responsive automática y labels móviles (`data-label`) en JS.
- Stack responsive para acciones de header.
- Grid `stats` adaptativo (2 columnas tablet, 1 en móvil).
- `modal-dialog-scrollable` y límites de ancho/alto coherentes.
- Gutter global ajustado para desktop/mobile.
