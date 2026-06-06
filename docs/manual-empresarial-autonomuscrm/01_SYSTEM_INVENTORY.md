# 01 — Inventario del Sistema AutonomusCRM

**Versión documentada:** código en `main` (commit `f8131e8` y posteriores)  
**Fuente:** análisis estático del repositorio — sin funcionalidades inventadas.

---

## 1. Resumen ejecutivo

AutonomusCRM es una plataforma SaaS de **operaciones de ingresos y clientes** construida en **.NET 9**, con:

- **UI:** Razor Pages (66 páginas routables)
- **API:** Controllers REST autenticados
- **BD:** PostgreSQL 16 (Entity Framework Core + migraciones)
- **Mensajería:** RabbitMQ (event bus)
- **Cache:** Redis
- **Workers:** servicio en background (`AutonomusCRM.Workers`)
- **IA:** módulos LLM (`AutonomusCRM.AI`) + ML/Enterprise AI (`Infrastructure/EnterpriseAI`)

---

## 2. Proyectos de solución

| Proyecto | Propósito |
|----------|-----------|
| `AutonomusCRM.Domain` | Entidades, eventos de dominio, reglas de negocio |
| `AutonomusCRM.Application` | Commands, queries, interfaces, DTOs |
| `AutonomusCRM.Infrastructure` | EF, repositorios, automatizaciones, IA, integraciones |
| `AutonomusCRM.API` | Razor Pages, controllers, middleware, auth |
| `AutonomusCRM.Workers` | Consumo RabbitMQ, agentes, jobs periódicos |
| `AutonomusCRM.AI` | Proveedores LLM, embeddings, agent service |
| `AutonomusCRM.Tests` | Pruebas unitarias e integración |

---

## 3. Entidades de dominio principales

| Entidad | Archivo | Descripción |
|---------|---------|-------------|
| Lead | `Domain/Leads/Lead.cs` | Prospecto comercial |
| Customer | `Domain/Customers/Customer.cs` | Cuenta/cliente |
| Deal | `Domain/Deals/Deal.cs` | Oportunidad de venta (requiere CustomerId) |
| User | `Domain/Users/User.cs` | Usuario del tenant |
| Tenant | (multi-tenant) | Aislamiento por `TenantId` |
| DomainEvent | Event store | Auditoría event-sourcing |

**Estados reales (enums):**

- **LeadStatus:** New, Contacted, Qualified, Converted, Lost, Unqualified
- **CustomerStatus:** Prospect, Lead, Qualified, Customer, VIP, Churned, Inactive
- **DealStatus:** Open, Closed, OnHold, Cancelled
- **DealStage:** Prospecting, Qualification, Proposal, Negotiation, ClosedWon, ClosedLost

---

## 4. Capas de aplicación

### Commands (escritura)
- Leads: Create, Update, Delete, Qualify, BulkUpdateLeadStatus
- Customers: Create, Update, Delete, UpdateCustomerStatus, BulkUpdateCustomerStatus
- Deals: Create, Update, Delete, UpdateDealStage, UpdateDealProbability, Close, Lose, BulkUpdateDealStage
- Users: Create, Update, ToggleUserStatus, AssignRole, MFA, bulk
- Workflows: Create, Update, Delete, Duplicate, AddTrigger/Condition/Action
- Tasks: AssignWorkflowTask, CompleteWorkflowTask

### Queries (lectura)
- GetLeadById, GetLeadsByTenant
- GetCustomerById
- GetDealById, GetDealsByTenant
- GetWorkflowTasks
- Event store / audit queries

### Repositorios (Infrastructure)
`LeadRepository`, `CustomerRepository`, `DealRepository`, `UserRepository`, `WorkflowRepository`, `WorkflowTaskRepository` — con `AsNoTracking`, `SearchPagedAsync`, agregados SQL.

---

## 5. API Controllers (muestra)

Todos requieren autenticación global salvo endpoints explícitos `[AllowAnonymous]`.

| Controller | Área |
|------------|------|
| `LeadsController` | CRUD leads, qualify |
| `CustomersController` | CRUD clientes |
| `DealsController` | CRUD deals, stage |
| `UsersController` | POST admin-only |
| `TenantsController` | POST admin-only |
| `AiController` | ML, NBA, enterprise cycle, governance |
| `RevenueController` | Revenue OS API |
| `TrustController` | Aprobaciones HITL |
| `IntegrationsController` | OAuth, sync |
| `HealthController` | Health check |

---

## 6. Middleware y seguridad

| Componente | Función |
|------------|---------|
| `CommercialWriteAuthorizationMiddleware` | Bloquea escritura UI en Leads/Customers/Deals/Workflows/Policies a roles Admin, Manager, Sales |
| `ApiTenantValidationMiddleware` | Valida JWT TenantId vs request |
| `PlanLimitMiddleware` | Límites de plan SaaS |
| Auth cookie + JWT | Login en `/Account/Login` |
| Policies | RequireAdmin (usado), RequireManager/Sales (definidas, no aplicadas en endpoints) |

---

## 7. Automatización (resumen)

Ver `05_AUTOMATION_CATALOG.md` y `06_AI_CATALOG.md`.

- `DomainEventDispatcher` → WorkflowEngine, OperationalAutomation, RevenueAutomation, RetentionAutomation, AutonomousOrchestration, BusinessMemory
- `Worker.cs` → suscripciones RabbitMQ + scan cada 15 min
- `BusinessMemoryConsolidationWorker` → cada 6 h

---

## 8. Internacionalización

- Idiomas: **español (default)**, **inglés**
- Recursos: `Resources/SharedResource.{es,en}.resx`, `localization-{es,en}.json` (~1069 claves)
- Selector de idioma en layout + cookie de cultura

---

## 9. Despliegue producción (referencia)

- Docker Compose: postgres, redis, rabbitmq, api, workers
- Nginx reverse proxy puerto 8091
- Scripts BD: `ops/database/*.sql`
- Deploy: `deploy/deploy-vps.ps1`, `deploy/apply-db-optimization-vps.ps1`

---

## 10. Páginas Razor (conteo)

| Categoría | Cantidad |
|-----------|----------|
| Páginas routables | 66 |
| Ítems menú lateral | 19 |
| Marketing público | 5 |
| CRUD + Import + Bulk | 20+ |

Inventario detallado de rutas: `04_MENU_MAP.md`.
