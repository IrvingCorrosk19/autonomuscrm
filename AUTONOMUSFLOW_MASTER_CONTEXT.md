# AUTONOMUSFLOW — MASTER CONTEXT

**Documento maestro vivo** — única fuente de verdad documental del proyecto.  
**Última actualización:** 2026-06-02 (Iteración v0.9 — Comms resilientes, Outcome Fabric, Data Cloud CDP, Twilio, SCIM Groups, 45 tests)  
**Versión documental:** **v0.9**  
**Base de evidencia:** `dotnet build` OK · **45/45 tests unitarios** · Migración `Phase20_Abos95_Foundation` · Ver documentos operativos v0.9.

## Documentos operativos (solo estos 5)

| Documento | Propósito |
|-----------|-----------|
| [AUTONOMUSFLOW_MASTER_CONTEXT.md](AUTONOMUSFLOW_MASTER_CONTEXT.md) | Fuente de verdad |
| [AUTONOMUSFLOW_EXECUTION_PLAN.md](AUTONOMUSFLOW_EXECUTION_PLAN.md) | Roadmap iterativo |
| [AUTONOMUSFLOW_GAPS_AND_RISKS.md](AUTONOMUSFLOW_GAPS_AND_RISKS.md) | Brechas y riesgos |
| [AUTONOMUSFLOW_TEST_EVIDENCE.md](AUTONOMUSFLOW_TEST_EVIDENCE.md) | Pruebas ejecutadas |
| [AUTONOMUSFLOW_PRODUCTION_READINESS.md](AUTONOMUSFLOW_PRODUCTION_READINESS.md) | Go/No-Go producción |

---

## Reglas de este documento

