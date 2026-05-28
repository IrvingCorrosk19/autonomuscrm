# DARK_MODE_ACTIVATION_FINAL_PREP

## Estado
**Dark-mode activation ready** — sin activar flag en producción.

## Preparación Fase 11
Selectores `html[data-crm-theme="dark"]` añadidos para:
- `.crm-runtime-bar`
- `.crm-runtime-links .btn.active`
- `.crm-ops-bar-card.crm-sticky-runtime`

## Activación futura (una línea de gobierno)
```html
<html data-crm-theme="dark">
```
o toggle JS que persista preferencia — sin tocar AdminLTE core.

## Superficies a validar al activar
- [ ] Dashboard KPI cards
- [ ] Overlays y modales `crm-overlay-modal`
- [ ] Tablas compact/comfortable
- [ ] Toasts y alerts
- [ ] Runtime + ops sticky bars
- [ ] Login (`crm-login-*`)

## Base
DARK_MODE_OPERATIONAL_VALIDATION, DARK_MODE_READY_FINAL.
