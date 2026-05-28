# BUSINESS_GAPS_REPORT

## Resumen
**47 gaps de negocio** identificados, agrupados por área. Prioridad P0 = bloquea valor comercial; P1 = limita escala; P2 = optimización.

---

## A. CRM Core & flujo comercial (P0–P1)

| # | Gap | P |
|---|-----|---|
| 1 | Sin entidad Opportunity entre Lead y Deal | P1 |
| 2 | Conversión Lead→Customer sin Application command | P0 |
| 3 | Sin vínculo automático Lead→Deal al calificar | P0 |
| 4 | `Deal.Lose()` sin flujo ni motivo pérdida | P0 |
| 5 | Sin máquina de estados (transiciones inválidas posibles) | P1 |
| 6 | Bulk updates sin eventos de dominio | P0 |
| 7 | `GetDealById` query/API incompleto | P1 |
| 8 | Renewal / Subscription inexistente | P1 |
| 9 | Upsell como deal hijo no modelado | P1 |
| 10 | Referidos no modelados | P2 |

---

## B. Pipeline & forecast (P0–P1)

| # | Gap | P |
|---|-----|---|
| 11 | Forecast 60/90 mock en UI | P0 |
| 12 | EstimatedRevenue Index no ponderado | P1 |
| 13 | Win rate / cycle time no calculados | P1 |
| 14 | Cuotas por rep inexistentes | P1 |
| 15 | DealStrategy output no en UI operativa | P0 |
| 16 | Simulación escenarios deshabilitada | P2 |

---

## C. Tareas & seguimiento (P0)

| # | Gap | P |
|---|-----|---|
| 17 | Sin módulo tareas (list/complete) | P0 |
| 18 | Sin DueDate / prioridad | P0 |
| 19 | Sin SLA “sin seguimiento” | P0 |
| 20 | Sin cadencias comerciales | P1 |
| 21 | DataQuality no crea tareas corrección | P1 |

---

## D. Customer Success (P0–P1)

| # | Gap | P |
|---|-----|---|
| 22 | Sin playbook post-ClosedWon | P0 |
| 23 | LTV nunca calculado automáticamente | P0 |
| 24 | Churn solo manual | P0 |
| 25 | Health score inexistente | P1 |
| 26 | Paneles CS con datos ficticios | P0 |
| 27 | Sin entidad contrato/renovación | P1 |

---

## E. Automatización (P0–P1)

| # | Gap | P |
|---|-----|---|
| 28 | UI workflows > motor ejecutable | P0 |
| 29 | Communicate / ActivateAgent stub | P0 |
| 30 | Policy engine no evalúa | P1 |
| 31 | Decision engine no conectado | P1 |
| 32 | Pocos eventos con agentes | P1 |
| 33 | Config agentes ignorada | P0 |

---

## F. Comunicaciones (P0)

| # | Gap | P |
|---|-----|---|
| 34 | Sin email real | P0 |
| 35 | Sin WhatsApp | P1 |
| 36 | Sin notificaciones internas | P0 |
| 37 | Sin historial comunicación | P1 |

---

## G. IA & agentes (P0–P2)

| # | Gap | P |
|---|-----|---|
| 38 | Communication agent decorativo | P0 |
| 39 | Compliance agent inoperante | P0 |
| 40 | LLM placeholder | P2 |
| 41 | Optimizer / DataQuality no suscritos | P1 |

---

## H. Reporting (P0–P1)

| # | Gap | P |
|---|-----|---|
| 42 | Dashboard.cshtml mock confunde | P0 |
| 43 | TimeSeries sin datos | P1 |
| 44 | Sin reporte CEO único | P0 |
| 45 | Sin export PDF ejecutivo | P2 |
| 46 | NRR/churn/ARR ausentes | P0 |
| 47 | Productividad por rep ausente | P1 |

---

## Impacto por área de negocio

| Área | Gaps P0 | Riesgo ingresos |
|------|---------|-----------------|
| Ventas | 12 | Alto |
| CS/Retención | 6 | Alto |
| Automatización | 5 | Medio-alto |
| Comunicación | 3 | Alto |
| Dirección | 4 | Alto |
