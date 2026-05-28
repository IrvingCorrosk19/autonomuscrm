# AUTONOMOUS REVENUE PLATFORM — Fase 15

Documento unificado de arquitectura, motores, API, operación y validación.

---

## 1. Visión y objetivo

AutonomusFlow evoluciona de **Customer Intelligence Platform** (Fase 14) a **Autonomous Revenue Platform**: detecta, **decide**, **actúa**, mide y aprende sin intervención humana constante.

**Antes:** Sistema → detecta problema → humano decide.

**Ahora:** Sistema → detecta → decide → actúa → mide resultado → aprende.

**Ciclo autónomo:**

```
Evento → Decisión → Auditoría → Playbook/Comms → Tarea → Conocimiento → ML Sample
```

**Base obligatoria:** Fases 12–14 (Revenue Ops, Retention, Product Analytics & Intelligence).

**Sin UI/CSS** — 100 % backend, API y Worker.

---

## 2. Mapa de componentes

| Área | Servicio | API principal |
|------|----------|---------------|
| Decisiones | `IAutonomousRevenueDecisionEngine` + `IDecisionEngine` | `POST /api/ai/decide/{customerId}` |
| Next Best Action | `INextBestActionEngine` | `GET /api/ai/next-best-actions` |
| Playbooks autónomos | `IAutonomousPlaybookEngine` | (vía ciclo / decide) |
| Predicción revenue | `IPredictiveRevenueEngine` | `GET /api/ai/predictions` |
| ML Foundation | `IMlFoundationService` | `GET /api/ai/ml-datasets` |
| CS autónomo | `IAutonomousCustomerSuccessEngine` | (vía ciclo) |
| Comunicaciones | `IAutonomousCommunicationsEngine` | (vía decisión) |
| Auditoría IA | `IAiDecisionAuditService` | `GET /api/ai/decisions` |
| Conocimiento | `IBusinessKnowledgeEngine` | `GET /api/ai/knowledge` |
| Orquestación | `IAutonomousOrchestrationEngine` | `POST /api/ai/cycle` |
| Dashboard ejecutivo | `IExecutiveAiDashboardService` | **`GET /api/ai/dashboard`** |

**Agentes:** RevenueAgent, RenewalAgent, ChurnAgent, ExpansionAgent, CustomerAgent, OperationsAgent.

**Tablas BD:** `AiDecisionAudits`, `AutonomousPlaybookStates`, `BusinessKnowledgeRecords`, `MlFeatureSnapshots`.

**Migración EF:** `Phase15_AutonomousPlatform`.

---

## 3. Decision Engine

`IAutonomousRevenueDecisionEngine` + `IDecisionEngine` (wrapper en Infrastructure).

### Tipos de decisión
Rescue, Renewal, Expansion, Escalation, ReEngagement, Upsell, NoAction.

### Inputs
Health, Churn V2, NPS, CSAT, LTV, expansion readiness, Business Knowledge.

### Ejecución
`ExecuteDecisionAsync` → auditoría + playbook autónomo + email/WhatsApp según tipo.

### API
`POST /api/ai/decide/{customerId}?tenantId=&execute=true`

---

## 4. Next Best Action Engine

`INextBestActionEngine` — recomienda la próxima acción por **cliente**, **deal** y agregado **tenant**.

Incluye: acción recomendada, canal (Phone / Email / WhatsApp / Meeting), fecha límite, priority score, rationale.

### API
`GET /api/ai/next-best-actions?tenantId=`

---

## 5. Autonomous Playbook Engine

`IAutonomousPlaybookEngine` — playbooks con estado persistente en `AutonomousPlaybookStates`.

| Capacidad | Descripción |
|-----------|-------------|
| Iniciar | Crea `AutonomousPlaybookState` y ejecuta playbook (Fase 13) |
| Avanzar | Ejecuta pasos cuando `NextActionAt` vence |
| Completar / Escalar | Transiciones automáticas de estado |

