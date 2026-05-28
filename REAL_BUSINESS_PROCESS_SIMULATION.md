# REAL_BUSINESS_PROCESS_SIMULATION

## Metodología
Simulaciones de **procesos de negocio completos** (no unit tests), ejecutadas contra capacidades reales del producto documentadas en código.

---

## Simulación 1 — Vendedor (día típico)

### Escenario
María recibe 10 leads del sitio web. Debe calificar, llamar, crear oportunidades y cerrar 2 deals este mes.

### Pasos simulados

| Paso | Acción | Resultado sistema |
|------|--------|-------------------|
| 1 | Leads aparecen en listado | ✓ |
| 2 | Score automático al crear | ✓ Lead Intelligence |
| 3 | María califica 6 leads | ✓ Qualify / status |
| 4 | Sistema recuerda llamar en 24h | **✗** sin tarea |
| 5 | María crea deals manualmente | ✓ pero lento |
| 6 | Email de bienvenida al lead | **✗** Communication stub |
| 7 | Ve sugerencias IA en deal | **✗** metadata oculta |
| 8 | Mueve etapas, cierra 1 deal | ✓ Close |
| 9 | Sistema alerta deals estancados | **✗** |
| 10 | María ve sus tareas pendientes | **✗** sin módulo |

### Resultado
María **usa el CRM como registro** y WhatsApp/email personal para vender. **Productividad del CRM: 40%.**

---

## Simulación 2 — Gerente comercial (revisión semanal)

### Escenario
Carlos revisa pipeline, forecast y equipo.

| Paso | Resultado |
|------|-----------|
| Abre Index | KPIs reales ✓ |
| Revisa pipeline por etapa | ✓ |
| Pregunta “¿cerramos 90d?” | Panel mock **✗** |
| Asigna leads en bulk a rep | ✓ pero **sin eventos** |
| Ve win/loss por motivo | **✗** Lose no existe |
| Confía en forecast para comité | Solo 30d real |

### Resultado
Carlos lleva **Excel paralelo** para comité de dirección.

---

## Simulación 3 — Customer Success (cliente recién ganado)

### Escenario
Ana recibe cuenta cerrada ayer. Debe onboarding 30 días.

| Paso | Resultado |
|------|-----------|
| Notificación deal cerrado | **✗** |
| Cola onboarding | **✗** |
| Cliente con risk score | ✓ al crear |
| Health / adopción | **✗** |
| Email bienvenida | **✗** |
| Tareas día 7 / 30 | **✗** |
| Alerta churn mes 4 | **✗** |

### Resultado
Ana trabaja en **spreadsheet + email**; CRM no guía post-venta.

---

## Simulación 4 — Soporte

### Escenario
Ticket de cliente existente — impacto en CRM.

| Paso | Resultado |
|------|-----------|
| Ver historial comunicaciones | **✗** |
| Ver health cliente | Solo risk estático |
| Escalar a CS por riesgo | Manual |

**Integración soporte:** no existe.

---

## Simulación 5 — Administrador

### Escenario
Configura workflows y agentes para el tenant.

| Paso | Resultado |
|------|-----------|
| Crea workflow Lead.Created → Assign | Parcial (parámetros UI incompletos) |
| Activa Communication agent | **Sin efecto** |
| Desactiva agente en Settings | **Sin efecto** en worker |
| Revisa audit | ✓ |
| Kill-switch compliance | **No bloquea** |

---

## Simulación 6 — CEO (decisión trimestral)

### Preguntas CEO

| Pregunta | ¿Responde el CRM? |
|----------|-------------------|
| ¿Crecimos ingresos? | Parcial (deals abiertos, no ARR) |
| ¿Churn subió? | **No** |
| ¿Dónde invertir en ventas? | **No** (sin productivity) |
| ¿ROI del CRM? | **No medible** |

---

## Resumen de simulaciones

| Rol | Proceso completable en CRM | Dependencia externa |
|-----|---------------------------|-------------------|
| Vendedor | 40% | Alta |
| Gerente | 50% | Media-alta |
| Customer Success | 15% | Muy alta |
| Soporte | 10% | Total |
| Administrador | 55% | Media |
| CEO | 20% | Muy alta |

---

## Criterio de éxito enterprise

Proceso **completo** = inicio en CRM, automatización, acción visible, métrica, cierre en CRM.

**Ninguna simulación alcanza 80%** hoy.
