# Análisis funcional y arquitectónico completo — AutonomusCRM

> **Modo:** Solo análisis (sin cambios de código).  
> **Fecha:** 2026-05-25  
> **Objetivo:** Base para diseñar pruebas funcionales E2E (Browser Tab / usuario real) de toda la aplicación.

---

# 1. Resumen general del sistema

## Nombre de la aplicación
**AUTONOMUS CRM** (AutonomusCRM)

## Propósito del sistema
CRM empresarial orientado a **autonomía operativa**: gestión de leads, clientes, pipeline de deals, usuarios multi-tenant, workflows de automatización, políticas de decisión, agentes IA en background y auditoría basada en eventos de dominio. El producto apunta a un CRM con capacidades event-driven, zero-trust y preparación para IA distribuida.

## Tipo de aplicación
- **Monolito modular** ASP.NET Core 9 que expone:
  - **UI web** (Razor Pages) — interfaz principal para usuarios.
  - **API REST** (`/api/*`) — consumo programático + Swagger en desarrollo.
- **Workers** (`AutonomusCRM.Workers`) — proceso separado con agentes autónomos suscritos al bus de eventos.
- **No es SPA**: navegación server-side con formularios POST y redirects.

## Arquitectura identificada
| Capa | Proyecto | Responsabilidad |
|------|----------|-----------------|
| Dominio | `AutonomusCRM.Domain` | Aggregates, eventos, reglas de negocio |
| Aplicación | `AutonomusCRM.Application` | CQRS (commands/queries), políticas, DTOs |
| Infraestructura | `AutonomusCRM.Infrastructure` | EF Core, repos, RabbitMQ, Redis, engines |
| Presentación | `AutonomusCRM.API` | Controllers + Razor Pages + middleware |
| Background | `AutonomusCRM.Workers` | Hosted services / agentes |
| IA (placeholders) | `AutonomusCRM.AI` + carpeta `/AI` | Interfaces sin proveedor real |

**Patrones:** Clean Architecture, DDD (aggregates + domain events), CQRS custom (`IRequest` / `IRequestHandler`), Event Sourcing parcial (tabla `DomainEvents`), multi-tenancy por `TenantId`.

## Tecnologías utilizadas
| Área | Tecnología |
|------|------------|
| Runtime | .NET 9 |
| Web | ASP.NET Core (Controllers + Razor Pages) |
| ORM | Entity Framework Core 9 + Npgsql |
| BD | PostgreSQL 16 |
| Cache | Redis 7 (fallback MemoryCache) |
| Mensajería | RabbitMQ 3 (fallback InMemoryEventBus) |
| Auth | JWT Bearer + Cookies (“Smart” scheme) |
| Passwords | BCrypt |
| Logging | Serilog (consola + archivo rotativo) |
| Contenedores | Docker Compose (local + VPS) |
| CI | GitHub Actions (build + tests + Postgres) |

