# 05 — Catálogo de Automatizaciones

**Fuente:** `DomainEventDispatcher`, `Worker.cs`, engines en Infrastructure.

---

## Eventos de dominio → pipelines (API, síncrono)

| Motor | Eventos clave | Efecto |
|-------|---------------|--------|
| **WorkflowEngine** | Cualquier evento con workflow activo | Assign, UpdateStatus, CreateTask |
| **OperationalAutomation** | LeadQualified, DealClosed | Customer+deal draft+task / onboarding tasks |
| **RevenueAutomation** | LeadCreated, LeadScoreUpdated, LeadQualified | SLA, asignación score alto, scan periódico |
| **RetentionAutomation** | CustomerCreated, DealClosed, RiskScore≥70 | Status Customer, playbooks, emails |
| **AutonomousOrchestration** | Varios (gated) | Decisiones autónomas + ejecución |
| **BusinessMemoryPipeline** | Seleccionados | Episodios memoria semántica |

---

## RabbitMQ → Workers

| Agente | Evento | Efecto |
|--------|--------|--------|
| LeadIntelligenceAgent | LeadCreated | Score → LeadScoreUpdated |
| CommunicationAgent | LeadCreated, CustomerCreated | Email/WhatsApp bienvenida |
| CustomerRiskAgent | CustomerCreated | Risk score |
| CustomerHealthAgent | CustomerCreated | Playbooks rescue/adoption |
| ChurnRiskAgent | RiskScore≥60 | Acciones churn |
| DealStrategyAgent | DealCreated, StageChanged | Tareas inteligencia ventas |
| OutcomeAttribution | DealClosed/Lost | NBA ML + ABOS learning |

---

## Jobs periódicos (cada 15 min)

`Worker.cs` por tenant:
- Revenue scan (deals estancados, leads inactivos)
- Data quality tasks (`DataQualityRevenueService`)
- Retention scan
- Renewal / Expansion agents
- Intelligence scan
- Customer insights
- Autonomous cycle completo

**Cada 6 h:** `BusinessMemoryConsolidationWorker`

---

## Limitaciones documentadas

| Componente | Estado |
|------------|--------|
| Workflow `Communicate` | Solo log |
| Workflow `ActivateAgent` | Solo log |
| AutomationOptimizerAgent | Solo log (TODO) |
| DataQualityGuardian | Registrado, no invocado |
| ComplianceSecurityAgent | No bloquea (TODO) |

---

## Monitoreo

- `/FailedEvents` — DLQ replay
- `/Audit` — eventos de dominio
- `/Tasks` — tareas generadas
- Logs Docker: `autonomuscrm-workers`, `autonomuscrm-api`