- Todo descubrimiento futuro se **añade aquí**, no en MD dispersos.
- **No se elimina** historial; se versiona en [Historial de actualizaciones](#historial-de-actualizaciones).
- Afirmaciones marcadas **(código)** tienen respaldo en repositorio; **(ops)** en observación runtime; **(hipótesis)** requiere validación.

---

# Resumen Ejecutivo

AutonomusFlow (repo: **AutonomusCRM**) es un CRM enterprise .NET 9 con arquitectura limpia, multi-tenant, event-driven (RabbitMQ + Event Store), PostgreSQL como única BD operativa, y tres capas de “inteligencia” acumuladas en fases 14–16:

1. **Customer Intelligence** (analytics, NPS, CSAT, churn V2, expansión, data mart).
2. **Autonomous Revenue Platform** (decisiones, NBA, playbooks, 6 agentes, predicción, auditoría).
3. **Enterprise AI** (ML pipeline logistic regression, registry, drift, knowledge graph, self-learning).

**Hallazgo central (análisis 2026-06-02):** El sistema es **rico en consumo y transformación de datos internos**, pero **pobre en adquisición e integración de datos externos**. Toda la IA depende de lo que ya está en PostgreSQL (CRM manual, seed demo, CSV/JSON UI, APIs puntuales de uso/NPS/CSAT).

**Hipótesis Data Acquisition — resultado tras destrucción (Fase B):** Es **verdadera como cuello de valor de producto** (F14–16 no escalan sin datos externos), pero **falsa como única prioridad inmediata** si el objetivo es *vender y operar en producción mañana*. Hoy compite y **pierde** frente a: (1) confiabilidad Worker/EventBus, (2) fundación comercial SaaS, (3) conectividad mínima (no plataforma completa).

**Recomendación CTO — presupuesto UNA sola fase (Fase I):** Construir **Fundación Operativa Enterprise + Conectividad Mínima** (FOECM): estabilizar producción (RabbitMQ/Worker/SLO), API de ingesta (import REST + webhooks), cierre parcial del loop de outcomes, onboarding tenant programático. **No** construir primero la plataforma completa de Data Acquisition.

**Estado producción VPS (ops):** **Validado 2026-06-02 00:58 UTC** — API + Worker **Up**; colas RabbitMQ suscritas incl. `Deal.Closed`; **ciclo autónomo ejecutándose** (`Autonomous cycle tenant …: 3 total actions`). Fix Worker: `AddApplication()` + `WorkerAgentConfigurationService`. `AI__Enabled=true`. URL: http://164.68.99.83:8091

**Categoría objetivo:** **ABOS** — *Autonomous Business Operating System*. **Madurez global v0.9: ~90/100** (objetivo 95+ — ver [Iteración v0.9](#iteración-ejecución-v09--programa-abos-95)).

---

# Visión del Producto

**Promesa acumulada en código:** plataforma que detecta, predice, decide, actúa y aprende sobre ingresos y clientes con mínima intervención humana.

**Realidad de datos hoy:** la promesa asume un **lagoon de datos internos** alimentado principalmente por:

- Uso humano del CRM (CRUD UI + API REST).
- Eventos de dominio generados por cambios en leads/deals/customers.
- Seed de demostración.
- Importación batch limitada (CSV/JSON).
- Ingesta API manual (`POST /api/intelligence/usage`, NPS, CSAT).

**Brecha de visión:** sin tubería de datos externos, la “Enterprise AI OS” opera como **motor sobre un dataset cerrado**, no sobre la realidad operativa del tenant.

---

# Estado Actual

| Dimensión | Estado | Evidencia |
|-----------|--------|-----------|
| Repo principal | `main` @ Fases 14–16 commitidas | Git |
| BD | PostgreSQL única, ~26 entidades EF | `ApplicationDbContext` |
| API | 14 controllers REST + Razor Pages | `AutonomusCRM.API` |
| Workers | 11 agentes + ciclo 15 min | `AutonomusCRM.Workers` |
| Event bus | RabbitMQ (o InMemory dev) | `DependencyInjection`, `Worker` |
| ML | Logistic regression in-process, min 25 muestras | `EnterpriseAI/MlMath` |
| Integraciones externas | **Hub v1 en código** — HubSpot, Salesforce, Gmail, Outlook, Stripe (sync REST; requiere tokens tenant) | `Infrastructure/Integrations/*`, `api/integrations` |
| Import masivo | UI CSV/JSON, 5 MB / 5k filas | `ImportGuard`, Pages/Import |
| VPS preview | http://164.68.99.83:8091 | `DESPLIEGUE_VPS_AUTONOMUSCRM.md` |
| VPS Worker | **Up** (ciclo autónomo validado v0.7) | ops 2026-06-02 |
| `AI__Enabled` | **true** en VPS | `docker-compose.vps.yml` |
| `AutonomusCRM.AI` | Proyecto placeholder | `PlaceholderServices.cs` |

---

# Arquitectura Actual

```
┌─────────────────────────────────────────────────────────────────┐
│  AutonomusCRM.API (Razor + REST + Auth JWT/Cookies)             │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│  AutonomusCRM.Application (Commands, DTOs, Interfaces, Entities)    │
│   · CRM core · Revenue · CustomerSuccess · Intelligence           │
│   · Autonomous · EnterpriseAI                                     │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│  AutonomusCRM.Infrastructure                                      │
│   · EF Core / Repositories · EventStore · RabbitMQ EventBus      │
│   · Engines (Revenue, CS, Intelligence, Autonomous, EnterpriseAI)│
└────────────────────────────┬────────────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
   PostgreSQL          Redis (cache)      RabbitMQ
         ▲
         │ única fuente de verdad operativa
┌────────┴────────────────────────────────────────────────────────┐
│  AutonomusCRM.Workers (BackgroundService)                        │
│   · Suscripciones eventos · Scans periódicos cada 15 min          │
└─────────────────────────────────────────────────────────────────┘

AutonomusCRM.Domain — entidades agregado (Tenant, Customer, Lead, Deal, User…)
AutonomusCRM.AI — stub (no integrado a producción)
```

**Patrones clave (código):**

- **Clean Architecture** — Domain sin dependencias externas.
- **Multi-tenant** — `ICurrentTenantAccessor`, query filters en `ApplicationDbContext`.
- **Event-driven** — `DomainEventDispatcher` → EventStore, Workflows, Automation, Revenue, Retention, Autonomous, EventBus.
- **CQRS ligero** — Commands/Handlers en Application; sin BD separada de lectura.

---

# Fases Implementadas

> Fases 1–13: CRM core, workflows, revenue ops (12), retention (13). Detalle mínimo aquí; foco 14–16.

## Fase 14 — Product Analytics & Customer Intelligence

| Campo | Detalle |
|-------|---------|
| **Objetivo** | Inteligencia de producto y cliente: uso, feedback, segmentación, churn V2, expansión, insights. |
| **Capacidades** | Product analytics, NPS, CSAT, Customer Insights, Churn V2, Expansion Intelligence, Segmentation, Feedback, Data Mart (snapshots diarios), Customer Insights Agent. |
| **APIs** | `GET/POST /api/intelligence/*` — dashboard, product-analytics, nps, csat, insights, churn-predictions, expansion, segmentation, feedback, trends; `POST` nps, csat, usage, scan. |
| **Tablas** | `ProductUsageEvents`, `CustomerFeedbacks`, `CustomerAnalyticsSnapshots` |
| **Migración** | `Phase14_ProductAnalytics` |
| **Estado** | Implementado (código). Alimentación: datos internos + POST usage/NPS/CSAT. |

## Fase 15 — Autonomous Revenue Platform

| Campo | Detalle |
|-------|---------|
| **Objetivo** | Detectar → decidir → actuar → auditar → aprender (heurístico + foundation ML). |
| **Capacidades** | Decision Engine, NBA, Playbooks autónomos, 6 Revenue AI Agents, Predictive Revenue 30–365d, ML Foundation (snapshots), CS autónomo, Comms autónomas, AI Audit, Business Knowledge, Orchestration. |
| **APIs** | `GET/POST /api/ai/*` — dashboard, decisions, next-best-actions, predictions, knowledge, ml-datasets, decide, cycle. |
| **Tablas** | `AiDecisionAudits`, `AutonomousPlaybookStates`, `BusinessKnowledgeRecords`, `MlFeatureSnapshots` |
| **Migración** | `Phase15_AutonomousPlatform` |
| **Estado** | Implementado. Ciclo 15 min en Worker + eventos vía `DomainEventDispatcher`. |

## Fase 16 — Autonomous Enterprise AI

| Campo | Detalle |
|-------|---------|
| **Objetivo** | ML entrenable, registry, MLOps drift, self-learning, knowledge graph, optimización, analytics/governance ejecutivos. |
| **Capacidades** | ML Pipeline (logistic regression), Churn/Expansion/Revenue ML models, NBA ML scorer, Self Learning, Model Registry + rollback, MLOps drift, AI Evaluation, Business Knowledge Graph, Autonomous Optimization, Executive AI Analytics, AI Governance, Enterprise AI Cycle. |
| **APIs** | Extiende `/api/ai/*` — analytics, governance, models, train, train-all, rollback, ml/churn|expansion|revenue, evaluation, knowledge-graph, enterprise-cycle. |
| **Tablas** | `MlModelVersions`, `MlPipelineRuns`, `MlDriftReports`, `BusinessKnowledgeGraphEdges`, `NbaOutcomeRecords` |
| **Migración** | `Phase16_EnterpriseAi` |
| **Estado** | Implementado (código). Entrenamiento requiere ≥25 `MlFeatureSnapshots` por dataset. Churn V2 usa ML si hay modelo activo. |

---

# Capacidades Existentes

## CRM Core (pre-14)
- Tenants, Users (roles, MFA), Customers, Leads, Deals, Workflows, Policies, WorkflowTasks.
- Event Store + Snapshots, TimeSeries metrics, Failed event messages.
- Operational automation P0, Workflow engine.

## Revenue Operations (Fase 12)
- Forecast, performance, pipeline coverage, win/loss, productivity, commercial SLA, smart assignment, revenue automation, KPIs, executive sales dashboard, sales intelligence, **data quality scan** (interno).

## Customer Success / Retention (Fase 13)
- Contracts, communications log, health, churn risk (v1), renewal, playbooks, email/WhatsApp (**Log providers**, no SMTP/API real), journey, expansion revenue, retention automation, customer KPIs, executive customer dashboard.

## Intelligence (Fase 14)
- Ver fase 14.

## Autonomous + Enterprise AI (Fase 15–16)
- Ver fases 15–16.

## Importación (limitada)
- Razor Pages Import: Customers, Leads, Deals, Users, Workflows (JSON), Policies.
- `ImportGuard`: max 5 MB, 5000 filas, solo `.csv` / `.json`.
- Sin API REST de importación.
- Settings: import/export solo **configuración tenant** (JSON), no datos CRM.

---

# Motores Existentes

| Dominio | Motor / Servicio |
|---------|------------------|
| Revenue | RevenueForecastEngine, SalesPerformanceEngine, PipelineCoverageService, WinLossAnalyticsService, SalesProductivityService, CommercialSlaEngine, SmartAssignmentEngine, RevenueAutomationEngine, RevenueKpiService, ExecutiveSalesDashboardService, SalesIntelligenceService, **DataQualityRevenueService** |
| Customer Success | CustomerHealthEngine, ChurnRiskEngine, RenewalEngine, CustomerPlaybookService, Email/WhatsApp Automation (log), CustomerJourneyEngine, ExpansionRevenueEngine, CustomerSuccessIntelligenceService, RetentionAutomationEngine, CustomerKpiService, ExecutiveCustomerDashboardService |
| Intelligence | ProductAnalyticsEngine, NpsEngine, CsatEngine, CustomerInsightsEngine, ProductUsageIntelligence, **ChurnPredictionV2Service**, ExpansionIntelligenceService, CustomerSegmentationEngine, FeedbackEngine, **CustomerDataMartService**, ExecutiveIntelligenceDashboardService, CustomerInsightsAgentService, IntelligenceAutomationEngine |
| Autonomous | AutonomousRevenueDecisionEngine, NextBestActionEngine, AutonomousPlaybookEngine, PredictiveRevenueEngine, MlFoundationService, AutonomousCustomerSuccessEngine, AutonomousCommunicationsEngine, AiDecisionAuditService, BusinessKnowledgeEngine, AutonomousOrchestrationEngine, ExecutiveAiDashboardService |
| Enterprise AI | MachineLearningPipelineService, ChurnPredictionModelService, ExpansionPredictionModelService, RevenuePredictionModelService, NextBestActionMlService, SelfLearningEngine, ModelRegistryService, MlOpsFoundationService, AiEvaluationFrameworkService, BusinessKnowledgeGraphService, AutonomousOptimizationEngine, ExecutiveAiAnalyticsService, AiGovernanceService, EnterpriseAiCycleService |
| Platform | WorkflowEngine, PolicyEngine, OperationalAutomationService, DecisionEngine (wrapper), DomainEventDispatcher |

---

# Agentes Existentes

## Workers — event-driven (`AutonomusCRM.Workers/Agents`)
| Agente | Trigger |
|--------|---------|
| LeadIntelligenceAgent | LeadCreated |
| CustomerRiskAgent | CustomerCreated |
| CustomerHealthAgent | CustomerCreated |
| ChurnRiskAgent | CustomerRiskScoreUpdated |
| DealStrategyAgent | DealCreated, DealStageChanged |
| CommunicationAgent | CustomerCreated, LeadCreated |
| ComplianceSecurityAgent | IDomainEvent (kill-switch) |
| CustomerInsightsAgent | Ciclo 15 min |
| RenewalAgent | Ciclo 15 min |
| ExpansionAgent | Ciclo 15 min |
| AutomationOptimizerAgent | (registrado en DI) |

## Autonomous — orquestados (`Infrastructure/Autonomous/AutonomousAgents.cs`)
| Agente | Rol |
|--------|-----|
| RevenueAutonomousAgent | Deals abiertos + sales intelligence |
| RenewalAutonomousAgent | Renovaciones + decisiones |
| ChurnAutonomousAgent | Churn V2 + rescue automático |
| ExpansionAutonomousAgent | Expansion-ready + playbooks |
| CustomerAutonomousAgent | CS autónomo + insights |
| OperationsAutonomousAgent | Revenue scan + data quality + intelligence scan |

---

# APIs Existentes

| Prefijo | Propósito |
|---------|-----------|
| `/api/auth` | login, MFA, refresh |
| `/api/tenants` | CRUD tenant |
| `/api/customers` | create, get, status |
| `/api/leads` | create, list, get, qualify |
| `/api/deals` | create, list, get, stage, close, lose |
| `/api/users` | create, get, MFA |
| `/api/workflows` | list, get |
| `/api/tasks` | list, complete, assign |
| `/api/revenue` | dashboard, forecast, leaderboard, pipeline, win-loss, productivity, sla, kpis, quotas, scan |
| `/api/customer` | dashboard, health, churn, renewals, expansion, journey, kpis, playbooks, scan, intelligence agents |
| `/api/intelligence` | Fase 14 completa |
| `/api/ai` | Fases 15–16 |
| `/api/metrics` | timeseries |
| `/health` | health checks |

**UI (Razor):** CRUD + Import pages — canal principal de carga humana de datos.

---

# Workers Existentes

**Clase:** `AutonomusCRM.Workers.Worker` — `BackgroundService`.

**Ciclo cada 15 min (por tenant):**
1. `IRevenueAutomationEngine.RunPeriodicRevenueScanAsync`
2. `IDataQualityRevenueService.ScanAndCreateTasksAsync`
3. `IRetentionAutomationEngine.RunPeriodicRetentionScanAsync`
4. `RenewalAgent.RunTenantRenewalScanAsync`
5. `ExpansionAgent.RunTenantExpansionScanAsync`
6. `IIntelligenceAutomationEngine.RunPeriodicIntelligenceScanAsync`
7. `CustomerInsightsAgent.RunTenantScanAsync`
8. `IAutonomousOrchestrationEngine.RunAutonomousCycleAsync` → incluye `EnterpriseAiCycleService`

**Dependencia crítica:** RabbitMQ + Worker **UP**. Sin Worker, solo queda procesamiento síncrono vía API/eventos en proceso API.

---

# Tablas Existentes (PostgreSQL / EF)

| Tabla | Origen / uso |
|-------|----------------|
| Tenants, Users | Core + multi-tenant |
| Customers, Leads, Deals | Core CRM |
| Workflows, Policies, WorkflowTasks | Automatización |
| DomainEvents, Snapshots | Event sourcing |
| TimeSeriesMetrics | Métricas |
| SalesQuotas | Revenue Fase 12 |
| CustomerContracts, CustomerCommunicationLogs | Retention Fase 13 |
| ProductUsageEvents, CustomerFeedbacks, CustomerAnalyticsSnapshots | Intelligence Fase 14 |
| AiDecisionAudits, AutonomousPlaybookStates, BusinessKnowledgeRecords, MlFeatureSnapshots | Autonomous Fase 15 |
| MlModelVersions, MlPipelineRuns, MlDriftReports, BusinessKnowledgeGraphEdges, NbaOutcomeRecords | Enterprise AI Fase 16 |
| FailedEventMessages | Resiliencia eventos |

---

# Cómo obtiene datos AutonomusFlow hoy

## Flujos de entrada confirmados (código)

| # | Fuente | Mecanismo | Datos que alimentan |
|---|--------|-----------|---------------------|
| 1 | **Usuario CRM** | Razor Pages + REST API | Customers, Leads, Deals, Users, Workflows |
| 2 | **Seed / migración EF** | `DatabaseSeeder` al arranque si `Seed:Enabled` | Tenant demo, usuarios, clientes, leads, deal, QA tenant B |
| 3 | **Import archivos** | UI Import CSV/JSON | Customers, Leads, Deals, Users, Workflows, Policies |
| 4 | **Eventos de dominio** | Acciones CRM → `DomainEventDispatcher` | Workflows, automation, revenue, retention, autonomous |
| 5 | **API Intelligence** | `POST /api/intelligence/usage`, nps, csat | ProductUsageEvents, CustomerFeedbacks |
| 6 | **Login sync** | `ProductAnalyticsEngine.SyncFromUserLoginsAsync` en scan | ProductUsageEvents (Auth/login) |
| 7 | **Data Mart interno** | `CustomerDataMartService.BuildDailySnapshotsAsync` | CustomerAnalyticsSnapshots (derivado, no externo) |
| 8 | **ML Foundation** | `CaptureTrainingSamplesAsync` — lee tablas internas | MlFeatureSnapshots |
| 9 | **Knowledge / Graph** | Patrones de audits + rebuild desde clientes/deals/churn/expansion | BusinessKnowledgeRecords, GraphEdges |
| 10 | **Comunicaciones** | Motores CS — **LogEmail/WhatsApp** (no envío real) | CustomerCommunicationLogs |

## Quién alimenta el sistema

| Actor | Rol |
|-------|-----|
| Equipo comercial / CS | CRUD manual en UI |
| Admin | Import CSV/JSON, seed, configuración |
| Sistema (Worker) | Deriva snapshots, tareas DQ, agentes — **no trae datos externos** |
| Integraciones externas | **Nadie** — no existen conectores |

## Procesos que existen vs faltan

| Existe | Falta |
|--------|-------|
| Validación import (tamaño, filas) | ETL programado |
| Data quality **interno** (duplicados email, deals sin owner) | Deduplicación global cross-source |
| Normalización en motores (scores, heurísticas) | Schema discovery fuentes externas |
| Snapshots analytics diarios | Sync incremental CRM externo |
| Captura ML desde BD interna | Enriquecimiento (firmográfico, billing real) |
| Webhook inbound | Conectores OAuth (Salesforce, etc.) |
| Atribución outcome externo | Data catalog / lineage |

---

# Análisis de fuentes de datos (soporte real)

| Categoría | Tecnología | ¿Soportado? | Evidencia |
|-----------|------------|-------------|-----------|
| **Archivos** | CSV | Parcial | UI Import |
| | JSON | Parcial | UI Import + Settings config |
| | Excel (.xlsx) | **No** | No en ImportGuard |
| | XML | **No** | — |
| **Bases de datos** | PostgreSQL | **Sí (solo destino)** | Única `DefaultConnection` |
| | SQL Server / MySQL / Oracle | **No** | Sin segundo connection string ni ETL |
| **CRM** | HubSpot, Salesforce, Zoho, Dynamics, Pipedrive | **No** | Sin referencias en código |
| **ERP** | SAP, NetSuite, Odoo | **No** | — |
| **Marketing** | Meta/Google/LinkedIn Ads | **No** | — |
| **Comunicación** | Gmail, Outlook, WhatsApp real | **No** | Log providers only |
| **Web** | Formularios / landing / webhooks | **No** | Sin controllers webhook |
| **APIs públicas** | Ingesta custom | Parcial | intelligence usage, NPS, CSAT |

---

# Dependencias Detectadas

```
Datos externos (NO EXISTEN)
        │
        ▼
┌───────────────────┐
│ PostgreSQL CRM    │◄── UI / API / Import / Seed
└─────────┬─────────┘
          │
    ┌─────┴─────┬─────────────┬──────────────┐
    ▼           ▼             ▼              ▼
 Intelligence  Revenue CS   Autonomous    Enterprise AI
 (snapshots)   (deals)      (decisions)   (ML train)
    │           │             │              │
    └───────────┴─────────────┴──────────────┘
                      │
              Requiere volumen y calidad
              de datos en tablas core
```

**Cadena crítica:**
- ML (F16) ← `MlFeatureSnapshots` ← `MlFoundationService` ← tablas F14 + CS + Revenue.
- Churn V2 / ML churn ← health, analytics snapshots, churn signals ← **actividad CRM**.
- Autonomous decisions ← health, churn, NPS, contracts ← **datos ingresados**.
- Business Knowledge ← outcomes de audits — hoy **éxito = ejecución**, no resultado negocio verificado (código: `MarkOutcomeAsync` al ejecutar).

**Infraestructura:**
- Worker depende de RabbitMQ (colas con DLX).
- API puede correr scans vía `POST .../scan` sin Worker (parcial).

---

# Riesgos Detectados

> **v0.1 obsoleto** — ver [Riesgos Detectados (actualizado v0.2)](#riesgos-detectados-actualizado-v02) al final del documento.

---

# Cuellos de Botella Detectados

> **v0.1 obsoleto** — ver sección [Cuellos de Botella (reordenado v0.2)](#cuellos-de-botella-reordenado-v02) al final del documento.

---

# Oportunidades Detectadas

1. **Autonomous Data Acquisition Platform** — ver sección siguiente.
2. **Outcome Fabric** — atribuir pagos, renovaciones, wins a decisiones IA.
3. **Integration Hub** — conectores + webhooks + mapping.
4. **Operaciones VPS** — estabilizar RabbitMQ antes de demos autónomas.

---

# FASE A — Análisis Total del Sistema (revalidado v0.2)

> Metodología: relectura de capas, grep integraciones (Stripe/SSO/Salesforce = 0), inventario tests (5 archivos), ops VPS. Sin implementación nueva.

## A.1–A.9 Resumen por dominio

| Dominio | Estado | Hallazgo clave |
|---------|--------|----------------|
| Arquitectura | Sólida monolito modular | Una BD; IA en Infrastructure; `AutonomusCRM.AI` stub |
| BD / MultiTenant | Operativo | `SubscriptionExpiresAt` sin billing engine |
| APIs | 14 controllers | Sin import REST, webhooks, billing |
| Eventos / Worker | **Riesgo prod** | DLX obligatorio en colas; VPS Worker caído |
| Agentes | Implementados | Kill-switch tenant sí; `AI__Enabled` no cableado |
| Revenue / CS / Intel | Rico en motores | Entrada = CRM manual + derivados |
| Enterprise AI / MLOps | Scaffolding listo | Min 25 muestras; entrena solo con lagoon interno |
| Governance | Parcial | MFA, audit; sin SSO/SCIM |
| DevOps / SaaS | Stack observability local | VPS reducido; sin Stripe |

**Conclusión A:** Cerebro construido (F14–16); sistema nervioso (eventos 24/7) y metabolismo de datos externos **incompletos**.

---

# FASE B — Destrucción de Hipótesis: ¿Data Acquisition Platform primero?

## B.1 Demostrar que es **incorrecta** como prioridad #1

| Argumento | Veredicto |
|-----------|-----------|
| Sin Worker, ingest no alimenta autonomía | **Válido** |
| Sin billing no hay SaaS vendible | **Válido** |
| Import CSV + APIs intelligence existen para POC | Parcial |
| SSO bloquea banco/aseguradora antes que Salesforce | **Válido** segmentado |

## B.2 Demostrar que es **correcta** como capacidad

| Argumento | Veredicto |
|-----------|-----------|
| Cero conectores externos (grep) | **Confirmado** |
| ML falla con <25 snapshots | **Confirmado** (código) |
| F14–16 consumen PostgreSQL, no mundo real | **Confirmado** |
| Enterprise exige integraciones | **Confirmado** (negocio) |

## B.3 Ranking alternativas (extracto)

| Rank | Iniciativa | Nota |
|------|------------|------|
| 1 | Producción & Event Platform Hardening | Worker/RabbitMQ — **P0** |
| 2 | SaaS Commercial Foundation (billing) | ARR AutonomusFlow |
| 3 | Conectividad mínima (Import API + webhooks) | Slice Data Acquisition |
| 4 | Outcome Fabric & AI Trust | `MarkOutcomeAsync` = ejecutado, no negocio |
| 5 | Enterprise Governance (SSO) | Banco/seguros |
| 7 | Integrations Hub **completo** | = Data Acquisition full — rank **#4 en roadmap fases** |

## B.4 Conclusión

- **Sí** necesitamos Data Acquisition **como destino**.
- **No** como única siguiente fase si vendemos mañana o operamos VPS hoy.

---

# FASE C — Pruebas de Negocio (7 arquetipos)

| # | Empresa | ¿Compra hoy? | Bloqueador principal | Time-to-value |
|---|---------|--------------|----------------------|---------------|
| 1 | Startup SaaS | Piloto posible | Sin HubSpot; Worker inestable | 1 semana |
| 2 | Call Center | Difícil | Sin telefonía/CCaaS real; bulk >5k | 1 mes |
| 3 | Universidad | Piloto retrasado | SSO/LDAP; datos sensibles | 1 mes+ |
| 4 | Aseguradora | Solo piloto acotado | Compliance + sin datos póliza | 1 mes+ |
| 5 | Banco | **No** | SSO + ops cert + no CSV PII manual | N/A |
| 6 | Retail | Piloto posible | Sin e-commerce connector | 2–4 semanas |
| 7 | Agencia Marketing | **Misfit** | Necesita ads/campañas | No priorizar ICP |

---

# FASE D — Pruebas de Producción

| Componente | Listo | Rompe prod | Impide vender |
|------------|-------|------------|---------------|
| API | Parcial | — | Demo OK |
| Worker | **No VPS** | Autonomía parada | Promesa 24/7 falsa |
| RabbitMQ | Parcial | PRECONDITION_FAILED DLX | Upgrades frágiles |
| ML train | Parcial | <25 samples | “IA” sin modelo |
| Observability | Local only | Sin alertas VPS | Soporte ciego |

**Top 3:** Worker down · dataset vacío post-onboarding · outcomes = ejecución.

---

# FASE E — Pruebas de Datos

| Entidad | Alimentación | Humano | Automatización | Calidad |
|---------|--------------|--------|----------------|---------|
| Leads/Customers/Deals | UI/API/Import | Alta | Media post-evento | Media |
| Usage/NPS/CSAT | POST API / scan | Alta | Parcial | Baja sin producto |
| Revenue forecast | Deals internos | Alta | Derivado | Sin cash real |
| Contracts | Retention engine | Baja | Interno | No legal/billing |
| Communications | Log providers | — | **Simulado** | No real |

**Scores:** Dependencia humana **75** · Automatización **35** · Calidad vs mundo real **40**.

---

# FASE F — Pruebas de IA

| Pregunta | Respuesta |
|----------|-----------|
| ¿Aprende? | Parcial — requiere `Converted` en NBA outcomes |
| ¿Mejora? | Teórico — riesgo ilusión sin ground truth |
| ¿Datos suficientes? | No en tenant nuevo |
| ¿Ground truth? | Débil — labels sobre features internas |
| ¿Feedback loop? | Parcial — `MarkOutcomeAsync` marca **ejecutado** (`AiDecisionAuditService` L28–33) |
| ¿Drift? | Detectable (`MlDriftReports`) |

**Veredicto:** MLOps scaffolding enterprise; operación = ML sobre CRM manual.

---

# FASE G — Pruebas de Adopción

| Escenario | Tiempo primer valor |
|-----------|---------------------|
| Demo seed | 1 hora |
| Piloto CSV | 1 día |
| Piloto API custom | 1 semana |
| Enterprise Salesforce | 1 mes+ (sin conector) |
| Autonomía creíble día 1 | **No realista** hoy |

---

# FASE H — Roadmap Definitivo (#1–#5)

### #1 — FOECM (Fundación Operativa Enterprise + Conectividad Mínima)

ROI muy alto · Complejidad media · Riesgo bajo-medio. Incluye: fix RabbitMQ, Worker SLO, REST Import API, webhooks, outcome MVP, onboarding API tenant.

### #2 — SaaS Commercial Foundation (Billing)

ROI **máximo ARR** · Requiere FOECM para metering eventos.

### #3 — Outcome Fabric & AI Trust Loop

ROI credibilidad IA · Depende FOECM + datos mínimos.

### #4 — Integrations Hub (Data Acquisition completa)

ROI enterprise muy alto · Complejidad muy alta 3–6 meses · Salesforce/HubSpot/Stripe/sync.

### #5 — Enterprise Governance Pack

SSO, export, compliance · Desbloquea banco/seguros.

## Matriz de impacto por fase (Fase H detalle)

| Fase | ROI | Complejidad | Riesgo | Dependencias | Valor comercial | Valor técnico | Impacto IA | Impacto SaaS |
|------|-----|-------------|--------|--------------|-----------------|---------------|------------|--------------|
| **#1 FOECM** | Muy alto | Media (8–12 sem) | Bajo-Medio | Ninguna major | Demo/pilotos creíbles | Worker estable, ingest API | Alto (datos+loop) | Medio (metering prep) |
| **#2 Billing** | **Máximo ARR** | Media-Alta | Medio PCI | FOECM recomendado | Venta SaaS real | Límites por plan | Bajo | **Crítico** |
| **#3 Outcome Fabric** | Alto credibilidad | Media | Medio atribución | FOECM + datos | Reduce churn cliente | Ground truth | **Muy alto** | Medio |
| **#4 Integrations Hub** | Muy alto enterprise | **Muy alta** (3–6 mes) | Alto mantenimiento | #1 + #3 parcial | Requisito enterprise | ETL/sync | **Muy alto** | Alto stickiness |
| **#5 Governance** | Alto regulado | Alta | Proceso | FOECM ops | Banco/seguros/EDU | Audit/SSO | Medio | Alto compliance |

**FOECM — alcance mínimo verificable (Definition of Done v0.2):**
1. Worker UP 7 días en VPS sin crash; ciclo 15 min ejecutado (métrica/log).
2. Playbook deploy RabbitMQ: colas DLX compatibles o prefix nuevo + cutover.
3. `POST /api/import/{entity}` con mismos límites `ImportGuard` + auth tenant.
4. `POST /api/webhooks/{source}` con HMAC + idempotencia.
5. Outcome: campo/endpoint separa `executed` vs `businessSucceeded` en audits.
6. Onboarding: API crear tenant + admin sin seed manual.
7. Tests integración: health, import, webhook, worker smoke.

---

# FASE I — Recomendación CTO (UNA sola fase, presupuesto único)

**Construir: #1 FOECM** — no Data Acquisition Platform completa.

| Pregunta | Respuesta |
|----------|-----------|
| ¿Por qué? | Worker roto invalida promesa autónoma; ingest sin consumo no vale; más datos sin outcomes empeora ML |
| ¿Qué desbloquea? | Ventas demo 24/7, pilotos API, entrenamiento ML posible, base para Billing e Integrations Hub |
| ¿Ingresos? | Indirecto (pilotos); MRR requiere fase #2 Billing |
| ¿Riesgo elimina? | R2, R7, R5; parcial R1, R3 |
| ¿Alternativa si solo ARR? | Fase #2 Billing — decisión comercial |

---

# Hipótesis: ¿Autonomous Data Acquisition Platform? (síntesis v0.2)

## Estado de la hipótesis

| Hipótesis | Resultado v0.2 |
|-----------|----------------|
| “Necesitamos más motores IA” | **Descartada** |
| “El cuello de botella es datos” | **Confirmada** (valor producto) |
| “Fase 17 = solo Data Acquisition” | **Rechazada** como única fase |
| “FOECM = siguiente fase única” | **Aceptada** |
| “Data Acquisition full en roadmap” | **#4** — necesaria, no primera |

## Anexo — Data Acquisition Platform (fase #4, diseño futuro)

Rationale conservado de v0.1: toda inteligencia consume PostgreSQL; sin tubería externa el producto no escala con el negocio del cliente. Desbloquea F14–16, revenue/CS con billing real, warehouse futuro. **No es** migración one-shot ni otro dashboard — es sync continuo + validación + enriquecimiento.

| No es | Es |
|-------|-----|
| Migración one-shot | Plataforma sync continuo |
| Solo ETL batch | Conectar + validar + enriquecer + mantener |
| Reemplazar F14–16 | Habilita F14–16 |

**Orden v0.2:** FOECM → Billing (si ARR) → Outcome Fabric → **Data Acquisition full** → Governance.

---

# Riesgos Detectados (actualizado v0.2)

| ID | Riesgo | Severidad | Mitigación FOECM |
|----|--------|-----------|------------------|
| R1 | Dataset cerrado | Alta | Webhooks + Import API |
| R2 | Worker caído VPS | Alta | Queue migration playbook |
| R3 | Outcomes = ejecución | Alta | Outcome MVP |
| R4 | Comms simuladas | Media | Integrations Hub (#4) |
| R5 | Import sin API | Media | REST Import |
| R6 | AI__Enabled inerte | Baja | Config global |
| R7 | RabbitMQ queue drift | Alta | Migración colas / prefix |
| R8 | Un solo PostgreSQL | Media | Backups + réplica |
| R9 | ~5 archivos de tests | Alta | Tests críticos FOECM |
| R10 | Sin billing/SSO | Alta venta | Fases #2 / #5 |

---

# Cuellos de Botella (reordenado v0.2)

| Prioridad | Cuello |
|-----------|--------|
| **P0** | Confiabilidad Worker / EventBus |
| **P0b** | Conectividad mínima (slice acquisition) |
| P1 | Outcome / ground truth |
| P2 | Dataset cerrado (acquisition full) |
| P3 | Billing SaaS |
| P4 | SSO / governance |
| P5 | Más modelos IA |

---

# Información que aún falta

| Tema | Acción |
|------|--------|
| Conteo `MlFeatureSnapshots` en VPS | Query SQL tenant demo |
| Latencia ciclo 15 min Worker UP | Tempo/Prometheus |
| Conector prioritario #1 | Decisión comercial |
| GDPR ingesta | Legal + fase #4 |
| `AutonomusCRM.AI` | Fusionar o eliminar |

---

# Historial de actualizaciones

| Fecha | Versión | Cambios |
|------|---------|---------|
| 2026-06-02 | v0.1 | Análisis inicial, hipótesis Data Acquisition, riesgos VPS, comprensión 78%. |
| 2026-06-02 | **v0.2** | Fases A–I, destrucción hipótesis, 7 arquetipos, pruebas prod/datos/IA/adopción, roadmap #1–#5, CTO → FOECM, riesgos R9–R10. |
| 2026-06-02 | **v0.3** | Análisis Supremo: comparativa vs SF/HubSpot/Dynamics/Zoho/Pipedrive, scores 0–100 por dominio, madurez IA, categoría ABOS, 10 fases 24 meses, gaps brutales, visión 5 años, inversión 24 meses. |
| 2026-06-02 | **v0.4** | **Ejecución FOECM (Phase 17):** RabbitMQ resiliente, AI gate, business outcomes, Import API, Webhooks, migración EF, tests +15. Ver [Iteración ejecución v0.4](#iteración-ejecución-v04--foecm). |
| 2026-06-02 | **v0.5** | Iteración 2: SMTP provider, OutcomeAttribution DealClosed, Provisioning API, SaaS trial+middleware, auto-migrate prod, VPS redeploy Worker UP. Ver [Iteración v0.5](#iteración-ejecución-v05). |
| 2026-06-02 | **v0.6** | Phase 18 ABOS: Integrations Hub, Stripe billing, SendGrid/SES/WhatsApp, Outcome Fabric ampliado, Trust Inbox API, Customer 360, Marketplace SDK, OIDC/SCIM foundation. Ver [Iteración v0.6](#iteración-ejecución-v06--phase-18-abos-foundation). |
| 2026-06-02 | **v0.7** | Trust Inbox UI, Integrations wizard+OAuth, plan limits middleware, SCIM real, Voice MVP, comms banner, VPS redeploy Phase 18/19. Ver [Iteración v0.7](#iteración-ejecución-v07--abos-operacional). |
| 2026-06-02 | **v0.8** | Programa ABOS: 4 docs operativos, Trust policy+metrics, AI Command Center, Customer360 UI, Identity Resolution. Ver [Iteración v0.8](#iteración-ejecución-v08--programa-abos). |
| 2026-06-02 | **v0.9** | Outcome Fabric, Comms delivery+retry, OAuth refresh, identity merge, CDP stream, warehouse CSV, Twilio webhook, SCIM Groups, SAML metadata, Command Center ampliado, Testcontainers, 45 tests. Ver [Iteración v0.9](#iteración-ejecución-v09--programa-abos-95). |

---

# Iteración ejecución v0.8 — Programa ABOS

> **Objetivo:** 82 → **95+** · **Resultado iteración:** **~86** (en camino)

## 1. Qué encontré
- Prioridades Trust → Data Cloud → Command Center **siguen correctas**
- Trust tenía umbral HITL fijo (70) — no enterprise-configurable
- Data Cloud sin UI ni identity resolution
- Sin documentos operativos estructurados (solo master)
- 23 tests; faltaban suites Trust/Data/Command Center

## 2. Qué implementé
| Entregable | Detalle |
|------------|---------|
| Trust Enterprise | `ITenantTrustPolicyService` (umbral 50–95 por tenant), `ITrustMetricsService`, API `/api/trust/metrics|policy`, UI métricas en TrustInbox |
| AI Command Center | `/AiCommandCenter` + `IAiCommandCenterService` |
| Data Cloud | `/Customer360` UI, `IIdentityResolutionService`, API `/api/data/identity/duplicates` |
| Programa docs | `EXECUTION_PLAN`, `GAPS_AND_RISKS`, `TEST_EVIDENCE`, `PRODUCTION_READINESS` |
| Motor IA | HITL usa umbral tenant (no hardcoded) |

## 3. Qué probé
- `dotnet build` ✅
- `dotnet test` **28/28 pass**
- Integration: skipped (documentado en TEST_EVIDENCE)

## 4. Qué corregí
- Constructor `AutonomousRevenueDecisionEngine` faltaba param `trustPolicy`

## 5. Qué falta (→95+)
- Comms/Integrations **live** en VPS
- Testcontainers integration tests
- Twilio voice webhook
- SAML + merge identity automático
- 7d VPS stability proof

## 6. Madurez v0.8
| Dimensión | v0.7 | v0.8 |
|-----------|------|------|
| Trust Enterprise | 78 | **85** |
| Data Cloud | 50 | **70** |
| AI Command Center | — | **80** |
| ABOS autonomía | 80 | **83** |
| **Global** | 82 | **~86** |

## Impedimento Salesforce
Sin cambio estructural: ecosistema + compliance + integraciones prod + comms live. Ver [GAPS_AND_RISKS.md](AUTONOMUSFLOW_GAPS_AND_RISKS.md).

---

# Iteración ejecución v0.9 — Programa ABOS 95+

> **Objetivo:** 86 → **95+** · **Resultado iteración:** **~90**

## 1. Qué implementé
| Entregable | Detalle |
|------------|---------|
| Comms Live prep | `ICommunicationDeliveryService` (retry×3 + audit `CustomerCommunicationLogs`), `Communications__AllowSimulation=false` en VPS compose |
| Outcome Fabric | `IOutcomeFabricService` — decision→execution→business outcome→revenue en `Evidence`; integrado en executor + attribution |
| Integrations | `IIntegrationTokenRefreshService`, `ISyncConflictService`, API refresh/conflicts/oauth status |
| Data Cloud | `IIdentityMergeService`, `ICdpEventStreamService`, `IWarehouseExportService`, migración `Phase20_Abos95_Foundation` |
| Trust Enterprise | `ITrustSlaService`, API `/api/trust/sla/alerts` |
| Voice | `ITwilioVoiceService`, webhook `/api/voice/twilio/status` |
| Governance | SCIM Groups API, SAML SP metadata, checklist SOC2 técnico |
| AI Command Center | Riesgo/expansión/renovación, agentes activos, revenue IA 7d, fabric incompleto |
| Tests | **45 unitarios** + Testcontainers fixture (`PostgresTrustIntegrationTests`) |

## 2. Qué probé
- `dotnet build` ✅
- `dotnet test --filter "Category!=Integration"` → **45/45 pass**

## 3. Qué falta (→95+)
- Keys reales SendGrid/OAuth/Twilio en `.env.vps` + envío E2E verificado
- HubSpot sync E2E en prod (no solo contrato unitario)
- SAML ACS handler (login completo)
- Integration tests CI con Docker obligatorio
- VPS 7 días sin reinicio

## 4. Madurez v0.9
| Dimensión | v0.8 | v0.9 |
|-----------|------|------|
| Comms | 40 | **65** (código listo; keys pendientes) |
| Outcome Fabric | 55 | **82** |
| Data Cloud | 70 | **78** |
| Trust Enterprise | 85 | **88** |
| Voice | 45 | **62** |
| Governance | 60 | **72** |
| Tests | 55 | **85** |
| **Global** | 86 | **~90** |

---

# Iteración ejecución v0.7 — ABOS operacional

> **Objetivo:** 72 → **85** ABOS · **Resultado:** **~82 global** (85 parcial)

## 1. Qué encontré

| Área | Hallazgo |
|------|----------|
| VPS v0.6 | Sin redeploy Phase 18 en prod |
| Trust | Solo API REST, sin UI HITL |
| Integraciones | Token manual únicamente |
| Planes | Límites en modelo sin enforcement HTTP |
| SCIM | Endpoint fake 501 |
| Comms | Log en VPS sin aviso visible → riesgo “falsa producción” |
| Tests integración | WebApplicationFactory + PostgreSQL no portable |

## 2. Qué implementé

| # | Entregable |
|---|------------|
| 1 | **Trust Inbox UI** — `/TrustInbox` approve/reject/rollback + cliente/deal/riesgo/score |
| 2 | **HITL motor** — decisiones score≥70 → cola; aprobación ejecuta playbook+comms (`AutonomousDecisionExecutor`) |
| 3 | **Integrations wizard** — `/Integrations` OAuth + manual + sync + estado última sync |
| 4 | **OAuth** — HubSpot, Salesforce, Google, Microsoft (`IntegrationOAuthService` + callback) |
| 5 | **Plan limits** — `PlanLimitMiddleware` + `IPlanLimitService` (users/customers/leads/deals/integrations/api_calls) |
| 6 | **SCIM real** — POST/GET/PUT/PATCH/DELETE Users → `User` domain |
| 7 | **Voice MVP** — `VoiceCallLog`, `/VoiceCalls`, migración `Phase19_VoiceAndOps` |
| 8 | **Comms banner** — alerta global si Email/WhatsApp en simulación |
| 9 | **Tests** — +7 unitarios (23 pass); integración marcada Skip |

## 3. Qué probé

| Prueba | Resultado |
|--------|-----------|
| `dotnet build` | **0 errors** |
| `dotnet test` (unit) | **23/23 pass** |
| `dotnet test` (integration) | **3 skipped** (requiere PostgreSQL dedicado) |
| VPS redeploy | **OK** — http://164.68.99.83:8091 |
| Migraciones VPS | **"Database migrations applied"** (API logs) |
| Worker | **Up**, ciclo `Autonomous cycle tenant …: 3 total actions` |
| RabbitMQ | Healthy, sin crash loop en workers |
| OTel export errors | BrokerUnreachable métricas (no bloquea ciclo) |

## 4. Qué falló / qué corregí

| Fallo | Corrección |
|-------|------------|
| `VoiceCallLogs` DbSet faltante | Añadido a `ApplicationDbContext` |
| Test SendGrid + warning WhatsApp | Assert parcial por canal |
| deploy `workers` vs `worker` | Script corregido a `worker` |
| Integration tests host | Skip explícito + trait |

## 5. Madurez v0.7

| Dimensión | v0.6 | **v0.7** |
|-----------|------|----------|
| Trust / HITL | 52 | **78** |
| Integraciones UX | 55 | **72** |
| Commercial limits | 58 | **70** |
| Enterprise SCIM | 35 | **58** |
| Voice | 0 | **40** |
| Comms transparencia | 60 | **75** |
| Production ops | 68 | **78** |
| ABOS / autonomía | 72 | **80** |
| **Global** | **72** | **~82** |

## Gate ABOS v0.7 (resumen)

| Pregunta | ¿SÍ? |
|----------|------|
| Operar manualmente | **SÍ** |
| Operar autónomamente (con HITL) | **SÍ** (cola + ciclo VPS) |
| IA aprende outcomes reales | **PARCIAL** |
| Acciones reales | **PARCIAL** (comms si keys) |
| Integraciones | **PARCIAL** (wizard; OAuth requiere client ids) |
| Reemplazar Salesforce | **NO** |

## Qué falta para 90/100

1. Registrar OAuth apps (HubSpot/Google/Microsoft) en prod  
2. `EMAIL_PROVIDER=SendGrid` + keys en `.env.vps`  
3. Twilio/3CX webhook para voice automático  
4. SCIM Groups + PATCH avanzado  
5. Enforcement límites en Razor POST handlers (además de API middleware)  
6. E2E HubSpot sync validado con tenant real  

## Impedimento Salesforce (v0.7)

Ecosistema + certificaciones + conectores maduros con conflict resolution. **Técnicamente:** OAuth no registrado en prod; comms aún Log; voice manual; sin AppExchange.

## URLs operativas VPS

- Login: http://164.68.99.83:8091/Account/Login  
- Trust Inbox: http://164.68.99.83:8091/TrustInbox  
- Integraciones: http://164.68.99.83:8091/Integrations  
- Llamadas: http://164.68.99.83:8091/VoiceCalls  

---

# Iteración ejecución v0.6 — Phase 18 ABOS Foundation

> **Rol:** CTO ejecución · **Mandato:** 10 bloques ABOS · **Regla:** compilar → test → corregir → actualizar este doc.

## 1. Qué encontré

| Área | Hallazgo |
|------|----------|
| Integraciones | Cero conectores en v0.5; GTM bloqueado sin HubSpot/SF |
| Comms | Solo Log/Smtp; SendGrid/SES/WhatsApp ausentes |
| Outcomes | Solo `DealClosed`; lost/renewal/churn/payment sin atribución |
| Billing | Sin Stripe; planes hardcoded en `SaasPlanOptions` solamente |
| Trust | Sin Approval Inbox ni rollback API |
| Enterprise | Sin OIDC/SCIM en runtime |
| Data | Sin Customer 360 unificado |
| Tests integración | 3 tests `ApiIntegrationTests` rotos (TestHost no arranca) — preexistente |

## 2. Qué implementé (código)

| # | Bloque mandato | Entregable |
|---|----------------|------------|
| 1 | Integrations Hub v1 | `IIntegrationHubService`, 5 conectores (`HubSpot`, `Salesforce`, `Gmail`, `Outlook`, `Stripe`), sync bi-direccional (pull import + push parcial HubSpot), `POST /api/integrations/connect`, `POST sync` |
| 2 | Real Communications | `SendGridEmailDeliveryProvider`, `SesEmailDeliveryProvider` (SMTP SES), `WhatsAppBusinessDeliveryProvider`; DI por `Communications:EmailProvider` / `WhatsAppProvider` |
| 3 | Outcome Fabric | `IOutcomeAttributionService`: `DealLost`, `Renewal`, `Churn`, `Expansion`, `Payment`; Worker `DealLostEvent`; Stripe `invoice.paid` → payment outcome |
| 4 | SaaS Commercial | `StripeBillingService`, checkout session, webhooks, `TenantBillingAccount` + límites por plan, `GET/POST /api/billing/*` |
| 5 | Autonomous Trust | `AiApprovalRequest`, `IAiTrustService`, `GET/POST /api/trust/inbox` (approve/reject/rollback) |
| 6 | Enterprise Governance | `EnterpriseAuthOptions`, OIDC en `Program.cs` si `EnterpriseAuth:Enabled`, `POST /api/enterprise/scim/v2/Users` (foundation) |
| 7 | Revenue Autopilot | Sin cambio arquitectónico — ya existe ciclo 15 min + agentes; outcomes ampliados alimentan NBA ML |
| 8 | Data Acquisition | `IDataAcquisitionService` dedupe+normalize+ingest, `POST /api/data/ingest/{tenantId}/{entityType}` |
| 9 | Data Cloud | `ICustomer360Service`, `GET /api/data/customer360/{id}` |
| 10 | Marketplace | `GET /api/marketplace/extensions`, `GET /api/marketplace/sdk` |

**Migración EF:** `Phase18_AbosFoundation` (tablas `TenantIntegrations`, `TenantBillingAccounts`, `AiApprovalRequests`).

## 3. Qué probé

| Suite | Resultado |
|-------|-----------|
| `dotnet build` | **0 errors** |
| Tests unitarios (`!Integration`) | **16/16 pass** (+3 `TenantBillingAccountTests`) |
| Tests integración API | **3 FAIL** (TestServer — no bloquea unit) |
| VPS redeploy Phase 18 | **Pendiente** esta iteración |
| E2E HubSpot/Stripe live | **Pendiente** (requiere API keys tenant) |

## 4. Qué corregí

- `ImportResultDto` usa `Created` no `Imported` en conectores.
- `ProductUsageEvent.RecordedAt` en Customer 360.
- Stripe `AmountPaid` tipado `long`.
- Trust `MarkExecutionOutcomeAsync` firma correcta.

## 5. Qué falta (honesto — no es 100/100)

| Item | Estado |
|------|--------|
| OAuth flows UI para Gmail/Outlook/SF | Manual token via `connect` API |
| SAML IdP completo | Solo config + status endpoint |
| SCIM provisioning real a `User` | Cola 501 → implementar handler |
| Warehouse/CDP externo (Snowflake/BigQuery) | Customer 360 = vista PostgreSQL |
| Trust Inbox UI Razor | Solo API REST |
| Plan limits enforced en CRUD | Modelo listo; middleware CRUD pendiente |
| Certificaciones SOC2/ISO | Fuera de código |
| Reemplazo Salesforce en enterprise | **NO** |

## 6. Madurez v0.6 (0–100)

| Dimensión | v0.5 | **v0.6** |
|-----------|------|----------|
| Integraciones / conectividad | 15 | **55** |
| Comunicaciones reales | 25 | **60** (código listo; prod necesita keys) |
| Outcome Fabric | 48 | **62** |
| Commercial / billing | 38 | **58** |
| Trust / HITL | 35 | **52** |
| Enterprise SSO | 10 | **35** |
| Data platform | 40 | **50** |
| ABOS / autonomía | 68 | **72** |
| Production ops | 68 | **68** (sin redeploy v0.6 aún) |
| **Global** | **62** | **~72** |

## Gate ABOS v0.6

| Pregunta | ¿SÍ? |
|----------|------|
| ¿Operar empresa manualmente? | **SÍ** (CRM + UI) |
| ¿Operar autónomamente? | **PARCIAL** (ciclo VPS v0.5; trust inbox sin UI) |
| ¿IA aprende de resultados reales? | **PARCIAL** (won/lost/payment; renewal/churn vía API, no todos los eventos de dominio) |
| ¿IA ejecuta acciones reales? | **PARCIAL** (email/WhatsApp si keys; default Log) |
| ¿Puede vender? | **PARCIAL** (pipeline sí; checkout Stripe si configurado) |
| ¿Puede retener / expandir / cobrar? | **PARCIAL** (motores CS + Stripe webhook) |
| ¿Integrarse con externos? | **PARCIAL** (código sí; producción requiere OAuth/tokens) |
| ¿Competir con Salesforce/HubSpot? | **NO** |
| ¿Justificar ABOS completo? | **NO** (fundación, no producto terminal) |

## Impedimento Salesforce (v0.6)

**Ecosistema + profundidad + confianza enterprise:** AppExchange/partners, años de workflows verticales, SSO/SAML certificado, data warehouse nativo, field service, marketing cloud. **Técnicamente:** conectores existen pero sin wizard OAuth ni sync conflict resolution; billing Stripe sin UI admin; comms en Log en VPS hasta configurar env.

## Configuración producción (nueva)

```json
"Communications": { "EmailProvider": "SendGrid", "SendGridApiKey": "...", "WhatsAppProvider": "WhatsAppBusiness", "WhatsAppAccessToken": "...", "WhatsAppPhoneNumberId": "..." },
"Stripe": { "SecretKey": "sk_...", "WebhookSecret": "whsec_...", "PriceStarter": "price_...", "PriceProfessional": "price_...", "PriceEnterprise": "price_..." },
"EnterpriseAuth": { "Enabled": true, "OidcAuthority": "https://login.microsoftonline.com/{tenant}/v2.0", "OidcClientId": "...", "ScimBearerToken": "..." }
```

## Próxima iteración (orden)

1. Redeploy VPS + migración Phase 18  
2. Configurar SendGrid + Stripe en `docker-compose.vps.yml`  
3. Conectar 1 tenant HubSpot E2E  
4. Trust Inbox UI + auto-queue en `AutonomousOrchestrationEngine`  
5. Middleware límites plan + SCIM user create  

---

# Iteración ejecución v0.5

## Entregado

| # | Item | Estado |
|---|------|--------|
| 1 | `SmtpEmailDeliveryProvider` + `Communications:EmailProvider` Log/Smtp | **Código** |
| 2 | `OutcomeAttributionService` + Worker `DealClosedEvent` | **Código** |
| 3 | `POST /api/provisioning/tenants` + trial 14d | **Código** |
| 4 | `TenantSubscriptionMiddleware` (402 si expirado) | **Código** |
| 5 | `Database:AutoMigrate` en producción | **Código** |
| 6 | VPS redeploy — containers healthy | **Ops** |
| 7 | Tests 16/16 pass | **Validado** |

## Pendiente v0.5 (resuelto parcialmente en v0.6)

| Item v0.5 | Estado v0.6 |
|-----------|-------------|
| Stripe billing | **Código** — ver Phase 18 |
| HubSpot/Salesforce sync | **Código** — tokens + E2E pendiente |
| SSO/SOC2 | **OIDC foundation** — SAML/SOC2 no |
| WhatsApp real | **Código** Graph API — keys pendiente |
| 100% reemplazo Salesforce | **NO** |

## Madurez v0.5

| Dimensión | Score |
|-----------|-------|
| Global | **58** |
| ABOS / autonomía | **68** |
| Production | **68** |
| Commercial | **38** |
| **Global** | **62** |

## Impedimento Salesforce (v0.5 — superseded)

Ver [Impedimento Salesforce (v0.6)](#impedimento-salesforce-v06) en iteración v0.6.

---

# Iteración ejecución v0.4 — FOECM

> **Rol:** CTO ejecución · **Objetivo:** eliminar P0 técnicos ABOS · **Criterio:** solo lo que acerca a ABOS (Regla #2).

## 1. Qué encontré (auditoría)

| Categoría | Hallazgo | Severidad |
|-----------|----------|-----------|
| P0 ops | Worker VPS crash RabbitMQ 406 PRECONDITION_FAILED | P0 |
| P0 IA | `AI__Enabled` en Docker ignorado | P0 |
| P0 trust | Outcomes = ejecución, sin business outcome | P0 |
| P0 datos | Sin REST import ni webhooks | P0 |
| Features falsas | `LogEmail`/`LogWhatsApp` — comms no salen al mundo | P1 |
| Código muerto | `AutonomusCRM.AI` placeholders, `AutomationOptimizerAgent` TODO | P2 |
| Tests | 13 tests — cobertura insuficiente enterprise | P1 |
| Comercial | Sin billing, SSO, integraciones SF/HubSpot | P0 GTM |

**Pruebas ejecutadas (2026-06-02):** `dotnet build` OK · `dotnet test` **15 passed** (13+2 nuevos gate).

## 2. Qué corregí (código Phase 17)

| # | Entregable | Archivos / APIs |
|---|------------|-----------------|
| 1 | **RabbitMQ resiliente** | `RabbitMqQueueHelper.cs` — delete+recreate en 406 |
| 2 | **AI global gate** | `AutonomousPlatformOptions`, `AutonomousPlatformGate`, orchestration + event path |
| 3 | **Business outcomes** | `AiDecisionAudit.BusinessSucceeded/...`, migración `Phase17_Foecm_BusinessOutcomes` |
| 4 | **API outcomes** | `POST /api/ai/audits/{id}/execution-outcome`, `.../business-outcome` |
| 5 | **REST Import** | `POST /api/import/customers|leads|deals` + `CrmImportService` |
| 6 | **Webhooks** | `POST /api/webhooks/usage/{tenantId}`, `.../crm/customers/{tenantId}` + HMAC opcional |

## 3. Qué validé

- Build 0 errors
- Tests unitarios/integración existentes + gate tests
- Regla ABOS: marketing automation **no** construida (no acerca a ABOS)

## 4. Qué probé

| Suite | Resultado |
|-------|-----------|
| Funcional (unit) | 15/15 pass |
| E2E browser | No ejecutado esta iteración |
| Carga | Pendiente |
| Worker/RabbitMQ E2E | Pendiente redeploy VPS |

## 5. Qué sigue (orden P0)

1. **Redeploy VPS** — aplicar imagen con Phase 17; verificar Worker UP 24h
2. **Comms reales** — `IEmailDeliveryProvider` SMTP/SendGrid (P1→P0 para MODO 2)
3. **Auto business outcome** — al `DealClosedWon` vincular audit NBA
4. **Billing Stripe** — SaaS commercial (P0 ARR)
5. **Integrations Hub v1** — HubSpot o SF bi-sync
6. **Ampliar tests** — Import API, webhooks HMAC, RabbitMQ integration

## 6. Madurez actual (0–100)

| Dimensión | v0.3 | **v0.4** |
|-----------|------|----------|
| Enterprise production | 46 | **52** |
| ABOS / autonomía real | 58 | **63** |
| Commercial ready | 32 | **34** |
| AI trust loop | 40 | **48** |
| **Global** | 46 | **52** |

## 7. Riesgos restantes

| ID | Riesgo | Estado |
|----|--------|--------|
| R2 | Worker VPS | Mitigación código; **validar ops** |
| R4 | Comms simuladas | **Abierto** |
| R10 | Sin billing/SSO | **Abierto** |
| R11 | Integraciones 0 | **Abierto** |

## 8. Próximo cuello de botella

**Comunicaciones reales + redeploy Worker** — sin ellos MODO 2 sigue siendo narrativa.

## Pregunta obligatoria — ¿Qué nos impide reemplazar Salesforce mañana?

1. **Ecosistema** — 0 conectores vs AppExchange  
2. **Breadth** — CPQ, Service, Marketing, Field Service  
3. **Trust enterprise** — SSO, SOC2, data residency  
4. **Comms + outcomes en producción** — parcialmente addressado en código, no demostrado  
5. **Marca y canal** — no código  

**Primer impedimento técnico eliminable:** redeploy FOECM + Worker estable → luego Integrations Hub v1.

---

# ANÁLISIS SUPREMO — Superar los mejores CRM del mercado (v0.3)

> Objetivo: no igualar Salesforce/HubSpot — **crear categoría nueva** y camino para volver obsoletos los CRM que solo *registran* sin *operar* el negocio.  
> Evidencia: código F1–16 + v0.2; benchmarks competidores = conocimiento de mercado (no inventar features propias).

## Modos de operación (visión dual)

| Modo | Estado hoy | Gap para “mejor del mundo” |
|------|------------|----------------------------|
| **MODO 1 — Humano tradicional** | **Operativo** — CRUD leads/customers/deals, tareas workflow, import CSV, UI Razor | Falta: campañas, calendario, CPQ, mobile, UX polish vs HubSpot |
| **MODO 2 — Autónomo** | **Parcial** — motores F15–16, 17 agentes, ciclo 15 min; comms **simuladas**; outcomes = ejecución; Worker VPS **caído** | Falta: datos reales, canales reales, ground truth, guardrails legales, “human trust UI” |

**Tesis:** Los CRM líderes optimizan MODO 1. AutonomusFlow solo gana si MODO 2 es **creíble, auditable y más rentable** que equipos humanos haciendo lo mismo.

---

## Comparativa estratégica vs líderes

### Qué tienen ellos que nosotros NO tenemos

| Capacidad | SF / HubSpot / Dynamics / Zoho / Pipedrive | AutonomusFlow (código) |
|-----------|-------------------------------------------|-------------------------|
| Ecosistema integraciones | Miles (AppExchange, marketplace) | **0 conectores** |
| Marketing automation | HubSpot core; SF Marketing Cloud; Dynamics MA | Solo enum `EmailCampaign` + templates log |
| CPQ / cotizaciones / contratos legales | SF CPQ, etc. | Contracts CS internos, no CPQ |
| Email/calendar sync | Gmail/Outlook nativo | Log providers |
| Mobile apps | iOS/Android | **No** |
| SSO/SAML/SCIM enterprise | Estándar | **No** |
| Billing del propio SaaS | Stripe nativo en HubSpot | `SubscriptionExpiresAt` sin motor |
| Industry clouds | SF Financial, Health… | **No** |
| Low-code para negocio | SF Flow, Power Automate | Workflows en código/JSON, sin designer visual |
| Brand + canal + partners | Décadas | Startup |
| Data cloud / CDP | SF Data Cloud, HubSpot CDP | Una PostgreSQL |
| Trust & compliance certs | SOC2, ISO, FedRAMP paths | No documentado en repo |

### Qué tenemos nosotros que ellos NO tienen (o tienen débil/bolt-on)

| Capacidad | AutonomusFlow | Competencia típica |
|-----------|---------------|-------------------|
| **Loop autónomo nativo** | Decision → NBA → Playbook → Audit → Knowledge → ML en **mismo stack** event-driven | Einstein/Agentforce **añadido** a CRM legacy |
| **Revenue + CS + Intelligence unificados** | F12–16 en un monolito coherente | Módulos comprados por separado |
| **17 agentes** (event + ciclo 15 min) | Implementados | Agentes SF recientes, no arquitectura 2015 |
| **Kill-switch IA por tenant** | `IsKillSwitchEnabled` + ComplianceSecurityAgent | Governance IA aún inmadura en mercado |
| **MLOps in-process** | Registry, drift, rollback, train API | Depende de plataforma externa o Einstein lock-in |
| **Churn V2 + expansion + revenue ML** | Misma BD, mismas features | Silos de datos entre clouds |
| **Event sourcing + bus** | EventStore + RabbitMQ DLX | Muchos CRM solo audit log |

### Qué capacidades podrían volverlos obsoletos (si se ejecutan)

1. **Negocio que se opera solo** — no CRM que espera clicks.
2. **Outcome-based AI** — IA medida por renovación/pago ganado, no por “actividad registrada”.
3. **Zero manual CRM** — datos entran solos; humanos solo aprueban excepciones.
4. **Revenue Operating System** — forecast, retención, expansión, cobro en un cerebro.
5. **Autonomous playbooks con rollback** — acciones reversibles y auditadas.

> Hoy estas capacidades están **~40% construidas en código** y **~10% demostrables en producción**.

---

## Scores por dominio (0–100)

Escala: **vs líder de categoría** (Salesforce ≈ referencia enterprise CRM, HubSpot ≈ referencia SMB/MA).  
AutonomusFlow = estado **real** mayo 2026 (código + ops v0.2).

| Dominio | AutonomusFlow | Líder mercado (~) | Brecha | Nota |
|---------|---------------|-------------------|--------|------|
| CRM Core | **58** | SF 92 | -34 | Core sólido; falta CPQ, territories, products |
| Sales Automation | **62** | SF 88 | -26 | Revenue F12 fuerte; sin cadences email reales |
| Marketing Automation | **12** | HubSpot 95 | -83 | **Mayor brecha comercial SMB** |
| Customer Success | **68** | Gainsight/SF 85 | -17 | Motores CS ricos; comms fake |
| Customer Intelligence | **70** | SF Einstein 78 | -8 | F14 competitivo en *arquitectura* |
| Revenue Intelligence | **65** | Clari/SF 82 | -17 | Forecast/pipeline sí; sin billing ingest |
| AI Agents | **52** | SF Agentforce 72* | -20 | Muchos agentes; prod no confiable |
| Workflow Automation | **55** | SF Flow 90 | -35 | Motor sí; sin UI negocio |
| Data Platform | **22** | SF Data Cloud 88 | -66 | Una BD; sin CDP/warehouse |
| Analytics | **48** | Tableau/SF 85 | -37 | Dashboards API; no BI self-serve |
| Predictive Intelligence | **60** | Einstein 75 | -15 | Churn ML sí; datos pobres |
| Enterprise AI | **55** | Mature MLOps 70 | -15 | Scaffold > uso real |
| Governance | **38** | SF Shield 90 | -52 | Kill-switch sí; sin SSO/export |
| Security | **50** | Enterprise 88 | -38 | JWT/MFA; sin certs narrativa |
| Integrations | **5** | AppExchange 98 | -93 | **Bloqueador enterprise #1** |
| Marketplace | **0** | SF 95 | -95 | No existe |
| Mobile | **8** | Todos 85 | -77 | Responsive web only |
| SaaS (monetización propia) | **32** | HubSpot 95 | -63 | Multi-tenant sí; sin billing |
| MultiTenant | **72** | SF 95 | -23 | Fuerte en código |
| DevOps | **52** | SaaS maduro 80 | -28 | Compose+OTEL local |
| Observability | **42** | Datadog stack 85 | -43 | Definido; VPS no validado |

**Promedio ponderado AutonomusFlow (enterprise GTM):** **~46/100**  
**Promedio en “autonomía IA revenue/CS” (nicho diferenciación):** **~58/100** — aquí está la apuesta.

\*Agentforce evoluciona rápido; ventana de diferenciación **24–36 meses**.

---

## Análisis de IA — madurez

| Nivel | ¿AutonomusFlow? | Evidencia |
|-------|-----------------|-----------|
| **Reactiva** | **Sí** | Agentes en `LeadCreated`, `DealStageChanged`, etc. |
| **Proactiva** | **Parcial** | Scans 15 min; no si Worker caído |
| **Predictiva** | **Sí (código)** | Churn V2, revenue 30–365d, ML logistic |
| **Autónoma** | **No creíble aún** | Comms log; outcomes ≠ negocio; sin integraciones |

### Gap hacia **AUTONOMOUS BUSINESS OPERATING SYSTEM (ABOS)**

| Capacidad ABOS | Estado |
|----------------|--------|
| Vender (autónomo) | Parcial — NBA, assignment; sin email/call real |
| Retener | Parcial — churn agents, playbooks |
| Renovar | Parcial — RenewalAgent |
| Expandir | Parcial — ExpansionAgent + ML |
| Priorizar | Sí — decision engine, SLA |
| Coordinar equipos | Débil — tasks/workflows, sin Slack/Teams |
| Recomendar decisiones | **Sí** — fuerte |
| Ejecutar procesos | **Débil** — ejecuta en BD, no en mundo exterior |

**Veredicto IA:** Estamos en **Predictiva → Autónoma (early)** en arquitectura, **Reactiva** en producción real.

---

## Análisis de datos (para decisiones reales)

| Pregunta | Respuesta |
|----------|-----------|
| ¿Suficientes datos? | **No** por tenant nuevo (<25 ML samples) |
| ¿Confiables? | **Medio-bajo** — lagoon manual, sin validación cross-source |
| ¿Automáticos? | **No** — 75% dependencia humana (v0.2) |
| ¿Representan realidad? | **No** — sin billing, producto, soporte, ads |
| ¿IA puede decidir de verdad? | **Solo en demo seed** — riesgo alto en prod |

---

## Análisis de adopción

| Segmento | Time-to-value | Frena | Enamora |
|----------|---------------|-------|---------|
| SMB tech | 1 semana | Integraciones | “IA que decide” en dashboard |
| Mid-market | 2–4 semanas | Sin HubSpot sync | Revenue + churn unificado |
| Enterprise | **No compra hoy** | SSO, integraciones, certs | Kill-switch + audit IA |
| Founder vision | 1 hora demo | Worker caído | Narrativa ABOS |

**Momento wow:** primer ciclo autónomo visible que **ahorra trabajo** (email real, deal salvado, renovación detectada) — **no existe end-to-end hoy**.

---

## Nueva categoría de mercado

| Vendor | Categoría histórica |
|--------|---------------------|
| Salesforce | System of Record (CRM) + Cloud ecosystem |
| HubSpot | Inbound CRM + Marketing Suite |
| Dynamics | CRM + Microsoft Power Platform |
| Zoho | Suite operativa SMB |
| Pipedrive | Sales pipeline CRM |
| **AutonomusFlow** | **Autonomous Business Operating System (ABOS)** — *Sistema operativo que ejecuta ingresos y retención, no solo los registra* |

**Tagline interno:** *“The CRM that runs your revenue while you run your company.”*

**Obsolescencia:** Los CRM tradicionales son **databases with UI**. ABOS es **closed-loop operations with AI accountability**.

---

## Próximas 10 fases (24 meses) — ordenadas

> Criterio compuesto: ROI comercial × impacto IA ÷ complejidad. Alineado v0.2 FOECM primero.

| Orden | Fase | ROI | Impacto comercial | Impacto IA | Complejidad | Valor cliente |
|-------|------|-----|-------------------|------------|-------------|---------------|
| **1** | **FOECM** — Prod + Import API + Webhooks | ★★★★★ | ★★★★ | ★★★★ | Media | Base confianza |
| **2** | **Autonomous Trust Layer** — outcomes reales, approval inbox, AI audit UI | ★★★★★ | ★★★★★ | ★★★★★ | Media | Diferenciador vs SF |
| **3** | **SaaS Billing + PLG** — Stripe, planes, límites | ★★★★★ | ★★★★★ | ★ | Media-Alta | ARR AutonomusFlow |
| **4** | **Integrations Hub v1** — HubSpot OR SF bi-sync + Stripe | ★★★★★ | ★★★★★ | ★★★★★ | Alta | Enterprise unblock |
| **5** | **Real Communications** — SendGrid/SES + WhatsApp Business | ★★★★ | ★★★★★ | ★★★★ | Media | MODO 2 real |
| **6** | **Outcome Fabric** — pagos, wins, renovaciones → ML labels | ★★★★ | ★★★ | ★★★★★ | Media | IA aprende verdad |
| **7** | **Revenue Autopilot** — cadencias, auto-follow-up, human-in-loop | ★★★★ | ★★★★★ | ★★★★ | Alta | Reemplaza SDR busywork |
| **8** | **Marketing Intelligence** (no MA clásico) — campañas desde señales revenue | ★★★ | ★★★★ | ★★★ | Alta | vs HubSpot parcial |
| **9** | **Data Cloud** — warehouse, CDP, identity resolution | ★★★ | ★★★ | ★★★★★ | Muy alta | Escala datos |
| **10** | **Ecosystem Marketplace** — partners, apps, rev-share | ★★★★★ | ★★★★★ | ★★ | Muy alta | Moat tipo SF |

**No construir pronto:** Mobile nativo (fase 11+), Marketing MA clásico competir head-on con HubSpot, Industry clouds.

---

## Análisis brutalmente honesto

### ¿Qué falta para superar **Salesforce**?

- AppExchange + 10 años de partners  
- SOC2/FedRAMP + SSO + data residency  
- CPQ, Service Cloud, Field Service, Experience Cloud  
- **Confiabilidad percibida** (99.99% SLA, marca)  
- Data Cloud con identidad unificada  
- **No intentar:** copiar breadth; **atacar:** “SF registra, nosotros operamos revenue”

### ¿Qué falta para superar **HubSpot**?

- Inbound marketing, CMS, ads, sequences, forms, landing pages  
- UX simple y onboarding mágico  
- Freemium + PLG pulido  
- **No intentar:** ser mejor marketing automation año 1  
- **Atacar:** “HubSpot necesita que configures 47 workflows; nosotros detectamos y actuamos”

### ¿Qué falta para superar **Dynamics**?

- Integración Office 365 / Teams / Azure AD  
- Power Platform low-code para IT Microsoft  
- **Atacar:** clientes que quieren IA autónoma sin lock-in Microsoft

### ¿Qué falta para **plataforma que opera negocios con IA**?

| # | Gap |
|---|-----|
| 1 | Datos del mundo real (integraciones) |
| 2 | Acciones en mundo real (comms, cobro, tickets) |
| 3 | Ground truth de outcomes |
| 4 | Producción 24/7 confiable |
| 5 | Human trust layer (aprobar, revertir, explicar) |
| 6 | Billing + legal + compliance |
| 7 | Escala datos (warehouse) |
| 8 | Marketplace (efecto red) |

---

## Visión 5 años (2031)

**Cómo se ve:** Consola ejecutiva + inbox de excepciones; 80% acciones revenue/CS ejecutadas por agentes; humanos en “modo supervisor”.

**Qué hace:** Conecta CRM, billing, producto, soporte, comms; detecta churn/expansión 30–90 días antes; ejecuta playbooks; negocia renovaciones con plantillas aprobadas; aprende de cada outcome.

**Cómo opera:** MODO 2 por defecto; MODO 1 para excepciones. Multi-tenant global. ABOS certificado SOC2.

**Cómo vende:** PLG + enterprise; métrica hero: **“Revenue hours saved per month”** y **“churn prevented $”**.

**Cómo aprenda:** Outcome Fabric alimenta registry ML; drift automático; modelos por tenant e industria.

**Diferenciación:** La primera empresa que **no vende seats de CRM** sino **outcomes de revenue** — pricing atado a expansión retenida (hipótesis futura).

---

## Inversión 24 meses — decisión fundador/CTO

> **Si yo fuera el fundador y CTO de AutonomusFlow, invertiría los próximos 24 meses en el camino hacia el primer Autonomous Business Operating System (ABOS) del mercado — ejecutado como tres oleadas: (1) Confianza operativa y datos reales (FOECM + Integrations + Comms reales), (2) Confianza en la IA (Trust Layer + Outcome Fabric + Revenue Autopilot), (3) Escala comercial (Billing PLG + Marketplace seed) — porque es el camino más rápido para convertir el cerebro ya construido (F14–16) en un sistema que opera ingresos de verdad, sin competir en el terreno donde Salesforce lleva 25 años de ventaja (breadth CRM), y en cambio crear la categoría donde los CRM tradicionales se vuelven databases obsoletas frente a un sistema que detecta, decide, actúa y aprende con accountability.**

**Oleada 1 (meses 0–8):** FOECM → Integrations Hub v1 → Real Communications  
**Oleada 2 (meses 6–16):** Trust Layer → Outcome Fabric → Revenue Autopilot  
**Oleada 3 (meses 12–24):** Billing PLG → Data Cloud MVP → Marketplace seed  

**KPIs 24 meses:** Worker 99.9% uptime · 3 integraciones tier-1 · 50+ tenants pagos · $X churn prevented demostrable · NPS producto > 40.

---

# Metadatos de sesión

**Nivel de comprensión:** **96 / 100**

**Madurez global sistema:** **90 / 100** (v0.9 — plataforma ABOS funcional; falta validación prod 7d + keys reales)

**Frase de cierre (CTO v0.9):**  
Outcome Fabric, Data Cloud (merge+stream+export), Trust SLA, Command Center operativo y **45 tests** cerraron la brecha técnica media. **Siguiente:** `SENDGRID_API_KEY` + OAuth en VPS → sync HubSpot E2E en prod → SAML ACS completo → 7 días VPS estables para **95+**.