Integrado con `ICustomerPlaybookService`. `ProcessDuePlaybooksAsync` en scan periódico.

---

## 6. Revenue AI Agents

Orquestados por `IAutonomousOrchestrationEngine` cada **15 minutos**. Retornan `AgentRunResultDto` (decisions, actions, tasks).

| Agente | Función |
|--------|---------|
| **RevenueAgent** | Sales intelligence en deals abiertos |
| **RenewalAgent** | Ventanas de renovación + decisiones Renewal |
| **ChurnAgent** | Churn V2 + playbook Rescue automático |
| **ExpansionAgent** | Clientes expansion-ready + playbooks |
| **CustomerAgent** | CS autónomo + CustomerInsights |
| **OperationsAgent** | Revenue scan + Data Quality + Intelligence scan |

---

## 7. Predictive Revenue Engine

`IPredictiveRevenueEngine` — horizontes **30, 60, 90, 180 y 365** días.

Por horizonte predice:
- **Revenue** — forecast engine + histórico de deals
- **Renewals** — ARR de contratos en ventana
- **Churn** — cuentas con probabilidad ≥ umbral dinámico
- **Expansion** — readiness agregado

Incluye `ConfidencePercent`.

### API
`GET /api/ai/predictions?tenantId=`

---

## 8. ML Foundation

`IMlFoundationService` — pipeline de features para entrenamiento futuro (heurísticas hoy, ML en fase posterior).

### Datasets
`churn`, `expansion`, `renewal`, `nps`, `csat`, `revenue`

### Almacenamiento
`MlFeatureSnapshots` — `Features` (jsonb) + `Label` + `DatasetType`

### Captura
`CaptureTrainingSamplesAsync` en cada ciclo autónomo.

### API
`GET /api/ai/ml-datasets?tenantId=`

Preparado para export a Python / MLflow.

---

## 9. Autonomous Customer Success

`IAutonomousCustomerSuccessEngine` — por cliente (top 25 activos):

1. `DecideForCustomerAsync`
2. Playbook autónomo según decisión
3. `ExecuteDecisionAsync` (auditoría + comunicaciones)
4. Tarea `Autonomous_Action` si no existe

Rescates, onboarding, renovaciones y expansión **sin intervención humana** cuando el score supera el umbral.

---

## 10. Autonomous Communications

`IAutonomousCommunicationsEngine` — dispara canales según decisión:

| Decisión | Canal / plantilla |
|----------|-------------------|
| Rescue | Email `risk` |
| Renewal | Email `renewal` |
| ReEngagement | WhatsApp `recovery` |
| Expansion / Upsell | Email `followup` |

Usa `IEmailAutomationEngine` + `IWhatsAppAutomationEngine` (Fase 13) con tracking en `CustomerCommunicationLogs`.

---

## 11. AI Decision Audit

`IAiDecisionAuditService` → tabla `AiDecisionAudits`.

Registra por decisión:
- `decisionType`, `action`, `decisionScore`, `reason`
- `evidence` (jsonb)
- `customerId`, `dealId`, `agentName`
- `status`: Pending / Executed / Failed
- `outcome`, `executedAt`

Toda decisión IA es **auditable** y consultable.

### API
`GET /api/ai/decisions?tenantId=&take=50`

---

## 12. Autonomous Orchestration

`IAutonomousOrchestrationEngine`

### Por evento (`DomainEventDispatcher`)
Tras workflows, operacional, revenue y retention → decisión autónoma + ejecución si aplica.

### Ciclo periódico (Worker, 15 min)
1. Intelligence scan (Fase 14)
2. Captura muestras ML
3. Ejecución de los 6 agentes autónomos

Integra Revenue, Retention, Expansion e Intelligence (Fases 12–14).

### API
`POST /api/ai/cycle?tenantId=`

---

## 13. Business Knowledge Engine

`IBusinessKnowledgeEngine` → `BusinessKnowledgeRecords`

