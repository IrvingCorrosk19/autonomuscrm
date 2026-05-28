# PIPELINE_BUSINESS_ANALYSIS

## Pregunta de negocio
**¿Puede un gerente comercial gestionar ventas reales con AutonomusFlow hoy?**

**Respuesta: Parcialmente sí** para visibilidad y movimiento de etapas; **no** para forecast confiable ni gestión de equipo a escala enterprise.

---

## Pipeline actual (modelo)

### Etapas (`DealStage`)
| Etapa | Probabilidad default | Uso comercial |
|-------|---------------------|---------------|
| Prospecting | 10% | Prospección inicial |
| Qualification | 25% | Calificación BANT/light |
| Proposal | 50% | Propuesta enviada |
| Negotiation | 75% | Negociación activa |
| ClosedWon | 100% | Ganado |
| ClosedLost | 0% | Perdido |

### Estados (`DealStatus`)
Open, Closed, OnHold, Cancelled — **OnHold/Cancelled sin métodos de dominio dedicados**.

---

## Lo que funciona para un gerente

1. **Kanban/listado de deals** con filtros (`Deals.cshtml`).
2. **Montos por etapa** en dashboard (`Index`) y totales en Deals.
3. **Probabilidad** manual y automática al cambiar etapa.
4. **Forecast 30 días ponderado** en Deals: `Σ (amount × probability)` para deals con `ExpectedCloseDate` ≤ 30d.
5. **Deals en riesgo** en Index: `probability < 50`.
6. **DealStrategyAgent** calcula riesgo y escribe sugerencias en `Metadata` (no expuestas en UI principal).

---

## Lo que NO funciona para un gerente

| Necesidad gerencial | Estado |
|---------------------|--------|
| Forecast 60/90 días confiable | UI **mock** ($310K/$185K/$142K hardcoded) |
| Win rate por etapa / período | No calculado |
| Velocidad de pipeline (cycle time) | No |
| Cobertura de cuota por rep | No (sin objetivos de cuota) |
| Comparativa período anterior | No time-series poblado |
| Simulación de escenarios | Botón deshabilitado |
| Pérdidas analizadas (`Deal.Lose`) | Sin flujo |
| Coaching visible al equipo | Metadata del agente oculta |
| Pipeline por producto/línea | No en dominio |

---

## Flujo comercial validado

```
Lead (captura) ──► Calificación manual/workflow ──► ?
                      │
                      ├──► ConvertToCustomer (página) ──► Customer
                      └──► Deal manual (CreateDeal) ──► Pipeline ──► Close
```

**Fuga:** No hay **oportunidad estándar** que vincule lead calificado → deal automáticamente. El gerente no ve un embudo único Lead→Opp→Deal.

---

## Métricas de pipeline — real vs decorativo

| Métrica | Fuente | ¿Real? |
|---------|--------|--------|
| Pipeline por etapa (montos) | Index.cshtml.cs | Sí |
| Ingresos estimados (suma abierta) | Index | Sí (no ponderado) |
| Forecast 30d ponderado | Deals.cshtml | Sí |
| Forecast 30/60/90 panel IA | Deals sidebar | **No** |
| Recomendaciones estrategia | DealStrategyAgent | Sí en DB, **no en UI** |

---

## Recomendaciones de negocio (sin UI-first)

1. **P0:** Exponer sugerencias de `DealStrategyAgent` y deals at-risk en bandeja de trabajo del vendedor.
2. **P0:** Comando `LoseDeal` + motivo de pérdida (para win/loss analysis).
3. **P1:** Forecast engine único (30/60/90) desde datos, eliminar mocks.
4. **P1:** Evento `Deal.Closed` → workflow CS (onboarding cliente).
5. **P2:** Cuotas y pipeline por owner para gerentes.

---

## Decisión gerencial simulada

| Escenario | ¿Puede decidir? |
|-----------|-----------------|
| “¿Cuánto cerramos este mes?” | Aproximado (30d ponderado), no 90d |
| “¿Qué deals salvamos?” | Parcial (lista riesgo por prob.) |
| “¿Por qué perdimos Q3?” | **No** (sin Lose ni motivos) |
| “¿El equipo cumple cuota?” | **No** |
