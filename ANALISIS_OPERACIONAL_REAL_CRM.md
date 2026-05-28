# Análisis operacional real — AutonomusCRM

| Campo | Valor |
|-------|-------|
| **Modo** | Solo análisis (sin pruebas, sin cambios de código, sin cambios de BD) |
| **Fecha** | 2026-05-25 |
| **Proyecto** | `AutonomusCRM.sln` (.NET 9, Clean Architecture + Event-Driven) |
| **UI principal** | Razor Pages + cookies (humano) |
| **API secundaria** | REST `/api/*` + JWT (integraciones, Swagger) |
| **BD analizada (config dev)** | PostgreSQL `localhost:5432` → base `autonomuscrm` |
| **Propósito** | Contexto maestro para batería E2E humana con datos casi reales |

---

## Resumen ejecutivo

**AutonomusCRM** es un CRM B2B multi-tenant orientado a **automatización por eventos**, **auditoría (event store)** y **agentes IA en background** (Worker). Resuelve la gestión del ciclo comercial desde **prospecto (Lead)** hasta **cierre (Deal)**, con **clientes**, **usuarios por rol**, **workflows**, **políticas** y **observabilidad**.

La interfaz humana es **Razor Pages** (formularios POST, redirects, tablas HTML, modales JS mínimos). No es SPA ni DataTables. Los roles CRM son exactamente **5**: `Admin`, `Manager`, `Sales`, `Support`, `Viewer`.

**Hallazgo crítico para pruebas futuras:** el RBAC en UI está **muy concentrado** en `/Users` y `/Settings` (`Admin,Manager`). El resto de módulos solo exige **usuario autenticado**; `Viewer` podría ejecutar POST de edición si conoce la URL (riesgo de permisos incompletos en UI).

**Hallazgo multitenant:** muchas páginas resuelven el tenant con `GetDefaultTenantIdAsync()` → **primer tenant de la BD**, no siempre el `TenantId` del usuario logueado.

---

# FASE 1 — Entender el negocio

## 1.1 Problema que resuelve

| Dimensión | Descripción |
|-----------|-------------|
| **Problema** | Centralizar pipeline comercial, convertir prospectos en clientes y oportunidades, auditar acciones y automatizar respuestas sin perder trazabilidad |
| **Usuario objetivo** | Equipos de ventas, gerencia comercial, administración de tenant, soporte operativo |
| **Diferenciador** | Event-driven + Event Store + motor de workflows/políticas + agentes autónomos (Worker) |

## 1.2 Flujo comercial principal (implementado)

```text
Prospecto (Lead)
    │  CreateLead → LeadCreatedEvent
    ▼
Contacto / asignación (opcional)
    │  AssignToUser → LeadAssignedEvent
    ▼
Calificación
    │  Qualify → LeadQualifiedEvent (+ score vía agente)
    ▼
Conversión a Cliente
    │  ConvertToCustomer → LeadConvertedToCustomerEvent + CustomerCreatedEvent
    ▼
Oportunidad (Deal)
    │  CreateDeal → DealCreatedEvent
    ▼
Pipeline por etapas
    │  UpdateStage → DealStageChangedEvent
    │  UpdateProbability → DealProbabilityUpdatedEvent
    ▼
Cierre
    │  Close → DealClosedEvent (ClosedWon)
    │  Lose → DealLostEvent (ClosedLost)
    ▼
Automatización paralela
    │  WorkflowEngine (triggers DomainEvent)
    │  PolicyEngine (evaluación en handlers)
    │  Workers/Agents (LeadIntelligence, CustomerRisk, DealStrategy, Communication…)
    ▼
Auditoría
    │  Event Store (tabla DomainEvents) + UI /Audit + export
```

## 1.3 Flujos secundarios

| Flujo | Descripción |
|-------|-------------|
| **Importación masiva** | CSV/JSON en Leads, Customers, Deals, Users, Workflows, Policies |
| **Acciones masivas** | Bulk status (Leads, Customers), bulk stage (Deals), bulk user status |
| **Gestión de usuarios** | Crear, editar, roles, MFA (API), activar/desactivar |
| **Configuración tenant** | Settings: nombre tenant, JSON settings, export/import config |
| **Soporte operativo** | `/Support`: health DB, EventBus, Cache |
| **Agentes IA** | Configuración por tenant en `/Agents`; ejecución real en `AutonomusCRM.Workers` |
| **API REST** | Integraciones externas con JWT (`/api/auth`, CRUD parcial) |

