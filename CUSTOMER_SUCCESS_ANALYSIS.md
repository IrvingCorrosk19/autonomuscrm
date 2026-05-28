# CUSTOMER_SUCCESS_ANALYSIS

## Pregunta de negocio
**¿Qué sucede después de cerrar una venta?**

**Respuesta hoy: El deal pasa a ClosedWon y el cliente existe en base de datos. No hay playbook automático de CS, onboarding operativo ni renovación.**

---

## Capacidades CS en código

| Área | Estado | Realidad |
|------|--------|----------|
| Onboarding UI | Existe (cards dismissibles) | **Educativo**, no operacional |
| Health score | No | Campo `RiskScore` ≠ health |
| Adopción producto | No | Sin métricas de uso |
| Churn | Enum `Churned` | **Manual**, sin predicción |
| Renovación | No en dominio | Solo copy en UI demo |
| Satisfacción (NPS/CSAT) | No | Mock en Index colapsado |
| LTV | Campo `LifetimeValue` | **Nunca calculado** por agentes |
| Post-close workflow | No | `Deal.Closed` sin suscriptores |

---

## Customer Risk Agent — expectativa vs realidad

**Documentación/UI sugiere:** LTV, churn, alertas.  
**Código:** score heurístico único al `Customer.Created` (email/teléfono/empresa).

No hay:
- Re-evaluación periódica de riesgo
- Alerta a CS cuando `RiskScore > 70`
- Acción automática (tarea, email, escalamiento)

---

## Journey post-venta actual

```
Deal.ClosedWon ──► Customer (si ya existía o se creó aparte)
       │
       └──► (vacío) ──► sin onboarding CS
                    └──► sin check-in 30/60/90
                    └──► sin renovación
                    └──► sin upsell oportunidad
```

---

## Fugas y abandonos detectados

| Punto | Fuga |
|-------|------|
| Cierre → activación | Cliente no recibe comunicación (CommunicationAgent stub) |
| Mes 1–3 uso | Sin health monitoring |
| Pre-renovación | Sin entidad contrato ni alerta 90d antes |
| Churn silencioso | Status manual; sin señales de inactividad |
| Upsell | Solo texto UI ficticio |

---

## Qué necesita un Customer Success Manager

1. **Cola de cuentas en riesgo** (datos reales, no sidebar mock).
2. **Playbook post-cierre:** tareas día 1, 7, 30.
3. **Health score compuesto:** uso + pagos + tickets + NPS.
4. **Renovación:** fecha fin contrato + deal de renovación automático.
5. **Expansión:** oportunidad upsell ligada a LTV y uso.

---

## Recomendaciones priorizadas

| P | Acción |
|---|--------|
| P0 | Evento `Deal.Closed` (Won) → workflow CreateTask “Onboarding CS” |
| P0 | Panel CS con clientes `RiskScore > 70` (datos reales, quitar mocks) |
| P1 | Job periódico: recalcular risk + flag inactividad |
| P1 | Entidad `Contract` o `Subscription` con `RenewalDate` |
| P2 | Health score formula + integración soporte/tickets |

---

## Valor si se cierra el gap

Retención B2B SaaS: reducir churn 5 puntos = **+25–40% LTV** en negocios subscription (McKinsey SaaS benchmarks). AutonomusFlow hoy **no puede medir ni actuar** sobre ese lever.