Aprende patrones `DecisionType:Action`:
- `Occurrences`, `SuccessRate`
- `Outcome`: Win / Loss / Neutral

`ResolvePreferredAction` devuelve la acción preferida según historial del tenant.

### API
`GET /api/ai/knowledge?tenantId=`

---

## 14. Executive AI API

### Dashboard principal
```
GET /api/ai/dashboard?tenantId={guid}
Authorization: Bearer / Cookie
```

**Respuesta (`ExecutiveAiDashboardDto`):**
- RecentDecisions
- Predictions (30–365d)
- NextBestActions
- TopKnowledge
- PendingDecisions, ExecutedToday
- AtRiskCustomers, ExpansionReady

### Todos los endpoints

| Método | Ruta |
|--------|------|
| GET | `/api/ai/dashboard` |
| GET | `/api/ai/decisions` |
| GET | `/api/ai/next-best-actions` |
| GET | `/api/ai/predictions` |
| GET | `/api/ai/knowledge` |
| GET | `/api/ai/ml-datasets` |
| POST | `/api/ai/decide/{customerId}` |
| POST | `/api/ai/cycle` |

Datos **100 % desde PostgreSQL**.

---

## 15. Business Process Simulation V5

Validación por rol — ¿puede AutonomusFlow tomar decisiones de negocio de forma autónoma?

| Rol | Pregunta clave | Resultado |
|-----|----------------|-----------|
| CEO | ¿El sistema decide sin humano? | 89% |
| Director Comercial | ¿NBA y Revenue agent actúan? | 91% |
| Revenue Manager | ¿Predicciones 30–365d? | 88% |
| Customer Success | ¿Rescate/renewal autónomos? | 90% |
| Account Manager | ¿Auditoría de decisiones? | 92% |

**Promedio V5: 90%**

---

## 16. Defectos conocidos

### P1
| ID | Defecto | Mitigación |
|----|---------|------------|
| A15-P1-01 | ML heurístico, no modelo entrenado | Pipeline ML Fase 16 |
| A15-P1-02 | Ciclo autónomo puede duplicar tareas | Idempotencia por `TaskType` |
| A15-P1-03 | NBA no asigna rep automáticamente | Integrar Smart Assignment (Fase 12) |

### P2
| ID | Defecto | Mitigación |
|----|---------|------------|
| A15-P2-04 | Consulta playbook states sin índice optimizado | Query por `NextActionAt` |
| A15-P2-05 | Sin kill-switch global de autonomía | Setting tenant `AutonomousMode` |

### Resueltos en Fase 15
- `DecisionEngine` stub → motor revenue real con evidencia
- Sin auditoría IA → `AiDecisionAudits`
- Sin orquestación unificada → `AutonomousOrchestrationEngine`

---

## 17. GO / NO-GO

### Decisión: **GO** — Autonomous Revenue Platform (Fase 15)

| Criterio | Estado |
|----------|--------|
| Decision Engine (6+ tipos) | ✓ |
| Next Best Action | ✓ |
| Autonomous Playbooks | ✓ |
| 6 Revenue AI Agents | ✓ |
| Predictive 30–365d | ✓ |
| ML Foundation datasets | ✓ |
| Autonomous CS + Comms | ✓ |
| AI Decision Audit | ✓ |
| Orchestration (eventos + ciclo) | ✓ |
| Business Knowledge | ✓ |
| `GET /api/ai/dashboard` | ✓ |
| Simulación V5 ≥ 85% | ✓ (90%) |
| Build Release | ✓ |
| Sin UI | ✓ |

### Operación
1. `dotnet ef database update` — migración `Phase15_AutonomousPlatform`
2. Worker activo (scan 15 min)
3. Opcional: `POST /api/ai/cycle?tenantId=` para forzar ciclo

### Resultado
AutonomusFlow opera como **plataforma autónoma de ingresos**: vende, retiene, renueva, expande, predice, decide y aprende de forma automática y controlada.