## 1.4 Dependencias técnicas

| Componente | Uso |
|------------|-----|
| **PostgreSQL** | Persistencia EF Core + Event Store |
| **Redis** (opcional) | Cache; fallback MemoryCache si no hay Redis |
| **RabbitMQ** (opcional) | Event Bus; dev usa `EventBus:Provider: InMemory` |
| **AutonomusCRM.Workers** | Procesa eventos y agentes (separado de la API) |
| **Serilog** | Logs en consola + `logs/autonomuscrm-*.txt` |

## 1.5 Automatizaciones y eventos (cadena)

```text
Comando UI/API
  → Agregado de dominio (Lead/Customer/Deal/User)
  → AddDomainEvent(...)
  → SaveChanges (EF)
  → IDomainEventDispatcher.DispatchAsync
       ├─ EventStore.SaveEventAsync
       ├─ WorkflowEngine.ExecuteWorkflowsAsync
       └─ IEventBus.PublishAsync
  → (Worker suscrito) Agentes procesan LeadCreated, CustomerCreated, etc.
```

**Nota:** `WorkflowEngine` tiene `TODO` en evaluación de condiciones y ejecución real de acciones (hoy principalmente **logging**).

---

# FASE 2 — Mapa funcional completo

## 2.1 Arquitectura de capas

| Capa | Proyecto | Responsabilidad |
|------|----------|-----------------|
| Domain | `AutonomusCRM.Domain` | Entidades, enums, eventos de dominio |
| Application | `AutonomusCRM.Application` | Commands, Queries, handlers, Policy/Workflow models |
| Infrastructure | `AutonomusCRM.Infrastructure` | EF, repos, EventBus, EventStore, engines |
| API | `AutonomusCRM.API` | Razor Pages + Controllers REST |
| Workers | `AutonomusCRM.Workers` | Background + agentes |
| AI | `AutonomusCRM.AI` | Placeholders configurables (`AI:Enabled`) |

## 2.2 Entidades de dominio (persistidas)

| Entidad | Tabla EF | Campos de negocio clave |
|---------|----------|-------------------------|
| **Tenant** | Tenants | Name, Settings (jsonb), KillSwitch, IsActive |
| **User** | Users | Email, Roles[], MFA, IsActive, TenantId |
| **Lead** | Leads | Name, Email, Phone, Company, Status, Source, Score, AssignedToUserId |
| **Customer** | Customers | Name, Email, Phone, Company, Status, LTV, RiskScore, LastContactAt |
| **Deal** | Deals | Title, Amount, Stage, Status, Probability, CustomerId, ExpectedCloseDate |
| **Workflow** | Workflows | Triggers[], Conditions[], Actions[], IsActive |
| **Policy** | Policies | Name, Expression, IsActive |
| **DomainEventRecord** | DomainEvents | Event sourcing / auditoría |

## 2.3 Roles y restricciones reales

| Rol | Contraseña demo | Restricción en código (Razor) |
|-----|-----------------|-------------------------------|
| Admin | Admin123! | Acceso Users + Settings |
| Manager | Manager123! | Igual que Admin en Users + Settings |
| Sales | Sales123! | **Sin** Users/Settings → AccessDenied |
| Support | Support123! | Solo autenticación global; **sin** página exclusiva más allá de Support |
| Viewer | Viewer123! | Solo autenticación global; **sin** enforcement de solo lectura en Leads/Deals |

