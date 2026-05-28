# MEJORAS_PRIORIZADAS

## Matriz de priorización
**Score = Valor negocio (1–5) × Facilidad inversa (1–5)** — ordenado por impacto comercial inmediato.

---

## P0 — Implementar primero (Fase 12 sprint 1–2)

| Rank | Mejora | Valor | Facilidad | Área |
|------|--------|-------|-----------|------|
| 1 | Módulo Tareas + DueDate + bandeja vendedor | 5 | 4 | Seguimiento |
| 2 | Deal at-risk → WorkflowTask + notificación | 5 | 4 | Pipeline |
| 3 | Bulk ops emiten domain events | 5 | 5 | Automatización |
| 4 | Exponer DealStrategy metadata en UI operativa | 4 | 5 | Pipeline |
| 5 | Comando LoseDeal + motivo pérdida | 4 | 4 | Pipeline |
| 6 | ConvertLead Application command + API | 4 | 4 | CRM Core |
| 7 | Lead.Qualified → auto CreateDeal draft + tarea | 5 | 3 | CRM Core |
| 8 | Marcar/retirar KPIs mock (Dashboard, forecast 90) | 5 | 5 | Reporting |
| 9 | Workflow UI captura parámetros de acciones | 4 | 3 | Automatización |
| 10 | Aplicar config tenant en agentes (on/off, umbrales) | 4 | 4 | IA |

---

## P1 — Fase 12 sprint 3–4 / Fase 13 inicio

| Rank | Mejora | Valor | Facilidad |
|------|--------|-------|-----------|
| 11 | Email SMTP mínimo (3 plantillas) | 5 | 3 |
| 12 | Deal.ClosedWon → playbook CS (tareas) | 5 | 4 |
| 13 | PurchaseRecorded → UpdateLifetimeValue | 4 | 4 |
| 14 | Risk re-score job + alerta CS > 70 | 4 | 3 |
| 15 | Forecast engine 30/60/90 unificado (datos reales) | 4 | 3 |
| 16 | Compliance routing fix + kill-switch bloqueante | 4 | 3 |
| 17 | Panel CS solo datos DB (quitar mocks) | 4 | 5 |
| 18 | GetDealById query + API completo | 3 | 5 |
| 19 | Evento Deal.Closed → suscriptores CS | 4 | 4 |
| 20 | Notificaciones in-app básicas | 4 | 3 |

---

## P2 — Fase 13–14

| Rank | Mejora | Valor | Facilidad |
|------|--------|-------|-----------|
| 21 | Entidad Contract/Renewal | 5 | 2 |
| 22 | WhatsApp Business API | 4 | 2 |
| 23 | Health score compuesto | 4 | 2 |
| 24 | Cuotas + coverage pipeline | 4 | 3 |
| 25 | TimeSeries desde eventos | 3 | 3 |
| 26 | Executive dashboard (ARR, NRR, churn) | 5 | 2 |
| 27 | LLM redacción asistida (opcional) | 3 | 2 |
| 28 | Cadencias comerciales predefinidas | 4 | 2 |
| 29 | Integración soporte/tickets | 4 | 2 |
| 30 | Report PDF programado | 3 | 3 |

---

## Quick wins (< 1 semana cada uno)

1. Etiqueta DEMO en `/Dashboard` y sidebars ficticios.
2. Documentación Agents alineada (sin LTV/churn hasta implementar).
3. `Index.EstimatedRevenue` ponderado por probabilidad.
4. Suscribir DataQualityGuardian a schedule diario.
5. Fix Compliance routing keys concretas.

---

## NO priorizar (explícito)

- Refinamiento UI / CSS / dark mode
- Nuevos componentes visuales
- Más agentes decorativos
- Triggers Schedule/Webhook antes de cerrar P0

---

## ROI esperado por bloque P0

| Bloque | ROI estimado |
|--------|--------------|
| Tareas + at-risk | +15–25% actividad en pipeline |
| Eventos bulk + workflows | +30% efectividad automatización |
| Quitar mocks | Evita decisiones erróneas (incalculable) |
| Lead→Deal | +10% conversión lead-opportunity |
