# ABOS Truth Audit — Inventory

**Date:** 2026-05-28 · Evidence from codebase scan + build/test

## Real (EXISTE)

| Area | Evidence |
|------|----------|
| Multi-tenant PostgreSQL | `ApplicationDbContext.cs` — 44 DbSets, 30+ query filters |
| Customer360 | `Customer360EnterpriseService.cs`, API `api/data/customer360-enterprise/{id}` |
| Revenue OS UI | `RevenueOsService.GetDashboardAsync`, `Pages/Revenue.cshtml.cs` |
| Business Memory | 10 tables, `BusinessMemoryPipeline`, `BusinessMemoryConsolidationWorker` |
| Trust/HITL | `AiTrustService`, `AiApprovalRequests`, `TrustInbox.cshtml` |
| MFA TOTP | `VerifyMfaCommandHandler` + OtpNet |
| Workers | `Worker.cs` — 11 agent subscriptions |
| Integrations HTTP | HubSpot, Salesforce, Gmail, Outlook, Stripe connectors |

## Fixed this sprint (was PARTIAL/FAKE)

| Area | Was | Now |
|------|-----|-----|
| LLM | `PlaceholderLlmProvider` | `ResilientLlmProvider` + 4 real providers |
| Simulation | Hardcoded impacts | `RevenueSimulationCalculator` |
| Graph confidence | 0.82/0.55/0.78 literals | `GraphConfidenceCalculator` |

## Still PARTIAL

- SAML/SCIM — code exists, not interop-tested
- Policy engine — TODO L67/L81 `PolicyEngine.cs`
- Communications — Log providers default
- Redis/RabbitMQ — fallbacks without env

## Still NO EXISTE

- Load/performance tests
- SOC2/FedRAMP artifacts
- Real ML training pipeline (tables exist, pipeline heuristic)

## Dead / theater

- `EnterpriseBlockerContractTests` — **replaced** with behavior tests
- `MarketplaceCatalogService` — static 4-item array
- `AutomationOptimizerAgent` — registered, not invoked

## TODOs in code (20 matches)

`PolicyEngine.cs`, `AutomationOptimizerAgent.cs`, `ComplianceSecurityAgent.cs`, `EventSourcingService.cs`, `UsersController.cs`

## Tests

- Unit: **164 PASS**
- Integration: **BLOCKED local** (Docker off), CI path fixed