**Políticas registradas** (`AddAutonomusPolicies`): `RequireAdmin`, `RequireManager`, `RequireSales` — **casi no usadas en Razor Pages** (solo `[Authorize(Roles = "Admin,Manager")]` en Users/* y Settings).

**Autenticación:** Cookie (UI 8h) + JWT Bearer (API). Login requiere `TenantId` + email + password.

## 2.4 Módulos UI — mapa detallado

### Módulo: Autenticación

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Account/Login`, `/Account/Logout`, `/Account/AccessDenied` |
| **PageModel** | `Login.cshtml.cs`, `Logout.cshtml.cs` |
| **Servicios** | `LoginCommandHandler`, `ITokenService`, `IUserRepository`, `ITenantRepository` |
| **Acciones** | GET login, POST login (cookie + claims), POST/GET logout |
| **Validaciones** | TenantId, email, password requeridos; MFA redirige a API |
| **Roles** | Anónimo |
| **Eventos** | `UserLoggedInEvent` (dominio usuario) |

---

### Módulo: Dashboard

| Campo | Valor |
|-------|-------|
| **Rutas** | `/` (Index), `/Dashboard` (alias página) |
| **PageModel** | `Index.cshtml.cs` |
| **Queries** | `GetLeadsByTenantQuery`, `GetDealsByTenantQuery` |
| **Acciones** | GET métricas: leads 24h, conversión, pipeline por etapa, revenue estimado |
| **Dependencias** | Primer tenant BD (`GetDefaultTenantIdAsync`) |
| **Roles** | Cualquier autenticado |

---

### Módulo: Leads

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Leads`, `/Leads/Create`, `/Leads/Edit/{id}`, `/Leads/Details/{id}`, `/Leads/Import`, `/Leads/BulkActions` |
| **API** | `api/Leads` (POST create, GET list, POST qualify) |
| **Commands** | `CreateLead`, `UpdateLead`, `DeleteLead`, `QualifyLead`, `BulkUpdateLeadStatus`, `ConvertLead` (vía dominio en Details) |
| **Acciones UI** | Listar (search, status, source), crear inline/lista, editar, eliminar, calificar, convertir a cliente, crear deal desde lead, import CSV/JSON, bulk |
| **Validaciones dominio** | Name obligatorio; score 0-100 |
| **Enums** | Status: New, Contacted, Qualified, Converted, Lost, Unqualified — Source: Website, Referral, SocialMedia, EmailCampaign, ColdCall, Partner, Event, Other |
| **Eventos** | `LeadCreated`, `LeadQualified`, `LeadConvertedToCustomer`, `LeadUpdated`, `LeadAssigned`, `LeadStatusChanged`, `LeadScoreUpdated` |
| **Roles** | Autenticado (sin distinción Sales vs Viewer en página) |

---

### Módulo: Clientes (Customers)

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Customers`, `/Customers/Create`, `/Customers/Edit/{id}`, `/Customers/Details/{id}`, `/Customers/Import`, `/Customers/BulkActions` |
| **API** | `api/Customers` |
| **Commands** | `CreateCustomer`, `UpdateCustomer`, `DeleteCustomer`, `UpdateCustomerStatus`, `BulkUpdateCustomerStatus`, `RecordContact` |
| **Acciones UI** | CRUD, contactar (RecordContact), crear deal, import, bulk status, eliminar |
| **Campos** | Name*, Email, Phone, Company, Status, LTV, RiskScore, Metadata |
| **Enums Status** | Prospect, Lead, Qualified, Customer, VIP, Churned, Inactive |
| **Eventos** | `CustomerCreated`, `CustomerUpdated`, `CustomerStatusChanged`, `CustomerLifetimeValueUpdated`, `CustomerRiskScoreUpdated`, `CustomerPurchaseRecorded` |
| **Roles** | Autenticado |

---

### Módulo: Pipeline / Deals

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Deals`, `/Deals/Create`, `/Deals/Edit/{id}`, `/Deals/Details/{id}`, `/Deals/Import`, `/Deals/BulkActions` |
| **API** | `api/Deals` |
| **Commands** | `CreateDeal`, `UpdateDeal`, `UpdateDealStage`, `UpdateDealProbability`, `CloseDeal`, `DeleteDeal`, `BulkUpdateDealStage` |
| **Acciones UI** | Listar (search, status, stage), crear, editar etapa/probabilidad, cerrar, eliminar, import, bulk stage |
| **Enums** | Stage: Prospecting → Qualification → Proposal → Negotiation → ClosedWon/ClosedLost — Status: Open, Closed, OnHold, Cancelled |
| **Eventos** | `DealCreated`, `DealStageChanged`, `DealProbabilityUpdated`, `DealClosed`, `DealLost`, `DealAssigned`, `DealAmountUpdated`, `DealUpdated` |
| **Roles** | Autenticado |

---

### Módulo: Usuarios y roles

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Users`, `/Users/Create`, `/Users/Edit/{id}`, `/Users/Roles`, `/Users/Import`, `/Users/BulkActions` |
| **API** | `api/Users`, `api/auth` |
| **Commands** | `CreateUser`, `UpdateUser`, `AssignRole`, `RemoveRole`, `ToggleUserStatus`, `BulkUpdateUserStatus`, MFA commands |
| **Acciones UI** | Listar, buscar, crear, editar, asignar/quitar rol, activar/desactivar, import, bulk |
| **Roles permitidos** | **Admin, Manager** únicamente (`[Authorize(Roles = "Admin,Manager")]`) |
| **Roles disponibles** | Admin, Manager, Sales, Support, Viewer |
| **Eventos** | `UserCreated`, `UserRoleAdded`, `UserRoleRemoved`, `UserActivated`, `UserDeactivated`, etc. |
| **UI decorativa** | Tabla “Roles y permisos” con datos estáticos; botón “Gestionar roles” → alert placeholder |

---

### Módulo: Workflows

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Workflows`, `/Workflows/Create`, `/Workflows/Edit/{id}`, `/Workflows/Import` |
| **API** | `api/Workflows` |
| **Commands** | `CreateWorkflow`, triggers/conditions/actions add, update, delete, duplicate (edit page) |
| **Acciones UI** | Listar, crear, editar, activar/desactivar, agregar trigger (DomainEvent + EventType), condición, acción, import/export JSON |
| **Motor** | `WorkflowEngine` — match por `trigger.EventType == domainEvent.EventType` |
| **Roles** | Autenticado |

---

### Módulo: Políticas

| Campo | Valor |
|-------|-------|
| **Rutas** | `/Policies`, `/Policies/Create`, `/Policies/Edit/{id}`, `/Policies/Import` |
| **Commands** | `CreatePolicy`, `UpdatePolicy`, `DeletePolicy`, `DuplicatePolicy` |
| **Acciones UI** | CRUD, expresión texto, activar/desactivar, import |
| **Motor** | `PolicyEngine` — evalúa expresiones sobre eventos |
| **Roles** | Autenticado |

---

### Módulo: Auditoría

| Campo | Valor |
|-------|-------|
| **Ruta** | `/Audit` |
| **PageModel** | `Audit.cshtml.cs` |
| **Acciones** | Filtrar por tipo de evento y fechas; POST export |
| **Fuente datos** | `IEventStore` / `DomainEvents` |
| **Roles** | Autenticado |

---

### Módulo: Configuración (Settings)

| Campo | Valor |
|-------|-------|
| **Ruta** | `/Settings` |
| **Commands** | `UpdateTenantCommand`, `UpdateSystemSettingsCommand` |
| **Acciones** | Actualizar tenant (name, email, region, timezone), JSON settings, export config, restore defaults, import config file |
| **Roles** | **Admin, Manager** |

---

### Módulo: Soporte

| Campo | Valor |
|-------|-------|
| **Ruta** | `/Support` |
| **Acciones** | GET health: database, eventbus, cache + TenantId |
| **Roles** | Autenticado (rol Support no tiene restricción extra) |

---

### Módulo: Agentes IA

| Campo | Valor |
|-------|-------|
| **Ruta** | `/Agents` |
| **Acciones** | Listar agentes (estático + config por tenant), POST `UpdateAgentConfig` |
| **Agentes Worker** | LeadIntelligence, CustomerRisk, DealStrategy, Communication, DataQualityGuardian, ComplianceSecurity, AutomationOptimizer |
| **Eventos típicos** | LeadCreated, CustomerCreated, DealCreated, DealStageChanged |
| **Roles** | Autenticado |

---

### Módulo: API REST (paralelo a UI)

| Controller | Endpoints principales |
|------------|----------------------|
| `AuthController` | login, refresh, verify-mfa |
| `LeadsController` | POST, GET, qualify |
| `CustomersController` | CRUD |
| `DealsController` | CRUD, stage, close |
| `UsersController` | usuarios |
| `TenantsController` | tenants |
| `WorkflowsController` | workflows |
| `HealthController` | `/health` |
| `MetricsController` | métricas |

---

# FASE 3 — Datos reales de negocio

## 3.1 Campos existentes vs. deseables para simulación real

### Leads (implementado)

| Campo | En producto | Recomendación E2E |
|-------|-------------|-------------------|
| Nombre | Sí* | Empresa + contacto realista |
| Email | Sí | dominio corporativo |
| Teléfono | Sí | formato país (PA, CR, CO) |
| Empresa | Sí | razón social |
| Fuente | Sí (enum) | mix Website/Referral/Event |
| Estado | Sí | distribuir en funnel |
| Score | Sí (agente/UI) | 20-95 según fuente |
| Responsable | Sí (AssignedToUserId) | 5 vendedores Sales |
| Industria, presupuesto, prioridad | **No** (solo Metadata jsonb) | usar Metadata en pruebas avanzadas |

### Clientes (implementado)

| Campo | En producto | Recomendación E2E |
|-------|-------------|-------------------|
| Nombre, email, teléfono, empresa | Sí | datos B2B consistentes con lead |
| Estado | Sí | Prospect → Customer → VIP |
| LTV, RiskScore | Sí | poblados por agentes o manual |
| País, provincia, ciudad, web, empleados, ingresos | **No** | Metadata o futuro |

### Deals (implementado)

| Campo | En producto | Recomendación E2E |
|-------|-------------|-------------------|
| Título, monto, descripción | Sí | escenarios por industria |
| Etapa, probabilidad | Sí | pipeline realista |
| Fecha cierre esperada | Sí | trimestre fiscal |
| Cliente vinculado | Sí | FK CustomerId |

### Usuarios

| Campo | En producto |
|-------|-------------|
| Email, password, nombre, apellido, roles[], MFA, activo | Sí |

## 3.2 Volúmenes recomendados para E2E “casi real”

| Entidad | Mínimo demo (seed actual) | **Ideal pruebas humanas** | Justificación |
|---------|---------------------------|---------------------------|---------------|
| Tenants | 1 | 1-2 | multitenant aislado |
| Usuarios | 5 (1/rol) | **15-25** | 3 Admin/Manager, 5 Sales, 3 Support, 4 Viewer |
| Leads | 3 | **80-120** | funnel y búsqueda |
| Clientes | 3 | **50-60** | post-conversión + import |
| Deals | 1 | **25-40** | pipeline por etapa |
| Workflows | 0-2 | **5-8** | triggers por evento |
| Policies | 0-1 | **3-5** | reglas negocio |
| Eventos (store) | automático | **500+** | auditoría y filtros |
| Agent configs | 7 nombres | 7 (1 por agente) | validar UI config |

## 3.3 Seed actual (referencia, sin ejecutar)

Al primer arranque con BD vacía (`DatabaseSeeder`):

- Tenant: `AutonomusCRM Demo`
- 5 usuarios demo (`*@autonomuscrm.local`, password `{Rol}123!`)
- 3 clientes, 3 leads, 1 deal

Si la BD ya tiene datos: solo **asegura** usuarios demo por rol (`EnsureDemoRoleUsersAsync`).

---

# FASE 4 — Simulación de empresa real

## 4.1 Empresa ficticia: TechNova Solutions

| Atributo | Valor |
|----------|-------|
| **Industria** | Software B2B / integración CRM |
| **Sede** | Ciudad de Panamá |
| **Mercados** | Panamá, Costa Rica, Colombia |
| **Producto** | Plataforma AutonomusCRM + servicios implementación |
| **Ticket promedio deal** | USD 8,000 – 120,000 |
| **Ciclo venta** | 30-90 días |

## 4.2 Estructura organizacional

```text
CEO / Admin CRM          → rol Admin (1)
Director Comercial       → rol Manager (1)
Gerentes zona (PA/CR/CO) → rol Manager (2)
Ejecutivos ventas        → rol Sales (5)
Analistas solo lectura   → rol Viewer (3)
Soporte N1/N2            → rol Support (2)
```

## 4.3 Volumen operativo diario esperado (simulado)

| Métrica | Volumen/día |
|---------|-------------|
| Leads nuevos | 8-15 |
| Contactos registrados | 10-20 |
| Calificaciones | 4-8 |
| Conversiones a cliente | 2-5 |
| Deals creados | 1-3 |
| Cambios de etapa | 5-10 |
| Cierres ganados | 0-2 |
| Eventos de dominio | 50-150 |
| Logins | 20-40 |

## 4.4 Distribución de datos TechNova (objetivo E2E)

| Entidad | Cantidad | Notas |
|---------|----------|-------|
| Leads | 100 | 40% Website, 25% Referral, resto mix |
| Clientes | 55 | ~45% desde conversión lead |
| Deals | 28 | por etapa: 6/6/5/4/4/3 (últimas Closed) |
| Usuarios | 13 | según organigrama |
| Workflows | 6 | ej. auto-score lead, alerta deal riesgo |
| Policies | 4 | ej. bloqueo deal >100k sin Manager |

---

# FASE 5 — Flujos humanos reales (escenarios)

## Escenario A — Vendedor (Sales): día típico

1. Login (`TenantId` + sales@…)
2. Dashboard → revisar leads 24h
3. `/Leads` → filtrar New → abrir detalle
4. Actualizar datos contacto → guardar
5. **Calificar** lead → confirmación
6. **Convertir a cliente** → redirect `/Customers/Details/{id}`
7. Desde cliente o lead → **Crear deal** (título, monto)
8. `/Deals/Details/{id}` → cambiar etapa a Proposal
9. Actualizar probabilidad → 65%
10. Cerrar deal (ClosedWon) con monto final
11. Verificar evento en `/Audit`
12. Logout

## Escenario B — Gerente (Manager): operación

1. Login manager
2. `/Users` → crear vendedor nuevo
3. `/Users/Edit/{id}` → asignar rol Sales
4. `/Leads/Import` → subir CSV 20 leads
5. `/Customers` → bulk cambio estado
6. `/Deals` → revisar pipeline por etapa
7. `/Workflows/Create` → workflow LeadCreated
8. `/Policies/Create` → política validación
9. `/Settings` → actualizar timezone tenant
10. `/Audit` → exportar JSON
11. Logout

## Escenario C — Admin: gobierno

1. Login admin
2. Dashboard + `/Agents` → revisar estado agentes
3. `/Users/Roles` → distribución roles
4. `/Settings` → export config → import config
5. Desactivar usuario prueba (`ToggleUserStatus`)
6. Eliminar lead/deal de prueba (limpieza)
7. Verificar `/Support` health OK
8. Logout

## Escenario D — Soporte (Support)

1. Login support
2. `/Support` → Database/EventBus/Cache Healthy
3. Intentar `/Users` → **AccessDenied** (esperado)
4. `/Leads` lectura (si política negocio lo permite)
5. Documentar hallazgos en ticket ficticio
6. Logout

## Escenario E — Consulta (Viewer)

1. Login viewer
2. `/Leads` listado (solo lectura **esperada**)
3. Intentar `/Leads/Edit/{id}` → **definir expectativa** (hoy puede permitir POST)
4. Intentar `/Users` → AccessDenied
5. `/Deals` vista pipeline sin modificar
6. Logout

## Escenario F — Negativos transversales

| # | Acción | Resultado esperado |
|---|--------|-------------------|
| F1 | `/Leads` sin cookie | Redirect `/Account/Login` |
| F2 | Credenciales inválidas | Mensaje error visible |
| F3 | Sales → `/Settings` | AccessDenied |
| F4 | Lead GUID inexistente | 404 o página error amigable |
| F5 | Crear lead nombre vacío | Validación HTML5 / error servidor |
| F6 | Import CSV corrupto | Mensaje error, sin corruptura BD |
| F7 | Deal monto ≤ 0 | ArgumentException / error UI |
| F8 | Rate limit 200 req/min | 429 (API) |

## Escenario G — Automatización (requiere Worker + EventBus)

1. Crear lead → verificar `LeadCreatedEvent` en Audit
2. Con Worker activo: score lead actualizado
3. Crear customer → risk score / LTV
4. Cambiar etapa deal → sugerencias Deal Strategy (logs Worker)

## Escenario H — API + UI mixto (opcional)

1. POST `/api/auth/login` → JWT
2. GET `/api/Leads?tenantId=...` con Bearer
3. Comparar consistencia con UI mismo tenant

---

# FASE 6 — Matriz de pruebas futura

| Módulo | Flujo real humano | Datos requeridos | Riesgo | Prioridad |
|--------|-------------------|------------------|--------|-----------|
| Auth | Login/logout 5 roles + MFA API | 5 usuarios + tenant | Medio | P0 |
| Dashboard | Métricas coherentes con BD | 50+ leads, 20+ deals | Bajo | P1 |
| Leads | CRUD + calificar + convertir | 100 leads variados | Alto | P0 |
| Customers | CRUD + contacto + import CSV | 50 clientes | Alto | P0 |
| Deals | Pipeline completo + cierre | 25 deals, etapas | Alto | P0 |
| Users | CRUD roles Admin/Manager | 15 usuarios | Alto | P0 |
| RBAC | Sales/Viewer bloqueados Admin | 5 roles | **Crítico** | P0 |
| Workflows | Crear + trigger real | 5 workflows activos | Medio | P1 |
| Policies | Crear + evaluar | 3 políticas | Medio | P2 |
| Audit | Filtros + export | 500+ eventos | Medio | P1 |
| Settings | Guardar tenant + import config | 1 tenant | Medio | P1 |
| Support | Health checks | servicios up | Bajo | P2 |
| Agents | Config JSON por agente | tenant + worker | Alto | P2 |
| Import | 6 módulos import | CSV válidos/inválidos | Alto | P1 |
| Bulk | 3 bulk pages | selección múltiple | Medio | P1 |
| Multitenant | 2 tenants datos cruzados | 2 tenants | **Crítico** | P1 |
| API JWT | Flujos paralelos UI | token | Medio | P2 |
| Visual | Modales, tablas, responsive | N/A | Bajo | P2 |

**Estimación casos ejecutables:** **180-220** casos humanos atómicos → **35-45** flujos E2E compuestos (escenarios A-H).

---

# FASE 7 — Puntos críticos

## 7.1 Concurrencia

- EF Core sin optimistic concurrency visible en entidades (`RowVersion` no detectado).
- Bulk actions: riesgo de sobrescritura si dos usuarios editan mismo registro.
- **Prueba futura:** dos sesiones Sales editan mismo deal.

## 7.2 Roles y permisos

| Hallazgo | Impacto E2E |
|----------|-------------|
| Solo Users/Settings con `[Authorize(Roles)]` | Viewer puede POST en Leads si UI muestra botones |
| Tabla permisos en `/Users` es **decorativa** | No confiar en UI para matriz permisos |
| Policies `RequireSales` no aplicadas en Razor | Gap seguridad / prueba negativa importante |

## 7.3 Multitenancy

- Login valida usuario en `TenantId` indicado.
- Páginas usan `GetDefaultTenantIdAsync()` → **primer tenant**, no claim del usuario.
- **Prueba futura obligatoria:** segundo tenant con datos distintos; usuario tenant B no debe ver datos tenant A.

## 7.4 Importaciones

- Formatos CSV/JSON en 6 módulos.
- Riesgos: duplicados email, encoding, filas vacías, GUID inválidos.
- Customers tiene modal import en lista; otros en rutas dedicadas.

## 7.5 Workflows

- Triggers por `EventType` string (debe coincidir nombre CLR del evento).
- Condiciones/acciones: **TODO** — pruebas deben validar **registro en BD + logs**, no efectos externos reales.

## 7.6 Auditoría

- Event Store persiste todos los eventos despachados.
- UI `/Audit` filtra y exporta.
- Detalle lead 404: `NotFound()` sin vista — UX pobre en pruebas.

## 7.7 Integraciones

| Integración | Estado |
|-------------|--------|
| PostgreSQL | Requerido |
| Redis | Opcional (cache) |
| RabbitMQ | Producción; dev InMemory |
| AI externa | `AI:Enabled: false` placeholder |
| Workers | Proceso separado; sin Worker los agentes no corren en background |

## 7.8 Agentes IA

- UI muestra estado **estático** “Active”.
- Lógica real en `AutonomusCRM.Workers` suscrito al bus.
- E2E “realista” requiere **API + Worker** levantados y mismo EventBus.

## 7.9 Health y seguridad

- Health checks: database, eventbus, cache.
- Rate limiting global 200/min.
- HSTS en producción.
- Cookies HttpOnly; JWT para API.
- BCrypt passwords.
- Kill-switch tenant (dominio, no probado en UI aún).

---

# FASE 8 — Veredicto y preparación

## 8.1 Capacidad de simulación empresarial

| Pregunta | Respuesta |
|----------|-----------|
| ¿Soporta simulación empresa real (TechNova)? | **Sí, parcial** — entidades y flujo comercial completos; faltan campos CRM avanzados (territorio, industria, campos custom UI) |
| ¿Cuántos usuarios simulados? | **13-25** recomendado |
| ¿Cuántos flujos E2E compuestos? | **35-45** |
| ¿Cuántos casos atómicos? | **180-220** |
| ¿Qué datos crear antes de E2E? | Ver tabla Fase 3.2 + empresa TechNova Fase 4 |

## 8.2 Datos a crear (fase posterior — no ejecutado aquí)

1. Script o UI masiva: 100 leads, 55 customers, 28 deals.
2. 13 usuarios con roles y asignaciones `AssignedToUserId`.
3. 6 workflows con triggers `LeadCreatedEvent`, `DealStageChangedEvent`, etc.
4. 4 policies con expresiones simples.
5. (Opcional) Segundo tenant para pruebas aislamiento.

## 8.3 Calificación preparatoria (1-10)

| Criterio | Nota | Comentario |
|----------|------|------------|
| **Arquitectura** | 9 | Clean + DDD + eventos bien separados |
| **Seguridad** | 6 | RBAC UI incompleto; tenant resolver débil |
| **Escalabilidad** | 8 | Bus/Workers/Redis preparados |
| **Realismo negocio** | 7 | Flujo ventas sólido; campos enterprise limitados |
| **Preparación pruebas E2E** | 8 | UI clara; muchos módulos; depende Worker para IA |

## 8.4 Veredicto análisis

**LISTO PARA DISEÑAR** batería E2E humana extremadamente realista, con condiciones:

1. Usar **solo terminología CRM** (Admin, Manager, Sales, Support, Viewer).
2. Levantar **API + PostgreSQL**; opcional **Worker** para escenarios G.
3. Priorizar **P0:** Auth, Leads, Customers, Deals, Users, RBAC negativos, multitenant.
4. Documentar gaps (Viewer edita, 404 vacío, workflows TODO) como bugs o alcance.

**NO GO** solo si se exigen permisos granulares por módulo **antes** de probar — el código actual no los implementa por completo en Razor.

---

# Anexo A — Inventario de eventos de dominio

| Área | Eventos |
|------|---------|
| Lead | Created, Updated, Qualified, ConvertedToCustomer, Assigned, StatusChanged, ScoreUpdated |
| Customer | Created, Updated, StatusChanged, LifetimeValueUpdated, RiskScoreUpdated, PurchaseRecorded |
| Deal | Created, Updated, StageChanged, ProbabilityUpdated, AmountUpdated, Assigned, Closed, Lost |
| User | Created, Updated, RoleAdded, RoleRemoved, Activated, Deactivated, LoggedIn, MfaEnabled/Disabled, PasswordChanged |
| Tenant | Created, Updated, Activated, Deactivated, KillSwitchEnabled/Disabled |

---

# Anexo B — Rutas UI consolidadas

| Ruta | Módulo |
|------|--------|
| `/Account/Login` | Auth |
| `/` | Dashboard |
| `/Leads` (+ Create, Edit, Details, Import, BulkActions) | Leads |
| `/Customers` (+ Create, Edit, Details, Import, BulkActions) | Customers |
| `/Deals` (+ Create, Edit, Details, Import, BulkActions) | Deals |
| `/Users` (+ Create, Edit, Roles, Import, BulkActions) | Users |
| `/Workflows` (+ Create, Edit, Import) | Workflows |
| `/Policies` (+ Create, Edit, Import) | Policies |
| `/Audit` | Auditoría |
| `/Settings` | Config (Admin/Manager) |
| `/Support` | Soporte |
| `/Agents` | Agentes |
| `/Account/AccessDenied` | RBAC |

---

# Anexo C — Configuración operativa (referencia)

```json
// appsettings.Development.json (analizado, no modificado)
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=autonomuscrm;..."
},
"EventBus": { "Provider": "InMemory" },
"Seed": { "Enabled": true, "EnsureRoleUsers": true }
```

---

# Anexo D — Prompt maestro sugerido (siguiente fase)

Cuando autorices ejecución de pruebas, el prompt debería exigir:

1. Empresa **TechNova Solutions** con volúmenes Fase 4.4.
2. Recorrido **5 roles × módulos P0** con Browser Tab.
3. Validación **multitenant** con 2 tenants.
4. Evidencia por caso: esperado / obtenido / screenshot / URL.
5. Ciclo fix solo si falla (fuera de este documento).

---

*Documento generado por análisis estático del repositorio. No se ejecutaron pruebas, no se modificó código ni base de datos.*
