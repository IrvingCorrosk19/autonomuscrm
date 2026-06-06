# 15 — Matriz de Capacidades del Sistema

| Capacidad | Implementado | UI | API | Automático | Notas |
|-----------|:------------:|:--:|:---:|:----------:|-------|
| CRUD Leads | ✅ | ✅ | ✅ | — | Paginación SQL |
| Qualify Lead | ✅ | ✅ | ✅ | ✅ | Crea customer+deal+task |
| Convert Lead | ✅ | ✅ | ❌ | Parcial | Solo UI Details |
| CRUD Customers | ✅ | ✅ | ✅ | — | |
| CRUD Deals | ✅ | ✅ | ✅ | — | Requiere CustomerId |
| Kanban Deals | ✅ | ✅ | — | — | |
| Forecast 30/60/90 | ✅ | ✅ | Parcial | — | SQL aggregates |
| Workflow engine | ✅ | ✅ | — | ✅ | Communicate log-only |
| Tasks | ✅ | ✅ | — | ✅ | |
| Lead scoring | ✅ | ✅ | — | ✅ | Worker |
| Email/WhatsApp | ✅ | Parcial | — | ✅ | Requiere config comms |
| Trust HITL | ✅ | ✅ | ✅ | ✅ | |
| ML Churn/Expansion | ✅ | Parcial | ✅ | ✅ | Enterprise cycle |
| LLM chat agents | Parcial | — | Smoke | ❌ | No en workers |
| Customer 360 | ✅ | ✅ | — | — | |
| Customer Success | ✅ | ✅ | — | ✅ | |
| Integrations OAuth | ✅ | ✅ | ✅ | — | HubSpot,SF,Google,MS |
| Voice calls log | ✅ | ✅ | — | — | Manual log |
| Audit event store | ✅ | ✅ | — | — | Export JSON |
| Policies ABAC | ✅ | ✅ | — | — | |
| i18n ES/EN | ✅ | ✅ | — | — | 1069 claves |
| Multi-tenant | ✅ | ✅ | ✅ | — | TenantId scope |
| Billing dashboard | ✅ | ✅ | — | — | |
| SAML/SCIM enterprise | Parcial | — | ✅ | — | EnterpriseAuth |
| Role UI write gate | ✅ | ✅ | ❌ | — | API gap Support/Viewer |
| Paginación server | ✅ | Leads/Customers/Deals/Users | — | — | 50/página |
| DB indexes Phase2 | ✅ | — | — | — | EF + SQL scripts |
| VPS deploy scripts | ✅ | — | — | — | backup+optimize |

**Leyenda:** ✅ completo · Parcial · ❌ no implementado
