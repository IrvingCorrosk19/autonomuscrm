# AI_AGENTS_EFFECTIVENESS

## Criterio de evaluación
**No aceptar agentes decorativos.** Cada agente debe: tomar una decisión, persistir valor, o disparar acción medible.

---

## Resumen ejecutivo

| Agente | ¿Decorativo? | Valor real hoy |
|--------|--------------|----------------|
| Lead Intelligence | **No** | Score 0–100 al crear lead |
| Customer Risk | **Parcial** | Risk score inicial; no LTV/churn |
| Deal Strategy | **Parcial** | Análisis en metadata; invisible al rep |
| Communication | **Sí** | Solo logs |
| Compliance & Security | **Sí** | No recibe eventos; no bloquea |

**LLM / IA generativa:** `PlaceholderServices` — **cero** impacto en decisiones.

**Config tenant (Settings/Agents):** almacenada; **ignorada** por workers.

---

## 1. Lead Intelligence Agent

### Qué hace
Reglas fijas sobre `LeadSource` + completitud de datos → `UpdateScore` → evento `Lead.ScoreUpdated`.

### Decisiones
- Asigna número (score), no asigna rep ni prioriza cola.

### Valor
- **Medio** para priorización manual si el rep ve el score.
- **Bajo** si nadie actúa (sin tareas/alertas).

### Gap
- No usa IA ni config tenant.
- No dispara workflow por umbral.

---

## 2. Customer Risk Agent

### Qué hace
Score 0–100 al `Customer.Created` (email, teléfono, empresa).

### Decisiones
- Persiste `RiskScore`; emite `Customer.RiskScoreUpdated`.

### Valor
- **Bajo-medio** como dato inicial.
- **Decorativo** si se promete LTV/churn (no implementado).

### Gap
- `UpdateLifetimeValue` nunca llamado.
- Sin re-scoring temporal ni alertas CS.

---

## 3. Deal Strategy Agent

### Qué hace
En `Deal.Created` y `Deal.StageChanged`:
- Calcula probabilidad “mejorada” (**no escribe** en deal)
- Detecta at-risk (prob baja, días estancado, cliente riesgoso, fecha vencida)
- Escribe strings en `deal.Metadata` (sugerencias por etapa)

### Decisiones
- Texto consultivo en JSON metadata — **no accionable**.

### Valor
- **Potencial alto** si se expone y convierte en tareas.
- **Bajo hoy** — el vendedor no ve el output en flujo principal.

---

## 4. Communication Agent

### Qué hace
Log de recepción de evento.

### Valor
**Nulo** para negocio.

### Acción requerida
Implementar o **retirar de marketing** hasta tener SMTP/WhatsApp.

---

## 5. Compliance & Security Agent

### Qué hace (si recibiera eventos)
- Kill-switch: log warning, **no detiene** pipeline
- Valida CorrelationId/TenantId
- Warnings por nombre de evento sensible

### Problema técnico de negocio
Suscripción RabbitMQ a `IDomainEvent` → **no coincide** con routing keys concretas → agente **inoperante** en la práctica.

### Valor
**Nulo** para governance real.

---

## Agentes adicionales (no en lista usuario pero relevantes)

| Agente | Estado |
|--------|--------|
| DataQualityGuardian | Lógica existe; **no suscrito** |
| AutomationOptimizer | TODO; **no suscrito** |

---

## Matriz decisión → valor

```
Evento ──► Agente ──► ¿Persiste dato? ──► ¿Acción visible? ──► ¿Ingresos?
Lead.Created ──► Lead Intel ──► Sí (score) ──► Parcial ──► Indirecto
Customer.Created ──► Risk ──► Sí (risk) ──► Parcial ──► Indirecto
Deal.StageChanged ──► Strategy ──► Sí (metadata) ──► No ──► No
Lead.Created ──► Communication ──► No ──► No ──► No
```

---

## Recomendaciones

1. **P0:** Deal Strategy → crear `WorkflowTask` + notificación cuando `isAtRisk`.
2. **P0:** Conectar config tenant a umbrales de agentes.
3. **P1:** Compliance en routing keys reales + kill-switch bloqueante.
4. **P1:** Communication con 3 plantillas (welcome, follow-up, renewal).
5. **P2:** LLM opcional para redacción, **no** para scoring base (reglas auditables primero).
