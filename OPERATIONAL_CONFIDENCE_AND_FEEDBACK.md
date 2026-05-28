# OPERATIONAL_CONFIDENCE_AND_FEEDBACK

## Sistema consolidado
- **Toast** — enter/leave, `aria-live`, acción Reintentar opcional.
- **trackOperation** — estados Procesando → Completado / Error operativo.
- **Flash** — `_FlashMessages` en layout.
- **Skeleton** — `crmUi.setLoading` + partial `_CrmLoadingSkeleton`.

## Predictabilidad runtime
Operaciones async no bloquean UI; errores recuperables muestran CTA retry cuando `onRetry` está definido.

## Transparencia
Mensajes en español operacional (“en progreso”, “completada”, “falló”).

## No introducido
Colas de operaciones offline, websockets de progreso — fuera de alcance incremental.

## Checklist confianza
- [ ] Error de red muestra toast + Reintentar
- [ ] Operación exitosa no duplica toasts excesivos
- [ ] `aria-busy` en skeleton targets