## Frameworks y librerías relevantes
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.AspNetCore.Authentication.Cookies`
- `Microsoft.AspNetCore.RateLimiting` (200 req/min global)
- `Swashbuckle.AspNetCore` (Swagger, solo Development)
- `BCrypt.Net-Next`
- `RabbitMQ.Client`
- `StackExchange.Redis` (opcional)

## Base de datos utilizada
**PostgreSQL** — base `autonomuscrm`.

**DbSets principales** (`ApplicationDbContext`):
| Tabla / DbSet | Tipo |
|---------------|------|
| `Tenants` | Aggregate |
| `Customers` | Aggregate |
| `Leads` | Aggregate |
| `Deals` | Aggregate |
| `Users` | Aggregate |
| `Workflows` | Entidad aplicación (jsonb triggers/conditions/actions) |
| `Policies` | Entidad aplicación |
| `DomainEvents` | Event store |
| `Snapshots` | Event sourcing snapshots |
| `TimeSeriesMetrics` | Métricas temporales |

Columnas `jsonb`: roles de usuario, settings de tenant, partes de workflow, payloads de eventos.

## Servicios externos detectados
| Servicio | Estado | Uso |
|----------|--------|-----|
| PostgreSQL | Activo | Persistencia |
| Redis | Activo / fallback memoria | Cache, refresh tokens |
| RabbitMQ | Activo / fallback memoria | Event bus, workers |
| OpenAI / LLM | **Placeholder** | Agentes IA, embeddings |
| Email (SendGrid/SES/SMTP) | **No integrado** | Mencionado en DecisionEngine |
| Cloudinary, SignalR, OCR, PDF, Puppeteer, QuestPDF | **No detectados** | — |
| Render (producción) | Fuera de alcance repo | BD producción separada |

## Integraciones
- **Event bus → Workers:** domain events publicados; 7 agentes en `AutonomusCRM.Workers` reaccionan (LeadIntelligence, CustomerRisk, DealStrategy, Communication, DataQualityGuardian, ComplianceSecurity, AutomationOptimizer).
- **WorkflowEngine / PolicyEngine / DecisionEngine:** motores internos; ejecución parcial / simulada en varios casos.
- **Health checks:** `/health`, `/health/ready`, `/health/live` + página `/Support`.

## Autenticación
| Mecanismo | Detalle |
|-----------|---------|
| Esquema “Smart” | Bearer JWT si header `Authorization: Bearer`; si no, cookie |
| Login UI | `/Account/Login` — email + password + TenantId (autocompletado) |
| Login API | `POST /api/Auth/login` — body: email, password, tenantId |
| Cookie | `access_token` HttpOnly tras login Razor |
| MFA | Soporte en dominio + `POST /api/Auth/verify-mfa` (UI muestra mensaje si requiere MFA) |
| Refresh | `POST /api/Auth/refresh` |
| Logout | `GET/POST /Account/Logout` |
| Anónimo | `/Account/*` (excepto Logout), `/Error`, `/api/Auth/*`, `/api/Health/*` |

## Manejo de roles
**Roles definidos en seed y UI:** `Admin`, `Manager`, `Sales`, `Support`, `Viewer`.

**Políticas ASP.NET** (`AuthorizationPolicies`):
- `RequireAdmin` → solo `Admin`
- `RequireManager` → `Admin` | `Manager`
- `RequireSales` → `Admin` | `Manager` | `Sales`
- `RequireSameTenant` → claim `TenantId` (comparación con recurso **TODO**)

**Hallazgo crítico para pruebas:** Las **Razor Pages no aplican políticas por rol** — solo exigen usuario autenticado. Las políticas se usan en **2 endpoints API** (`CreateUser`, `CreateTenant`). Cualquier rol autenticado puede acceder a toda la UI (Usuarios, Settings, etc.).

---

# 2. Mapa completo de módulos

> **Nota:** Esta aplicación es un **CRM**, no un sistema escolar. No existen módulos de Estudiantes, Profesores, Padres, Escuelas, Notas académicas, Carnets, etc. Los módulos reales se listan abajo.

---

## Módulo: Login / Account

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Account/Login`, `/Account/Logout`, `/Account/AccessDenied` |
| **PageModels** | `LoginModel`, `LogoutModel` |
| **Controlador API** | `AuthController` (`/api/Auth`) |
| **Servicios** | `LoginCommandHandler`, `VerifyMfaCommandHandler`, `RefreshTokenCommandHandler`, `ITokenService`, `IUserRepository`, `ITenantRepository` |
| **Entidades** | `User`, `Tenant` |
| **Acciones** | Login (GET/POST), Logout, MFA verify, Refresh token |
| **Restricciones** | Login anónimo; resto autenticado |
| **Roles** | Todos (login); credenciales demo por rol en Development/VPS seed |

---

## Módulo: Dashboard (Home)

| Campo | Valor |
|-------|-------|
| **Rutas** | `/` (Index), `/Dashboard` |
| **PageModels** | `IndexModel`, `DashboardModel` |
| **Servicios** | `GetLeadsByTenantQuery`, `GetDealsByTenantQuery`, `ITenantRepository` |
| **Entidades** | `Lead`, `Deal`, `Tenant` |
| **Acciones** | Ver KPIs: leads 24h, conversión, revenue estimado, pipeline por etapa |
| **Restricciones** | Autenticado |
| **Roles** | Todos (sin filtro UI) |

**Cálculos en servidor (Index):**
- `ConversionRate` = leads Qualified / total leads × 100
- `EstimatedRevenue` = suma amount deals Open
- `DealsAtRisk` = deals con probability < 50
- Pipeline por `DealStage`

---

## Módulo: Leads

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Leads`, `/Leads/Create`, `/Leads/Edit/{id}`, `/Leads/Details/{id}`, `/Leads/Import`, `/Leads/BulkActions` |
| **PageModels** | `LeadsModel`, `Leads.CreateModel`, `EditModel`, `DetailsModel`, `ImportModel`, `BulkActionsModel` |
| **Controlador API** | `LeadsController` |
| **Servicios** | `CreateLeadCommand`, `UpdateLeadCommand`, `DeleteLeadCommand`, `QualifyLeadCommand`, `BulkUpdateLeadStatusCommand`, `GetLeadsByTenantQuery`, `ILeadRepository` |
| **Entidades** | `Lead` (`LeadStatus`, `LeadSource`) |
| **Acciones** | Listar, filtrar, crear (inline + página), editar, calificar, convertir a cliente, crear deal desde lead, eliminar, import CSV/JSON, bulk update status |
| **Restricciones** | Autenticado; tenant vía `GetDefaultTenantIdAsync()` (primer tenant) |
| **Roles API** | Autenticado (sin rol) |
| **Roles UI** | Todos autenticados |

---

## Módulo: Deals (Pipeline)

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Deals`, `/Deals/Create`, `/Deals/Edit/{id}`, `/Deals/Details/{id}`, `/Deals/Import`, `/Deals/BulkActions` |
| **PageModels** | `DealsModel`, `CreateModel`, `EditModel`, `DetailsModel`, `ImportModel`, `BulkActionsModel` |
| **Controlador API** | `DealsController` |
| **Servicios** | `CreateDealCommand`, `UpdateDealCommand`, `UpdateDealStageCommand`, `UpdateDealProbabilityCommand`, `CloseDealCommand`, `DeleteDealCommand`, `BulkUpdateDealStageCommand`, `GetDealsByTenantQuery` |
| **Entidades** | `Deal` (`DealStatus`, `DealStage`), `Customer` |
| **Acciones** | CRUD, cambiar stage/probability, cerrar deal, import, bulk stage/probability |
| **Restricciones** | Deal requiere `CustomerId` existente |
| **Roles** | Todos autenticados (UI) |

---

## Módulo: Customers (Clientes)

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Customers`, `/Customers/Create`, `/Customers/Edit/{id}`, `/Customers/Details/{id}`, `/Customers/Import`, `/Customers/BulkActions` |
| **PageModels** | `CustomersModel`, `CreateModel`, `EditModel`, `DetailsModel`, `ImportModel`, `BulkActionsModel` |
| **Controlador API** | `CustomersController` |
| **Servicios** | `CreateCustomerCommand`, `UpdateCustomerCommand`, `UpdateCustomerStatusCommand`, `DeleteCustomerCommand`, `BulkUpdateCustomerStatusCommand`, `GetCustomerByIdQuery` |
| **Entidades** | `Customer` (`CustomerStatus`) |
| **Acciones** | CRUD, cambiar status, crear deal desde customer, record contact, delete, import CSV/JSON, bulk status |
| **Roles** | Todos autenticados |

---

## Módulo: Users (Usuarios y Roles)

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Users`, `/Users/Create`, `/Users/Edit/{id}`, `/Users/Import`, `/Users/BulkActions`, `/Users/Roles` |
| **PageModels** | `UsersModel`, `CreateModel`, `EditModel`, `ImportModel`, `BulkActionsModel`, `RolesModel` |
| **Controlador API** | `UsersController` |
| **Servicios** | `CreateUserCommand`, `UpdateUserCommand`, `ToggleUserStatusCommand`, `AssignRoleCommand`, `RemoveRoleCommand`, `EnableMfaCommand`, `BulkUpdateUserStatusCommand` |
| **Entidades** | `User` (Roles jsonb) |
| **Acciones** | Listar, crear, editar, asignar/quitar rol, activar/desactivar, import, bulk activate/deactivate, vista resumen roles |
| **Restricciones API** | `POST /api/Users` requiere `RequireAdmin` |
| **Restricciones UI** | **Ninguna por rol** — cualquier usuario autenticado puede gestionar usuarios |
| **Roles disponibles UI** | Admin, Manager, Sales, Support, Viewer |

---

## Módulo: Workflows

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Workflows`, `/Workflows/Create`, `/Workflows/Edit/{id}`, `/Workflows/Import` |
| **PageModels** | `WorkflowsModel`, `CreateModel`, `EditModel`, `ImportModel` |
| **Controlador API** | `WorkflowsController` (solo GET list/detail) |
| **Servicios** | `CreateWorkflowCommand`, `UpdateWorkflowCommand`, `DeleteWorkflowCommand`, `DuplicateWorkflowCommand`, `AddWorkflowTrigger/Condition/ActionCommand`, `IWorkflowEngine` |
| **Entidades** | `Workflow`, `WorkflowTrigger`, `WorkflowCondition`, `WorkflowAction` |
| **Acciones** | Crear, editar, duplicar, eliminar, añadir trigger/condition/action, import JSON |
| **Roles** | Todos autenticados |

---

## Módulo: Policies (Políticas)

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Policies`, `/Policies/Create`, `/Policies/Edit/{id}`, `/Policies/Import` |
| **PageModels** | `PoliciesModel`, `CreateModel`, `EditModel`, `ImportModel` |
| **Servicios** | `CreatePolicyCommand`, `UpdatePolicyCommand`, `DeletePolicyCommand`, `DuplicatePolicyCommand`, `IPolicyEngine` |
| **Entidades** | `Policy` (expression, name, tenant) |
| **Acciones** | CRUD, duplicar, import |
| **Roles** | Todos autenticados |

---

## Módulo: Agents (Agentes IA)

| Campo | Valor |
|-------|-------|
| **Ruta** | `/Agents` |
| **PageModel** | `AgentsModel` |
| **Servicios** | `GetAgentConfigQuery`, `UpdateAgentConfigCommand`; workers: 7 agentes hosted |
| **Entidades** | Config en tenant settings / agent config store |
| **Acciones** | Ver lista estática de agentes, editar config JSON por agente |
| **Roles** | Todos autenticados |
| **Nota** | Estado “Active” y LastRun son **informativos/simulados** en UI; IA real es placeholder |

---

## Módulo: Audit (Auditoría)

| Campo | Valor |
|-------|-------|
| **Ruta** | `/Audit` |
| **PageModel** | `AuditModel` |
| **Servicios** | `GetAuditEventsQuery`, `IEventStore` |
| **Entidades** | `DomainEventRecord` |
| **Acciones** | Filtrar por tipo/fecha, listar eventos (max 1000), export JSON |
| **Roles** | Todos autenticados |

---

## Módulo: Settings (Configuración)

| Campo | Valor |
|-------|-------|
| **Ruta** | `/Settings` |
| **PageModel** | `SettingsModel` |
| **Servicios** | `GetSystemSettingsQuery`, `UpdateSystemSettingsCommand`, `UpdateTenantCommand` |
| **Entidades** | `Tenant`, settings dictionary |
| **Acciones** | Actualizar tenant, settings JSON, export/import config, restore defaults |
| **Roles** | Todos autenticados |

---

## Módulo: Support (Soporte / Salud)

| Campo | Valor |
|-------|-------|
| **Ruta** | `/Support` |
| **PageModel** | `SupportModel` |
| **Servicios** | `HealthCheckService` |
| **Acciones** | Mostrar estado Database / EventBus / Cache |
| **Roles** | Todos autenticados |

---

## Módulo: API REST (transversal)

| Controlador | Prefijo | Endpoints clave |
|-------------|---------|-----------------|
| Auth | `/api/Auth` | login, verify-mfa, refresh |
| Health | `/api/Health` | health, metrics |
| Users | `/api/Users` | create (Admin), get, enable-mfa |
| Tenants | `/api/Tenants` | create (Admin), get |
| Customers | `/api/Customers` | create, get, update status |
| Leads | `/api/Leads` | create, list, get, qualify |
| Deals | `/api/Deals` | create, list, get, stage, close |
| Workflows | `/api/Workflows` | list, get |
| Metrics | `/api/Metrics` | timeseries CRUD |

**Stubs API:** Varios `GET {id}` devuelven placeholder o TODO (GetUser, GetCustomer, GetLead, GetDeal parcial).

---

## Módulo: Workers (Background — no UI directa)

| Agente | Eventos suscritos (conceptual) |
|--------|-------------------------------|
| LeadIntelligenceAgent | LeadCreated |
| CustomerRiskAgent | CustomerCreated |
| DealStrategyAgent | DealCreated, DealStageChanged |
| CommunicationAgent | CustomerCreated, LeadCreated |
| DataQualityGuardian | Todos |
| ComplianceSecurityAgent | IDomainEvent |
| AutomationOptimizerAgent | Periódico |

---

# 3. Flujo funcional completo por módulo

## 3.1 Autenticación

### Qué hace el usuario
1. Abre `/Account/Login`
2. Ve Tenant ID precargado + tabla demo (si `Seed:Enabled`)
3. Ingresa email/contraseña → Entrar

### Qué ocurre internamente
1. `LoginModel.OnPostAsync` → `LoginCommandHandler`
2. Si `TenantId` vacío, busca usuario por email en todos los tenants
3. BCrypt verifica password; MFA branch si habilitado
4. `TokenService` genera JWT + refresh token (`RefreshTokenService` → Redis/memoria)
5. Cookie auth + cookie `access_token` HttpOnly
6. Redirect a `/`

### Tablas
- `Users`, `Tenants`

### Validaciones
- Usuario activo; password hash; tenant resuelto

### AJAX
- **Ninguno** — form POST clásico

---

## 3.2 Dashboard

### Flujo
1. Usuario autenticado entra a `/`
2. `GetDefaultTenantIdAsync()` → primer tenant (o crea “Default Tenant”)
3. Carga leads y deals del tenant
4. Calcula métricas en memoria

### Tablas
- `Leads`, `Deals`, `Tenants`

### Riesgo E2E
- **No usa TenantId del JWT** — siempre primer tenant de BD

---

## 3.3 Leads — flujo completo de ventas

### Flujo típico
1. **Listar** `/Leads` — filtros query: search, status, source
2. **Crear** — form en lista o `/Leads/Create` → `CreateLeadCommand` → evento `LeadCreated`
3. **Detalle** `/Leads/Details/{id}`
4. **Calificar** → `QualifyLeadCommand` → status Qualified
5. **Convertir a cliente** → `CreateCustomerCommand` + `lead.ConvertToCustomer(customerId)`
6. **Crear deal** → busca/crea customer por email → `CreateDealCommand`
7. **Eliminar** → `DeleteLeadCommand`

### Tablas
- `Leads`, `Customers`, `Deals`, `DomainEvents`

### Servicios
- Handlers CQRS + repositorios + `IDomainEventDispatcher`

### Workers automáticos
- `LeadIntelligenceAgent` reacciona a creación de lead (placeholder IA)

### Validaciones dominio
- Email/nombre requeridos en create; transiciones de status en aggregate

---

## 3.4 Customers

### Flujo
1. Listar / filtrar `/Customers`
2. Crear / editar (status: Prospect → Customer → VIP → Churned, etc.)
3. **Details:** crear deal, record contact, delete
4. **Import:** CSV (Name,Email,Phone,Company) o JSON array
5. **BulkActions:** POST a `/Customers/BulkActions` con `customerIds` + `action=updateStatus`

### Tablas
- `Customers`, `DomainEvents`

---

## 3.5 Deals (Pipeline)

### Flujo
1. Listar pipeline `/Deals` — filtros stage, status
2. Crear deal (requiere customer)
3. **Edit:** title, amount, stage, probability, expected close date
4. **Details:** update probability, update stage, close deal, delete
5. **Bulk:** bulk stage / probability

### Cálculos
- Probabilidad por defecto según stage en dominio (`Deal.GetDefaultProbabilityForStage`)
- Dashboard agrega montos por stage

### Tablas
- `Deals`, `Customers`

### Eventos
- `DealCreated`, `DealStageChanged`, `DealClosed`, etc.

---

## 3.6 Users

### Flujo Admin (UI sin restricción real)
1. `/Users` — listado con búsqueda
2. `/Users/Create` — POST email, password, nombre
3. `/Users/Edit/{id}` — asignar/quitar rol, toggle active
4. `/Users/Roles` — conteo por rol

### Tablas
- `Users` (Roles jsonb)

---

## 3.7 Workflows

### Flujo
1. Crear workflow (nombre, descripción)
2. Editar: añadir triggers (event type), conditions (expression), actions (type + params)
3. Duplicar / eliminar
4. Import JSON

### Ejecución automática
- `WorkflowEngine` + event bus (parcialmente implementado)

### Tablas
- `Workflows` (jsonb para triggers, conditions, actions)

---

## 3.8 Policies

### Flujo
1. Crear política con expresión
2. Editar / duplicar / eliminar
3. `PolicyEngine` evalúa en runtime (decisiones)

### Tablas
- `Policies`

---

## 3.9 Agents

### Flujo
1. Ver 7 agentes con descripción estática
2. POST actualizar config JSON por agente
3. Workers procesan eventos en background (sin feedback UI en tiempo real)

---

## 3.10 Audit

### Flujo
1. GET con filtros eventType, from, to
2. Lee hasta 1000 eventos de `IEventStore`
3. Export POST → JSON descargable

### Tablas
- `DomainEvents`

---

## 3.11 Settings

### Flujo
1. Ver/editar tenant (name, email, region, timezone)
2. Settings JSON (MfaRequired, KillSwitch, MinConfidence, OperationMode)
3. Export/import config JSON
4. Restore defaults

### Tablas
- `Tenants` (Settings jsonb)

---

## 3.12 Support

### Flujo
1. Muestra resultado health checks: database, eventbus, cache

---

# 4. Mapa de navegación completo

```
Login (/Account/Login) [anónimo]
 │
 └── [autenticado] Layout sidebar (_Layout.cshtml)
      │
      ├── Dashboard (/)
      │     └── KPIs leads/deals/pipeline
      │
      ├── Principal
      │   ├── Leads (/Leads)
      │   │   ├── Create (/Leads/Create)
      │   │   ├── Edit (/Leads/Edit/{id})
      │   │   ├── Details (/Leads/Details/{id})
      │   │   ├── Import (/Leads/Import) [POST]
      │   │   └── BulkActions (/Leads/BulkActions) [POST]
      │   │
      │   ├── Pipeline (/Deals)
      │   │   ├── Create, Edit, Details, Import, BulkActions
      │   │
      │   ├── Clientes (/Customers)
      │   │   ├── Create, Edit, Details, Import, BulkActions
      │   │
      │   └── Soporte (/Support)
      │
      ├── Autonomía
      │   ├── Agentes IA (/Agents)
      │   ├── Workflows (/Workflows)
      │   │   ├── Create, Edit/{id}, Import
      │   └── Políticas (/Policies)
      │       ├── Create, Edit/{id}, Import
      │
      ├── Administración
      │   ├── Usuarios y Roles (/Users)
      │   │   ├── Create, Edit/{id}, Import, BulkActions, Roles
      │   ├── Auditoría (/Audit)
      │   └── Configuración (/Settings)
      │
      ├── Cerrar sesión (POST /Account/Logout)
      │
      └── [no en sidebar]
            ├── /Dashboard (placeholder)
            ├── /Error
            └── /Account/AccessDenied

API paralela (Swagger dev):
/api/Auth, /api/Health, /api/Users, /api/Tenants,
/api/Customers, /api/Leads, /api/Deals, /api/Workflows, /api/Metrics
```

---

# 5. Mapa de roles y permisos

## Roles detectados

| Rol | Email demo | Password demo | Policy ASP.NET |
|-----|------------|---------------|----------------|
| Admin | admin@autonomuscrm.local | Admin123! | RequireAdmin |
| Manager | manager@autonomuscrm.local | Manager123! | RequireManager |
| Sales | sales@autonomuscrm.local | Sales123! | RequireSales |
| Support | support@autonomuscrm.local | Support123! | *(ninguna)* |
| Viewer | viewer@autonomuscrm.local | Viewer123! | *(ninguna)* |

## Matriz acceso esperado vs implementado

| Área | Admin | Manager | Sales | Support | Viewer | **Implementado UI** |
|------|-------|---------|-------|---------|--------|---------------------|
| Dashboard | ✓ | ✓ | ✓ | ✓ | ✓ | Todos autenticados |
| Leads/Deals/Customers | ✓ | ✓ | ✓ | ? | ? | Todos autenticados |
| Users/Roles | ✓ | ? | ✗ | ✗ | ✗ | **Todos autenticados** |
| Settings | ✓ | ? | ✗ | ✗ | ✗ | **Todos autenticados** |
| Workflows/Policies/Agents | ✓ | ✓ | ✗ | ✗ | ✗ | **Todos autenticados** |
| Audit | ✓ | ✓ | ✗ | ✓ | ✗ | **Todos autenticados** |
| API CreateUser/CreateTenant | ✓ | ✗ | ✗ | ✗ | ✗ | RequireAdmin ✓ |

## Permisos / policies / attributes

```text
RequireAdmin      → UsersController.CreateUser, TenantsController.CreateTenant
RequireManager    → registrada, NO usada en controllers/pages
RequireSales      → registrada, NO usada
RequireSameTenant → handler incompleto (solo verifica claim existe)
```

**Global:**
- Controllers: `[AuthorizeFilter]` autenticado
- Razor: `AuthorizeFolder("/")`

---

# 6. Dependencias y servicios críticos

## Servicios principales (Application)

| Servicio | Función |
|----------|---------|
| 50× IRequestHandler | CQRS commands/queries |
| ITokenService | JWT + claims (TenantId, roles) |
| IRefreshTokenService | Tokens en cache 7 días |
| IDomainEventDispatcher | Publica eventos post-save |
| IWorkflowEngine | Ejecuta workflows |
| IPolicyEngine | Evalúa políticas |
| IDecisionEngine | Reglas de decisión |
| IEventSourcingService / IEventStore | Auditoría |

## Repositorios

`TenantRepository`, `CustomerRepository`, `LeadRepository`, `DealRepository`, `UserRepository`, `WorkflowRepository`, `PolicyRepository`, `UnitOfWork`, `TimeSeriesRepository`

## DbContext

`ApplicationDbContext` — único contexto EF; migraciones en `Infrastructure/Persistence/Migrations`

## Infraestructura externa

| Componente | Implementación | Fallback |
|------------|----------------|----------|
| Event bus | RabbitMQEventBus | InMemoryEventBus |
| Cache | RedisCacheService | MemoryCacheService |
| IA | PlaceholderLLMProvider, PlaceholderAgentService | AI:Enabled=false |
| Logging | Serilog file + console | — |

## No detectados

Cloudinary, Email real, SignalR, Storage blob, OCR, PDF (QuestPDF/Puppeteer), pagos, carnets.

---

# 7. Riesgos detectados para pruebas funcionales

| # | Riesgo | Impacto E2E | Severidad |
|---|--------|-------------|-----------|
| 1 | **UI sin autorización por rol** | Tests de permisos fallarán si asumen restricción UI | Alta |
| 2 | **GetDefaultTenantIdAsync() everywhere** | Usuario multi-tenant no aislado; siempre primer tenant | Alta |
| 3 | **RequireSameTenant incompleto** | API puede acceder datos otro tenant si conoce IDs | Alta |
| 4 | **Data Protection keys en contenedor** | Antiforgery inválido tras recreate Docker → POST login falla | Media |
| 5 | **Sin AJAX** | E2E más simple (forms), pero no hay feedback async | Baja |
| 6 | **Redirects silenciosos en errores** | Catch log + redirect sin mensaje visible al usuario | Media |
| 7 | **Import CSV frágil** | Split por coma sin escape; sin validación filas duplicadas | Media |
| 8 | **API GET stubs** | Tests API de detalle pueden no reflejar UI | Media |
| 9 | **Workers desacoplados de UI** | Agentes “Active” en UI no verificables sin logs/RabbitMQ | Media |
| 10 | **IA placeholder** | Flujos “inteligentes” no producen efectos reales | Baja |
| 11 | **Dependencia RabbitMQ/Redis** | Support page Unhealthy si servicios caídos | Media |
| 12 | **Concurrencia bulk** | Bulk updates sin transacción explícita por lote | Baja |
| 13 | **MFA habilitado manual** | Flujo MFA solo vía API verify-mfa | Media |
| 14 | **Rate limiting 200/min** | Stress E2E puede recibir 429 | Baja |
| 15 | **Cálculos dashboard server-side** | Correcto para E2E — verificar contra BD post-acción | Info |

---

# 8. Datos requeridos para pruebas

> Adaptado a dominio **CRM** (no escolar).

## Datos mínimos (smoke)

| Entidad | Cantidad | Notas |
|---------|----------|-------|
| Tenant | 1 | Seed “AutonomusCRM Demo” |
| Users (por rol) | 5 | admin, manager, sales, support, viewer |
| Customers | 3+ | Seed incluye 3 |
| Leads | 3+ | Seed incluye 3 |
| Deals | 1+ | Seed incluye 1 |

## Datos recomendados (regresión funcional)

| Entidad | Cantidad sugerida | Motivo |
|---------|-------------------|--------|
| Tenants | 2 | Probar aislamiento multi-tenant (cuando se implemente) |
| Users | 10 | Mix roles + inactive |
| Customers | 30 | Paginación, filtros, bulk |
| Leads | 50 | Conversión masiva, import CSV |
| Deals | 25 | Todas las stages, open/closed |
| Workflows | 5 | Con triggers/conditions/actions |
| Policies | 5 | Expresiones variadas |
| DomainEvents | 100+ | Audit filters/export |

## Datos por flujo E2E crítico

| Flujo | Precondiciones |
|-------|----------------|
| Login por rol | 5 usuarios seed, tower TenantId |
| Lead → Customer → Deal | Lead New, customer no existente con mismo email |
| Qualify lead | Lead status New |
| Bulk customer status | ≥3 customers seleccionados en UI |
| Import CSV | Archivo `Name,Email,Phone,Company` |
| User assign role | Usuario target + rol Support |
| Workflow edit | Workflow existente con id |
| Audit export | ≥1 evento en store |
| Settings export/import | Settings previos en tenant |

## Credenciales de prueba (local/VPS seed)

```
Admin:   admin@autonomuscrm.local   / Admin123!
Manager: manager@autonomuscrm.local / Manager123!
Sales:   sales@autonomuscrm.local   / Sales123!
Support: support@autonomuscrm.local / Support123!
Viewer:  viewer@autonomuscrm.local  / Viewer123!
```

## Entorno de ejecución sugerido

```text
Local:  docker compose up -d postgres redis rabbitmq && dotnet run --project AutonomusCRM.API
VPS:    http://164.68.99.83:8091/Account/Login
BD:     PostgreSQL autonomuscrm (NO Render producción)
```

---

# 9. Matriz de cobertura funcional futura (E2E)

| Módulo | Crear | Editar | Eliminar | Consultar | Permisos (por rol) | Flujo E2E prioritario |
|--------|-------|--------|----------|-----------|-------------------|------------------------|
| Login/Logout | — | — | — | ✓ | 5 roles | Login → Dashboard → Logout |
| Dashboard | — | — | — | ✓ | Todos | Ver KPIs tras crear lead/deal |
| Leads | ✓ | ✓ | ✓ | ✓ | Todos* | Crear → Qualify → Convert → Deal |
| Customers | ✓ | ✓ | ✓ | ✓ | Todos* | CRUD + import CSV + bulk status |
| Deals | ✓ | ✓ | ✓ | ✓ | Todos* | Pipeline stage → close won |
| Users | ✓ | ✓ | — | ✓ | **Admin esperado** | Crear user + assign role |
| Users/Roles | — | — | — | ✓ | Admin | Ver conteos por rol |
| Workflows | ✓ | ✓ | ✓ | ✓ | Admin/Manager* | Create → add trigger → duplicate |
| Policies | ✓ | ✓ | ✓ | ✓ | Admin* | CRUD + import |
| Agents | — | ✓ config | — | ✓ | Admin* | Update agent JSON config |
| Audit | — | — | — | ✓ | Admin* | Filter + export JSON |
| Settings | — | ✓ | — | ✓ | Admin* | Update tenant + export/import |
| Support | — | — | — | ✓ | Todos | Health statuses Healthy |
| API Auth | — | — | — | ✓ | — | login 401/200, refresh |
| API CRUD | ✓ | ✓ | — | ✓ | Bearer JWT | Customers/Leads/Deals sin cookie |

\* *Comportamiento esperado de negocio; hoy UI permite todos los roles autenticados.*

### Prioridad de automatización Browser Tab

1. **P0:** Login (5 roles), Lead funnel completo, Deal pipeline, Logout  
2. **P1:** Customers import/bulk, Users CRUD roles, Settings export  
3. **P2:** Workflows/Policies, Audit export, Agents config  
4. **P3:** Support health, API-only endpoints, MFA path  

---

# 10. Veredicto final

## Calificaciones (1–10)

| Criterio | Nota | Comentario |
|----------|------|------------|
| **Arquitectura** | 8/10 | Clean + DDD + CQRS bien separado; tenant resolution en UI es deuda |
| **Seguridad** | 5/10 | Auth global OK; **roles no aplicados en UI**; SameTenant incompleto |
| **Escalabilidad** | 7/10 | RabbitMQ, Redis, workers; monolito API+UI |
| **Mantenibilidad** | 7/10 | Handlers claros; duplicación `GetDefaultTenantIdAsync` en cada PageModel |
| **Cobertura funcional** | 7/10 | CRM core completo en UI; IA/email stubs |
| **Preparación para pruebas E2E** | 6/10 | Forms server-side favorecen E2E; gaps de permisos y tenant confunden asserts |

## Qué falta para pruebas funcionales reales

### Bloqueantes / alta prioridad
1. **Definir matriz de permisos esperada** — ¿Viewer solo lectura? ¿Sales sin Settings? Documentar vs implementar.
2. **Estabilizar login en Docker/VPS** — persistir Data Protection keys o documentar “hard refresh” tras deploy.
3. **TenantId desde claims del usuario** — para tests multi-tenant futuros.
4. **Datos seed reproducibles** — script/seed con volúmenes sugeridos (sección 8).

### Recomendado antes de suite E2E grande
5. Aplicar `[Authorize(Policy=...)]` en Razor Pages según rol (o aceptar que tests de permisos son solo API).
6. Mensajes de error visibles (TempData) en todos los catch que solo redirigen.
7. Completar API GET by id para parity API/UI tests.
8. Health de Workers visible (opcional) para validar agentes.

### Entorno de pruebas
9. Stack completo: Postgres + Redis + RabbitMQ + API (+ Workers para eventos).
10. Usuario Browser Tab: base URL local `http://localhost:5080` o VPS `:8091`.
11. **No usar Render/producción** para E2E destructivos.

### Cobertura actual de tests automatizados
- **13 tests** unitarios/integración (`ApiIntegrationTests`: health, login, 401 customers).
- **Sin tests E2E Browser** — este documento es la base para crearlos.

---

## Resumen ejecutivo

**AutonomusCRM** es un CRM multi-tenant .NET 9 con UI Razor (42 páginas), API REST (27 acciones), PostgreSQL y procesamiento event-driven via RabbitMQ Workers. Los módulos funcionales reales son: **Dashboard, Leads, Deals, Customers, Users, Workflows, Policies, Agents, Audit, Settings, Support** — no existen módulos escolares.

La interfaz es **100% formularios POST + redirect** (sin AJAX), lo cual facilita E2E con Browser Tab. Los mayores riesgos para pruebas son la **ausencia de control de roles en Razor Pages** y el uso del **primer tenant de BD** en lugar del tenant del usuario autenticado.

Con los 5 usuarios demo por rol, seed automático y entorno Docker/VPS ya disponible, se puede iniciar inmediatamente una batería E2E de **flujos P0** (login, lead-to-deal, pipeline), documentando desviaciones de permisos como bugs conocidos o ampliando la suite cuando se endurezca la autorización UI.

---

*Documento generado por análisis estático del repositorio `c:\Proyectos\autonomuscrm` — sin modificación de código ni base de datos.*
