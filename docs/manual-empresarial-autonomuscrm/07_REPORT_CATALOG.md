# 07 — Catálogo de Reportes y Analítica

Solo métricas y dashboards **implementados en código**.

---

## 1. Command (`/` — Index)

**Servicio:** `IAiCommandCenterService.GetFlowCommandAsync`  
**Métricas típicas:** revenue generated/protected, cuentas en riesgo, expansiones, renovaciones, decisiones en vivo, snapshot workforce  
**Periodo:** 7 o 30 días (query param)

**Uso:** Priorización diaria del ejecutivo/vendedor senior.

---

## 2. Revenue OS (`/revenue`)

**Servicio:** `IRevenueOsService.GetDashboardAsync`  
**Plus:** `IGraphReasoningEngine.DetectRevenueLeakAsync` — explicación de fugas de ingreso

**Interpretación:** Identificar dónde se pierde pipeline (deals estancados, leads inactivos).

---

## 3. Executive OS (`/executive`)

**Servicio:** `IExecutiveOsService.GetDashboardAsync`  
**Export:** HTML board/executive (`?handler=Export`)

**Rol:** Admin, Manager (home redirect).

---

## 4. Pipeline / Deals (`/Deals`)

**Agregados SQL:** `DealRepository.GetListSummaryAsync`

| Métrica | Significado |
|---------|-------------|
| Forecast 30/60/90 | Suma ponderada (Amount × Probability) de deals abiertos por ventana de cierre |
| Win Rate | Won / (Won + Lost) |
| Revenue Closed | Suma Amount deals ClosedWon |
| Pipeline Open | Suma Amount deals Open (vista kanban) |

---

## 5. Leads (`/Leads`)

**Agregados:** `LeadRepository.GetListSummaryAsync`, `GetSourceStatsAsync`

| Métrica | Significado |
|---------|-------------|
| TotalCount | Leads filtrados |
| QualifiedCount | Calificados |
| NewCount | Estado New |
| HighScoreCount | Score > 70 |
| AvgScore | Promedio scores |
| SourceStats | Distribución por `LeadSource` |

---

## 6. Customers (`/Customers`)

**Agregados:** `CustomerRepository.GetListSummaryAsync`

| Métrica | Significado |
|---------|-------------|
| TotalCount | Clientes tenant |
| AvgLtv | Lifetime Value promedio |
| HighLtvCount | LTV > 10,000 |
| HighRiskCount | RiskScore > 70 |
| AvgRisk / LowRiskCount | Riesgo promedio y bajo riesgo |

---

## 7. Tasks (`/Tasks`)

**Repositorio:** conteos `CountByTenantAsync`, `CountOverdueOpenAsync`  
**Filtros:** status, assignee, overdue, priority

---

## 8. Trust Studio (`/TrustInbox`)

Cola de aprobaciones IA, métricas SLA, severidad, umbrales.

---

## 9. Agents / Decisions / Outcomes / Playbooks

| Página | Servicio |
|--------|----------|
| `/Agents` | Command center recent decisions |
| `/command/decisions` | Historial filtrable |
| `/command/outcomes` | Outcome Fabric summary |
| `/command/playbooks` | Estados playbook autónomo |

---

## 10. Customer Success (`/customer-success`)

Tickets, casos, playbooks CS — datos de `CustomerSuccess` page model.

---

## 11. Memory (`/Memory`)

`ISemanticMemoryService.GetDashboardAsync` — timeline memoria, estado embedding provider.

---

## 12. Integrations (`/Integrations`)

`IntegrationHealthDashboardDto` — salud conexiones OAuth/sync.

---

## 13. Billing (`/billing`)

`IBillingDashboardService` — suscripción tenant.

---

## 14. Audit (`/Audit`)

Event store paginado, conteos por tipo, export JSON hasta 10,000 eventos.

---

## 15. API analítica (para integraciones)

| Endpoint área | Servicio |
|---------------|----------|
| `api/ai/dashboard` | Executive AI dashboard |
| `api/ai/analytics` | Executive AI analytics |
| `api/ai/predictions` | Predictive revenue |
| `api/ai/ml/churn` | Churn ML predictions |
| `api/ai/ml/expansion` | Expansion ML |
| `api/ai/ml/revenue` | Revenue ML forecast |
| `api/ai/evaluation` | Model metrics |
| `api/ai/governance` | AI governance report |

---

## Cómo interpretar (Sales)

1. **Revenue OS** → qué hacer hoy para proteger ingresos  
2. **Leads métricas** → calidad del embudo superior  
3. **Deals forecast** → compromiso con dirección  
4. **Tasks overdue** → riesgo de SLA y churn  
5. **No confundir** métricas de página actual (paginación 50) con totales tenant — revisar cards de resumen
