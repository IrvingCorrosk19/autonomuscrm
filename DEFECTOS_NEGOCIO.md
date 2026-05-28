# DEFECTOS_NEGOCIO

## Definición
**Defecto de negocio:** comportamiento del producto que rompe o falsea un proceso comercial, genera decisión incorrecta, o promete capacidad no entregada.

No incluye defectos visuales/CSS.

---

## Críticos (bloquean confianza enterprise)

| ID | Defecto | Impacto |
|----|---------|---------|
| BN-01 | CommunicationAgent anunciado como activo pero **no envía nada** | Cliente sin follow-up; promesa falsa |
| BN-02 | Forecast 90d **hardcoded** junto a datos reales | CEO decide con números falsos |
| BN-03 | `Dashboard.cshtml` KPIs inventados bajo marca operativa | Riesgo reputacional en demo→producción |
| BN-04 | Bulk update **no dispara** automatización | Gerente cree que automatizó; no ocurrió |
| BN-05 | Config agentes en Settings **no afecta** workers | Admin pierde control tenant |
| BN-06 | Compliance/kill-switch **no bloquea** operaciones | Riesgo governance enterprise |
| BN-07 | CustomerRisk promete LTV/churn; **solo score inicial** | CS confía en dato incompleto |
| BN-08 | DealStrategy escribe ayuda en metadata **invisible** | Inversión IA sin retorno campo |

---

## Altos (degradan proceso comercial)

| ID | Defecto | Impacto |
|----|---------|---------|
| BN-09 | Sin consecuencia si vendedor no hace seguimiento | Pipeline se pudre |
| BN-10 | `Deal.Lose` inexistente en operación | No hay análisis pérdidas |
| BN-11 | Lead→Deal desconectado | Fugas en embudo |
| BN-12 | Workflows UI configuran triggers **no implementados** | Admin pierde tiempo |
| BN-13 | Parámetros Assign/CreateTask **no capturados** en UI edición | Workflows fallan silenciosamente |
| BN-14 | ClosedWon sin playbook CS | Churn temprano post-venta |
| BN-15 | Paneles Customers/Agents con alertas **ficticias** | Decisiones CS erróneas |
| BN-16 | EstimatedRevenue Index **no ponderado** | Pipeline inflado |
| BN-17 | Policy evaluation **siempre true** | Compliance teatro |

---

## Medios

| ID | Defecto | Impacto |
|----|---------|---------|
| BN-18 | Sin GetDeal API completo | Integraciones rotas |
| BN-19 | ConvertLead solo en Razor handler | No reutilizable/multi-canal |
| BN-20 | TimeSeries API vacía | Reportes tendencia imposibles |
| BN-21 | DataQuality/Optimizer no suscritos | Calidad datos no remediada |
| BN-22 | Transiciones estado sin validación | Datos incoherentes reporting |
| BN-23 | OnHold/Cancelled deal sin operación clara | Pipeline sucio |

---

## Severidad por rol

| Rol | Defectos que más le afectan |
|-----|----------------------------|
| CEO | BN-02, BN-03, BN-16 |
| Gerente | BN-04, BN-10, BN-11 |
| Vendedor | BN-08, BN-09, BN-13 |
| CS | BN-01, BN-07, BN-14, BN-15 |
| Admin | BN-05, BN-06, BN-12, BN-17 |

---

## Acción inmediata recomendada (sin esperar fase completa)

1. Etiquetar `/Dashboard` y paneles mock como **DEMO** en UI.
2. Documentación comercial alineada a capacidades reales (este análisis).
3. Desactivar marketing de “IA comunicación” hasta Fase 13.
