# MOBILE_UX_VALIDATION

## Validación objetivo

- Desktop/laptop: estructura intacta, tablas completas.
- Tablet: headers y acciones wrap sin colisiones.
- Mobile: tablas convertidas a bloques con etiquetas por columna.

## Cobertura funcional revisada

- Login, Dashboard, Leads, Customers, Deals, Users, Workflows, Policies, Audit, Settings, Agents.

## Ajustes clave mobile

- `.crm-page-actions` full-width en breakpoints bajos.
- Gutter reducido en móvil para evitar clipping.
- `stats` de 1 columna en pantallas pequeñas.
- Pipeline cards con ancho relativo al viewport.
- Modales scrollables para evitar corte de formularios.

## Estado

GO técnico para mobile UX base enterprise, sin cambios de lógica.
