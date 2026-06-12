# Manual de Usuario Empresarial — Rol Admin

**Producto:** AutonomusCRM / AutonomusFlow  
**Rol documentado:** Admin (único rol con política `RequireAdmin`)  
**Usuario demo:** `admin@autonomuscrm.local` / `Admin123!`  
**Pantalla de inicio:** `/executive` (`RoleHomeRedirect.cs`)  
**Evidencia técnica:** `DemoRoleUsers.cs`, `CommercialWriteAuthorizationMiddleware.cs`, `RoleHomeRedirect.cs`, `AuthorizationPolicies.cs`, `UsersController.cs`, `TenantsController.cs`

---

## Capítulo 1. Quién es este rol

### 1.1 Definición

El **Admin** es el administrador del tenant y la máxima autoridad operativa dentro de AutonomusCRM. En el seed de demostración corresponde al usuario **Admin Sistema** (`admin@autonomuscrm.local`). Es el único rol autorizado para **provisionar tenants y usuarios vía API REST** (`POST api/tenants`, `POST api/users` con política `RequireAdmin`).

### 1.2 Objetivos estratégicos

| Objetivo | Descripción |
|----------|-------------|
| Gobernanza del tenant | Configurar Settings, políticas ABAC, integraciones y facturación |
| Seguridad y cumplimiento | Auditar eventos, gestionar usuarios/roles, MFA y kill-switch autónomo |
| Supervisión de ingresos | Usar Executive OS y Revenue OS para decisiones de dirección |
| Gobernanza de IA | Aprobar/rechazar decisiones en Trust Studio (HITL) |
| Operación comercial | Escritura completa en Leads, Customers, Deals, Workflows y Policies (igual que Manager y Sales en UI) |

### 1.3 Responsabilidades operativas

1. **Provisioning:** crear tenants (`POST api/tenants`) y usuarios API (`POST api/users`); gestión UI en `/Users`, `/Users/Create`, `/Users/Roles`, `/Users/Import`.
2. **Configuración:** `/Settings` (perfil tenant, MFA, kill-switch, restauración de defaults).
3. **Políticas y automatización:** `/Policies`, `/Workflows` — definir reglas ABAC y flujos por eventos de dominio.
4. **Trust Studio:** `/TrustInbox` — aprobar, rechazar, rollback y ajustar umbral de aprobación (50–95).
5. **Auditoría:** `/Audit` — consulta y export JSON (hasta 10.000 eventos).
6. **Integraciones:** `/Integrations` — HubSpot, Salesforce, email, Stripe.
7. **Resolución de incidentes:** `/FailedEvents` (DLQ), logs workers, health checks.
8. **Facturación:** `/billing` — dashboard de suscripción del tenant.

### 1.4 KPIs que el Admin debe monitorear

| KPI | Fuente en sistema | Interpretación |
|-----|-------------------|----------------|
| Forecast 30/60/90 | `/Deals` — `DealRepository.GetListSummaryAsync` | Compromiso de ingresos ponderado (Amount × Probability) |
| Win Rate | `/Deals` | Won / (Won + Lost) |
| Revenue generated/protected | `/` Command, `/executive` | Salud del flujo de ingresos (7 o 30 días) |
| Pending Trust approvals | `/TrustInbox`, badge sidebar | Backlog HITL; riesgo de SLA vencido |
| Leads HighScoreCount (>70) | `/Leads` | Calidad del embudo superior |
| Customer HighRiskCount (>70) | `/Customers` | Cuentas en riesgo de churn |
| Tasks overdue | `/Tasks?overdueOnly=true` | Incumplimiento SLA operativo |
| Failed events | `/FailedEvents` | Eventos no procesados por workers |

### 1.5 Impacto en el negocio

- Sin Admin configurado correctamente, **no hay usuarios**, **no hay integraciones** y **las decisiones autónomas** pueden ejecutarse sin supervisión HITL adecuada.
- El Admin es el punto de escalamiento para **brechas documentadas** (p. ej. API comercial POST sin filtro de rol vs UI bloqueada para Support/Viewer).
- La redirección post-login a `/executive` posiciona al Admin en la **vista consolidada de dirección**, no en el pipeline diario de un vendedor (`/revenue` es home de Sales).

---

## Capítulo 2. Acceso, login, permisos y seguridad

### 2.1 Credenciales demo

| Campo | Valor |
|-------|-------|
| Email | `admin@autonomuscrm.local` |
| Contraseña | `Admin123!` (patrón `{Role}123!` en `DemoRoleUsers.PasswordFor`) |
| Nombre | Admin Sistema |

La pantalla de login muestra cuentas demo desde `DemoRoleUsers.All`.

### 2.2 Flujo de acceso

1. Navegar a `/Account/Login`.
2. Autenticarse con email y contraseña (o cuenta demo Admin).
3. Si MFA está habilitado, completar segundo factor (`EnableMfaCommand` vía `POST api/users/{id}/enable-mfa`).
4. Tras login exitoso, `RoleHomeRedirect.GetHomePath` redirige a **`/executive`**.

### 2.3 Matriz de permisos Admin (código verificado)

| Capacidad | Admin | Evidencia |
|-----------|:-----:|-----------|
| Lectura autenticada general | ✅ | `[Authorize]` global |
| Home `/executive` | ✅ | `RoleHomeRedirect.cs` |
| POST `api/tenants` | ✅ | `TenantsController` + `RequireAdmin` |
| POST `api/users` | ✅ | `UsersController` + `RequireAdmin` |
| UI `/Users/*` | ✅ | `[Authorize(Roles = "Admin,Manager")]` |
| UI `/Settings` | ✅ | `[Authorize(Roles = "Admin,Manager")]` |
| Escritura comercial UI (Leads/Customers/Deals/Workflows/Policies) | ✅ | `CommercialWriteAuthorizationMiddleware` — roles `Admin`, `Manager`, `Sales` |
| Trust Studio Approve/Reject/Rollback | ✅ | `TrustInbox.cshtml.cs` — sin restricción de rol adicional (autenticado) |
| Billing `/billing` | ✅ | Sin `[Authorize(Roles=...)]` en `Billing.cshtml.cs` |
| Búsqueda global Ctrl+K | ✅ | `/api/flow/search` |

### 2.4 Escritura comercial — middleware

`CommercialWriteAuthorizationMiddleware` bloquea POST y GET a rutas `/Create` o `/Edit` en:

- `/Leads`, `/Customers`, `/Deals`, `/Workflows`, `/Policies`

para roles que **no** sean Admin, Manager o Sales. El Admin **nunca** recibe redirect a `/Account/AccessDenied` por escritura comercial en UI.

### 2.5 Políticas de autorización registradas

`AuthorizationPolicies.cs` define:

- `RequireAdmin` → solo rol Admin
- `RequireManager` → Admin o Manager
- `RequireSales` → Admin, Manager o Sales
- `RequireSameTenant` → aislamiento por tenant

**Uso real verificado:** `RequireAdmin` en `POST api/tenants` y `POST api/users`. Las políticas `RequireManager` y `RequireSales` están registradas pero **no aplicadas** en endpoints comerciales según documentación enterprise.

### 2.6 Seguridad operativa recomendada

1. **Rotar contraseñas demo** en entornos no demostrativos.
2. **No asignar rol Admin** a ejecutivos comerciales; usar Sales o Manager.
3. Revisar **Audit** tras cambios en Users, Settings o Policies.
4. Mantener **kill-switch** (`Settings` → `KillSwitch`) accesible para detener ciclo autónomo en incidentes.
5. Validar que integraciones OAuth usen callbacks correctos antes de producción.
6. Conocer la **brecha UI vs API:** Support/Viewer bloqueados en Razor pero API POST comercial solo exige autenticación — restringir tokens API en producción.

### 2.7 MFA

- Habilitación: `POST api/users/{id}/enable-mfa?tenantId={guid}`.
- Gestión de usuarios bloqueados por MFA: reset desde `/Users/Edit` (escalar internamente si el Admin pierde acceso).

---

## Capítulo 3. Menús disponibles (19 ítems del sidebar)

El menú lateral está definido en `Pages/Shared/Flow/_FlowSidebar.cshtml`. **Los 19 ítems son visibles para todos los roles autenticados**; la restricción es por **acceso denegado** en acciones de escritura o páginas con `[Authorize(Roles=...)]`, no por ocultar ítems.

| # | Sección | Etiqueta | Ruta | Uso principal Admin |
|---|---------|----------|------|---------------------|
| 1 | Command | Command | `/` | Métricas flujo, decisiones IA, workforce snapshot |
| 2 | Command | Trust Studio | `/TrustInbox` | Aprobación HITL; badge con pendientes |
| 3 | Command | Workforce | `/Agents` | Agentes autónomos y decisiones recientes |
| 4 | Revenue | Revenue OS | `/revenue` | Fugas de ingreso, grafo explicativo |
| 5 | Revenue | Executive | `/executive` | **Home post-login** — dashboard consolidado |
| 6 | Revenue | Pipeline | `/Deals` | Kanban/lista oportunidades, forecast |
| 7 | Customers | Directory | `/Customers` | Directorio clientes, LTV, riesgo |
| 8 | Customers | Customer 360 | `/Customer360` | Búsqueda vista 360 |
| 9 | Customers | Customer Success | `/customer-success` | Tickets, casos, playbooks CS |
| 10 | Commerce | Leads | `/Leads` | Gestión prospectos |
| 11 | Intelligence | Memory | `/Memory` | Memoria semántica empresarial |
| 12 | Operations | Tasks | `/Tasks` | Tareas workflow y operativas |
| 13 | Platform | Integrations | `/Integrations` | HubSpot, Salesforce, email, Stripe |
| 14 | Platform | Voice | `/VoiceCalls` | Registro de llamadas |
| 15 | Admin | Users | `/Users` | Usuarios, roles, importación |
| 16 | Admin | Policies | `/Policies` | Políticas ABAC |
| 17 | Admin | Audit | `/Audit` | Event sourcing / auditoría |
| 18 | Admin | Settings | `/Settings` | Perfil tenant, MFA, kill-switch |
| 19 | Admin | Billing | `/billing` | Suscripción y facturación |

### Rutas críticas fuera del sidebar

| Ruta | Propósito Admin |
|------|-----------------|
| `/Leads/Create`, `/Edit`, `/Details` | CRUD y Qualify/Convert/Create Deal |
| `/Customers/Create`, `/Edit`, `/Details` | CRUD cliente |
| `/Deals/Create`, `/Edit`, `/Details` | CRUD deal, Close/Lose |
| `/Workflows`, `/Workflows/Edit` | Automatizaciones configurables |
| `/command/decisions` | Historial decisiones filtrable |
| `/command/outcomes` | Outcome Fabric |
| `/command/playbooks` | Playbooks autónomos |
| `/customers/{id}/360` | Vista 360 individual |
| `/FailedEvents` | DLQ — replay eventos fallidos |
| `/Users/Create`, `/Users/Roles`, `/Users/Import` | Gestión usuarios |

### Nota sobre Access Denied (otros roles)

Sales recibe **Access Denied** en `/Users` y `/Settings` (`[Authorize(Roles = "Admin,Manager")]`). Support y Viewer reciben **Access Denied** al intentar POST o `/Create`/`/Edit` en módulos comerciales. **Admin y Manager no tienen estas restricciones** en los módulos documentados.

### Búsqueda global

**Ctrl+K** abre búsqueda que consulta `/api/flow/search` (incluye Trust Studio, Leads, Deals, etc.).

---

## Capítulo 4. Flujo diario del Admin

### 4.1 Ritual matutino (30 minutos)

| Minuto | Acción | Ruta |
|--------|--------|------|
| 0–5 | Login → Executive OS | `/executive` |
| 5–10 | Revisar badge Trust Studio | `/TrustInbox` |
| 10–15 | Command Center — decisiones y cuentas en riesgo | `/` |
| 15–20 | Tasks overdue del tenant | `/Tasks?overdueOnly=true` |
| 20–25 | Failed Events si hay alertas | `/FailedEvents` |
| 25–30 | Integrations health | `/Integrations` |

### 4.2 Durante la jornada

| Evento | Acción Admin |
|--------|--------------|
| Nueva solicitud de usuario | `/Users/Create` o `POST api/users` |
| Decisión IA pendiente crítica | `/TrustInbox` → Approve/Reject |
| Cambio de política comercial | `/Policies` o `/Workflows/Edit` |
| Incidente workers/RabbitMQ | Logs + `/FailedEvents` replay |
| Solicitud de forecast | `/Deals` o export Executive |

### 4.3 Ritual de cierre (15 minutos)

1. Trust Studio — cero pendientes críticos/overdue SLA.
2. Audit — muestreo de eventos del día (cambios Users/Settings).
3. Command — revisar métricas 7 días.
4. Verificar que kill-switch refleje estado deseado en Settings.

### 4.4 Ritual semanal

| Día | Acción |
|-----|--------|
| Lunes | Executive OS + forecast 30/60/90 en `/Deals` |
| Miércoles | Trust backlog + Memory dashboard |
| Viernes | Export Audit JSON; revisión roles en `/Users/Roles` |
| Según necesidad | Billing, rotación credenciales demo, backup VPS |

---

## Capítulo 5. Procesos operativos paso a paso

### 5.1 Provisionar un nuevo tenant (solo Admin)

**Prerrequisito:** token JWT de usuario Admin.

1. `POST api/tenants` con body `CreateTenantCommand` (nombre, settings).
2. Verificar respuesta `201 Created` con `tenantId`.
3. `GET api/tenants/{id}` para confirmar datos.
4. Configurar Settings del nuevo tenant vía UI o `UpdateSystemSettingsCommand`.
5. Crear usuario inicial con `POST api/users` o `/Users/Create`.

### 5.2 Crear usuario (UI y API)

**UI (`/Users/Create`):** Admin y Manager pueden usar el formulario (`CreateUserCommand`).

**API (`POST api/users`):** **solo Admin** (`RequireAdmin`).

Pasos UI:

1. Ir a `/Users` → Crear.
2. Completar email, contraseña, nombre, apellido.
3. Asignar rol en `/Users/Roles` o `/Users/Edit` (`AssignRole`).
4. Opcional: habilitar MFA vía API.

### 5.3 Gestionar roles de equipo

1. `/Users/Roles` — vista de roles del sistema: Admin, Manager, Sales, Support, Viewer.
2. `/Users/Edit/{id}` — asignar rol, activar/desactivar usuario.
3. `/Users/BulkActions` — acciones masivas.
4. `/Users/Import` — importación CSV.

**Buena práctica:** vendedores → Sales; supervisores → Manager; solo TI/dirección → Admin.

### 5.4 Configurar Settings del tenant

1. Abrir `/Settings` (`[Authorize(Roles = "Admin,Manager")]`).
2. Editar perfil tenant, comunicaciones, umbrales.
3. **KillSwitch:** `false` = ciclo autónomo activo (si `AutonomousPlatformGate` lo permite).
4. Exportar/importar configuración JSON desde handlers `OnPostExportConfigAsync` / `OnPostImportConfigAsync`.
5. Restaurar defaults con handler de configuración por defecto.

### 5.5 Aprobar decisión en Trust Studio

1. Abrir `/TrustInbox` (badge en sidebar si hay pendientes).
2. Seleccionar ítem de la cola (severidad: critical, high, medium, low).
3. Revisar explicabilidad (`ExplainTrustApprovalAsync`) y Outcome Fabric.
4. Opcional: **Simulate** (`?preview=simulate`).
5. **Approve** → ejecuta decisión (`executeDecision: true`).
6. **Reject** → con nota opcional.
7. **Rollback** → si ya se ejecutó y requiere reversión.
8. Ajustar umbral: `OnPostSetThresholdAsync` (50–95).

### 5.6 Crear y calificar un Lead (escritura comercial)

1. `/Leads/Create` — estado inicial `New`, evento `LeadCreatedEvent`.
2. Automatizaciones: SLA 24h, `LeadIntelligenceAgent` (score), `CommunicationAgent` (si configurado).
3. `/Leads/Details` → **Qualify** → Customer auto, deal borrador ($1, IsDraft), tarea alta prioridad.
4. Actualizar deal borrador en `/Deals/Edit` con monto real.

### 5.7 Cerrar un Deal ganado

1. `/Deals/Details` → mover etapas (Prospecting → … → Negotiation).
2. **Close** → `DealClosedEvent`.
3. Post-cierre: Retention actualiza Customer/LTV; Operational crea tareas onboarding D0/D7/D30; OutcomeAttribution aprende.

### 5.8 Configurar un Workflow

1. `/Workflows` → Crear.
2. Definir trigger (tipo evento dominio, p. ej. `Lead.Created`).
3. Añadir condiciones y acciones: `Assign`, `UpdateStatus`, `CreateTask`.
4. **Limitación:** acciones `Communicate` y `ActivateAgent` solo registran log — no envían mensajes ni activan LLM.

### 5.9 Reprocesar eventos fallidos

1. `/FailedEvents` — listar DLQ.
2. Identificar evento y causa en logs (`autonomuscrm-workers`).
3. Ejecutar replay según UI disponible.
4. Verificar en `/Audit` y `/Tasks` que se generaron efectos esperados.

### 5.10 Exportar auditoría

1. `/Audit` — filtrar por tipo/fecha.
2. Export JSON (hasta 10.000 eventos).
3. Archivar para cumplimiento o investigación de incidentes.

---

## Capítulo 6. Automatizaciones relacionadas con el rol Admin

El Admin no ejecuta automatizaciones manualmente, pero **las configura, supervisa y resuelve fallos**.

### 6.1 Motores síncronos (API — `DomainEventDispatcher`)

| Motor | Eventos clave | Efecto | Supervisión Admin |
|-------|---------------|--------|-------------------|
| WorkflowEngine | Cualquier evento con workflow activo | Assign, UpdateStatus, CreateTask | `/Workflows`, `/Tasks` |
| OperationalAutomation | LeadQualified, DealClosed | Customer+deal draft+task / onboarding | `/Leads`, `/Deals` |
| RevenueAutomation | LeadCreated, LeadScoreUpdated, LeadQualified | SLA, asignación score alto | `/Tasks`, `/revenue` |
| RetentionAutomation | CustomerCreated, DealClosed, RiskScore≥70 | Status, playbooks, emails | `/Customers`, `/customer-success` |
| AutonomousOrchestration | Varios (gated) | Decisiones autónomas | `/TrustInbox`, Settings kill-switch |
| BusinessMemoryPipeline | Seleccionados | Episodios memoria | `/Memory` |

### 6.2 Workers RabbitMQ

| Agente | Evento | Efecto |
|--------|--------|--------|
| LeadIntelligenceAgent | LeadCreated | Score → LeadScoreUpdated |
| CommunicationAgent | LeadCreated, CustomerCreated | Email/WhatsApp bienvenida |
| CustomerRiskAgent | CustomerCreated | Risk score |
| CustomerHealthAgent | CustomerCreated | Playbooks rescue/adoption |
| ChurnRiskAgent | RiskScore≥60 | Acciones churn |
| DealStrategyAgent | DealCreated, StageChanged | Tareas inteligencia ventas |
| OutcomeAttribution | DealClosed/Lost | NBA ML + ABOS learning |

### 6.3 Jobs periódicos (`Worker.cs` — cada 15 min por tenant)

- Revenue scan (deals estancados, leads inactivos)
- Data quality tasks
- Retention scan
- Renewal / Expansion agents
- Intelligence scan
- Customer insights
- Ciclo autónomo completo

**Cada 6 h:** `BusinessMemoryConsolidationWorker`.

### 6.4 Limitaciones que el Admin debe conocer

| Componente | Estado real |
|------------|-------------|
| Workflow `Communicate` | Solo log |
| Workflow `ActivateAgent` | Solo log |
| AutomationOptimizerAgent | Solo log (TODO) |
| DataQualityGuardian | Registrado, no invocado |
| ComplianceSecurityAgent | No bloquea (TODO) |

### 6.5 Monitoreo Admin

- `/FailedEvents` — DLQ replay
- `/Audit` — trazabilidad
- `/Tasks` — tareas generadas
- Logs: `docker logs autonomuscrm-api`, `docker logs autonomuscrm-workers`

---

## Capítulo 7. Uso de IA (Command, Trust, Agents, Memory)

### 7.1 Command Center (`/`)

**Servicio:** `IAiCommandCenterService.GetFlowCommandAsync`

- Métricas: revenue generated/protected, cuentas en riesgo, expansiones, renovaciones.
- Decisiones en vivo y snapshot workforce.
- Periodo: 7 o 30 días (query param).
- Enlace directo a Trust Studio si `PendingApprovals > 0`.

### 7.2 Trust Studio (`/TrustInbox`)

- Cola HITL con severidad y SLA (`ITrustSlaService`).
- Acciones: Approve, Reject, Rollback, Simulate, SetThreshold.
- Métricas: `ITrustMetricsService.GetMetricsAsync`.
- **Admin y Manager** son los roles operativos de aprobación según documentación enterprise.

### 7.3 Workforce / Agents (`/Agents`)

- Vista de agentes autónomos y decisiones recientes.
- Complementar con `/command/decisions`, `/command/outcomes`, `/command/playbooks`.

### 7.4 Memory (`/Memory`)

**Servicio:** `ISemanticMemoryService.GetDashboardAsync`

- Timeline de memoria empresarial semántica.
- Estado del proveedor de embeddings.
- Consolidación cada 6 h vía worker.

### 7.5 Revenue OS y Executive OS

| Módulo | Ruta | Servicio | Uso Admin |
|--------|------|----------|-----------|
| Revenue OS | `/revenue` | `IRevenueOsService` + `IGraphReasoningEngine` | Fugas de ingreso |
| Executive OS | `/executive` | `IExecutiveOsService` | Home; export HTML |

### 7.6 API de IA (integraciones)

| Endpoint | Salida |
|----------|--------|
| `GET api/ai/ml/churn` | Predicciones churn |
| `GET api/ai/ml/expansion` | Oportunidades expansión |
| `GET api/ai/ml/revenue` | Forecast ML |
| `POST api/ai/enterprise-cycle` | Train + drift + graph |
| `GET api/ai/analytics` | Analytics ejecutivo |
| `GET api/ai/governance` | Reporte gobernanza |
| `GET api/ai/dashboard` | Executive AI dashboard |

### 7.7 Gate autónomo

`AutonomousRevenueDecisionEngine` combina health, churn V2, expansion, NPS/CSAT, memoria semántica. Controlado por `AutonomousPlatformGate` + **kill-switch** en Settings.

### 7.8 Expectativas realistas (Admin)

| Expectativa incorrecta | Realidad |
|------------------------|----------|
| "La IA cierra ventas sola" | Crea tareas y decisiones; humano o Trust aprueba |
| "ChatGPT en cada email" | CommunicationAgent usa templates configurados |
| "Trust vacío = IA rota" | Puede significar sin decisiones HITL pendientes |
| "LLM en workers producción" | `LlmAgentService` en tests; workers default sin LLM cableado |

---

## Capítulo 8. Reportes y analítica

### 8.1 Executive OS (`/executive`) — reporte principal Admin

- Dashboard consolidado dirección.
- Trust pending approvals.
- Export HTML (`?handler=Export`).

### 8.2 Command (`/`)

- Flow command metrics 7/30 días.
- Priorización diaria operativa.

### 8.3 Revenue OS (`/revenue`)

- Dashboard ingresos.
- `DetectRevenueLeakAsync` — explicación fugas pipeline.

### 8.4 Pipeline / Deals (`/Deals`)

| Métrica | Significado |
|---------|-------------|
| Forecast 30/60/90 | Suma ponderada deals abiertos por ventana cierre |
| Win Rate | Won / (Won + Lost) |
| Revenue Closed | Suma Amount ClosedWon |
| Pipeline Open | Suma Amount deals Open |

### 8.5 Leads (`/Leads`)

TotalCount, QualifiedCount, NewCount, HighScoreCount (>70), AvgScore, SourceStats.

### 8.6 Customers (`/Customers`)

TotalCount, AvgLtv, HighLtvCount (>10.000), HighRiskCount (>70), AvgRisk.

### 8.7 Tasks (`/Tasks`)

Conteos por tenant, overdue, filtros status/assignee/priority.

### 8.8 Audit (`/Audit`)

Event store paginado, conteos por tipo, **export JSON hasta 10.000 eventos**.

### 8.9 Trust Studio métricas

SLA aprobaciones, severidad cola, umbral configurado.

### 8.10 Memory, Integrations, Billing

- `/Memory` — dashboard memoria semántica.
- `/Integrations` — `IntegrationHealthDashboardDto`.
- `/billing` — `IBillingDashboardService`.

### 8.11 Cómo interpretar como Admin

1. **Executive OS** → salud global del negocio en el tenant.
2. **Audit** → quién cambió qué (Users, Settings, deals).
3. **Deals forecast** → compromiso con dirección; validar ExpectedCloseDate.
4. **Trust metrics** → gobernanza IA efectiva.
5. **No confundir** totales de página paginada (50 ítems) con cards de resumen SQL.

---

## Capítulo 9. Escenario real completo

### Contexto

**Empresa:** TechScale SaaS (tenant demo).  
**Admin:** admin@autonomuscrm.local.  
**Situación:** Lunes 8:00 — nuevo lead inbound, usuario Sales solicitado, decisión IA pendiente, deal estancado detectado por Revenue scan.

### Paso 1 — Inicio en Executive (8:00)

Admin inicia sesión → redirección `/executive`. Revisa:

- Forecast 30 días: $245.000 (3 deals en Negotiation).
- Trust pending: 2 (badge sidebar).
- Cuentas high risk: 4 en `/Customers` resumen.

### Paso 2 — Trust Studio (8:10)

En `/TrustInbox`, primera decisión severidad **high**: playbook de retención propone email automático a cliente RiskScore 72.

- Revisa Outcome Fabric y explicabilidad.
- **Approve** — decisión ejecutada; Audit registra aprobación con UserId Admin.

Segunda decisión: asignación automática lead score 85 → **Approve**.

### Paso 3 — Nuevo usuario Sales (8:25)

Gerente comercial solicita cuenta para nuevo ejecutivo.

- Admin abre `/Users/Create`.
- Email: `nuevo.vendedor@techscale.local`, rol Sales en `/Users/Edit`.
- Alternativa automatizada: `POST api/users` con JWT Admin (único método API permitido).

### Paso 4 — Lead inbound (8:40)

Lead creado vía web (`POST /api/leads` o UI):

- Estado `New`, fuente `Website`.
- Worker asigna score 78 en 2 minutos.
- RevenueAutomation crea tarea SLA 24h.

Admin verifica en `/Leads` que HighScoreCount incrementó y asigna manualmente si workflow no lo hizo.

### Paso 5 — Deal estancado (9:00)

Revenue OS señala deal "Acme Corp — Enterprise" 45 días en Proposal.

- Admin abre `/Deals/Details`, revisa historial en Audit.
- Crea tarea en `/Tasks` asignada al Sales owner: "Reactivar propuesta Acme".
- Notifica al Manager por canal interno.

### Paso 6 — Qualify y pipeline (9:30)

El Sales califica otro lead; Admin supervisa:

- Customer auto-creado, deal borrador $1.
- Admin edita deal → monto $48.000, etapa Qualification, ExpectedCloseDate +45 días.

### Paso 7 — Cierre de jornada Admin (18:00)

- Trust Studio: 0 pendientes.
- `/FailedEvents`: vacío.
- Export Audit parcial del día → archivo JSON archivado.
- Settings: kill-switch = false, integraciones email OK en `/Integrations`.

### Resultado

- Usuario Sales operativo.
- Decisiones IA gobernadas.
- Pipeline actualizado y deal estancado escalado.
- Trazabilidad completa en Audit.

---

## Capítulo 10. Errores comunes

| # | Error | Causa | Solución Admin |
|---|-------|-------|----------------|
| 1 | 403 en `POST api/users` con cuenta Manager | `RequireAdmin` solo Admin | Usar cuenta Admin o UI `/Users/Create` (Manager puede UI) |
| 2 | 403 en `POST api/tenants` | Misma política | Solo Admin |
| 3 | Sales reporta Access Denied en `/Users` | `[Authorize(Roles = "Admin,Manager")]` | Normal — Admin/Manager gestionan usuarios |
| 4 | Trust Studio vacío | Sin decisiones HITL pendientes | Revisar umbral en Trust; kill-switch; ciclo autónomo |
| 5 | Score lead siempre 0 | Worker no procesó | `/FailedEvents`, logs LeadIntelligenceAgent, replay |
| 6 | Deal borrador $1 tras Qualify | OperationalAutomation diseño | Editar monto en `/Deals/Edit` |
| 7 | Workflow no dispara | Inactivo o trigger incorrecto | `/Workflows/Edit` — verificar EventType |
| 8 | Communicate no envía email | Acción solo log | Usar CommunicationAgent vía eventos Lead/Customer |
| 9 | Forecast $0 | Sin ExpectedCloseDate | Sales/Admin actualizan deals |
| 10 | Email no enviado | Comms no configurado | `/Settings` + `/Integrations` |
| 11 | Integración OAuth falla | Callback/credenciales | Reconectar en `/Integrations` |
| 12 | MFA bloquea login | MFA sin setup usuario | `/Users/Edit` o `enable-mfa` API |
| 13 | Tareas no se crean | Worker caído | `docker ps`, reiniciar workers |
| 14 | Health 503 | Postgres/Redis down | Reiniciar docker compose |
| 15 | API comercial desde Support | Brecha UI vs API | Restringir tokens; política organizacional |
| 16 | Paginación muestra 50 ítems | Diseño SearchPagedAsync | Usar cards resumen y filtros |
| 17 | AssignRole acepta string libre | Sin whitelist estricta en dominio | Admin valida roles permitidos manualmente |
| 18 | Export Audit incompleto | Límite 10.000 | Filtrar por fecha y exportar por lotes |
| 19 | Memory sin embeddings | Provider no configurado | Config sección `AI` en deployment |
| 20 | Billing vacío | tenantId vacío en sesión | Re-login, verificar tenant |

---

## Capítulo 11. Preguntas frecuentes (FAQ) — Rol Admin

**Total: 100 preguntas numeradas específicas del rol Admin.**

### Acceso y identidad (1–10)

**1. ¿Cuál es el email del Admin demo?**  
`admin@autonomuscrm.local`, definido en `DemoRoleUsers.All`.

**2. ¿Cuál es la contraseña del Admin demo?**  
`Admin123!` — patrón `{Role}123!` en `DemoRoleUsers.PasswordFor("Admin")`.

**3. ¿A qué pantalla llego tras login como Admin?**  
A `/executive`, según `RoleHomeRedirect.GetHomePath` cuando el claim de rol contiene "Admin".

**4. ¿El Admin comparte home con algún otro rol?**  
Sí, con **Manager** — ambos redirigen a `/executive`.

**5. ¿Puedo cambiar el home del Admin sin código?**  
No. La redirección está hardcodeada en `RoleHomeRedirect.cs`.

**6. ¿El Admin puede iniciar sesión desde la lista de cuentas demo?**  
Sí. `Login.cshtml.cs` expone `DemoAccounts` desde `DemoRoleUsers.All`.

**7. ¿Qué nombre muestra el perfil Admin demo?**  
Admin Sistema (FirstName: Admin, LastName: Sistema).

**8. ¿El Admin necesita MFA obligatoriamente?**  
No por defecto en seed demo. MFA se habilita por usuario vía `EnableMfaCommand`.

**9. ¿Puedo tener varios usuarios Admin en un tenant?**  
Sí. El rol Admin se asigna vía `/Users/Roles`; no hay límite de uno en el código documentado.

**10. ¿Qué ocurre si un Admin pierde acceso MFA?**  
Otro Admin debe intervenir vía `/Users/Edit` o restauración de entorno; no hay flujo self-service documentado.

### Permisos exclusivos Admin (11–25)

**11. ¿Qué puede hacer el Admin que el Manager no puede vía API?**  
`POST api/tenants` y `POST api/users` — ambos requieren política `RequireAdmin`.

**12. ¿El Manager puede crear usuarios en UI?**  
Sí, `/Users/Create` autoriza `Admin,Manager`. La diferencia es la API REST POST.

**13. ¿Dónde está definida la política RequireAdmin?**  
`AuthorizationPolicies.cs` y `Extensions.AddAutonomusPolicies` — `RequireRole("Admin")`.

**14. ¿Qué controller usa RequireAdmin para tenants?**  
`TenantsController.CreateTenant` — `[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]`.

**15. ¿Qué controller usa RequireAdmin para users API?**  
`UsersController.CreateUser` — `[Authorize(Policy = "RequireAdmin")]`.

**16. ¿Puedo crear tenants desde la UI Razor?**  
No hay página `/Tenants/Create` documentada. Provisioning tenant es vía `POST api/tenants`.

**17. ¿El Admin puede leer cualquier tenant?**  
`GET api/tenants/{id}` requiere autenticación, no RequireAdmin. El aislamiento operativo depende del token/sesión del tenant activo.

**18. ¿RequireManager incluye al Admin?**  
Sí. `RequireRole("Admin", "Manager")` — Admin satisface políticas Manager y Sales.

**19. ¿El Admin está exento del CommercialWriteAuthorizationMiddleware?**  
Sí. Admin está en `WriteRoles = ["Admin", "Manager", "Sales"]`.

**20. ¿El Admin puede acceder a `/Settings`?**  
Sí. `[Authorize(Roles = "Admin,Manager")]` en `Settings.cshtml.cs`.

**21. ¿El Admin puede gestionar `/Users`?**  
Sí. Misma autorización Admin,Manager en `Users.cshtml.cs` y subpáginas.

**22. ¿El Admin puede usar Billing?**  
Sí. `Billing.cshtml.cs` no restringe por rol; el ítem sidebar es visible para todos.

**23. ¿El Admin puede aprobar en Trust Studio?**  
Sí. `TrustInbox.cshtml.cs` no añade restricción de rol; Admin y Manager operan HITL.

**24. ¿Puedo asignar rol Admin a mí mismo vía API sin ser Admin?**  
`AssignRole` en dominio no tiene whitelist estricta documentada, pero gestión UI requiere Admin/Manager. Buena práctica: solo Admin asigna Admin.

**25. ¿Qué políticas están registradas pero sin uso en endpoints comerciales?**  
`RequireManager` y `RequireSales` — documentado en matriz enterprise como no aplicadas en controllers comerciales.

### Menú y navegación Admin (26–40)

**26. ¿Cuántos ítems tiene el sidebar?**  
19, definidos en `_FlowSidebar.cshtml`.

**27. ¿Se ocultan ítems del menú al Admin?**  
No. Todos los ítems son visibles; las restricciones son por página/handlers.

**28. ¿Qué significa el badge numérico en Trust Studio?**  
Cantidad de aprobaciones pendientes (`trustPending` en sidebar model).

**29. ¿Cómo accedo a búsqueda global?**  
Ctrl+K → `/api/flow/search`.

**30. ¿Cuál es la ruta del Command Center?**  
`/` (Index).

**31. ¿Dónde está Workforce en el menú?**  
Sección Command → `/Agents`.

**32. ¿El Admin debe usar `/revenue` o `/executive`?**  
Home es `/executive`; `/revenue` es complementario para fugas de ingreso (home de Sales es `/revenue`).

**33. ¿Puedo acceder a `/Deals` como Admin?**  
Sí, con escritura completa.

**34. ¿Dónde está Failed Events?**  
`/FailedEvents` — no está en sidebar; acceso directo URL o búsqueda Ctrl+K.

**35. ¿El enlace del email de usuario en sidebar a dónde va?**  
A `/Settings`.

**36. ¿Qué secciones agrupan el menú?**  
Command, Revenue, Customers, Commerce, Intelligence, Operation, Platform, Admin.

**37. ¿Puedo ver Customer Success como Admin?**  
Sí, `/customer-success` sin restricción de rol documentada.

**38. ¿Voice Calls está disponible para Admin?**  
Sí, `/VoiceCalls` visible en sidebar.

**39. ¿Dónde configuro políticas ABAC?**  
`/Policies` — escritura permitida por CommercialWrite middleware.

**40. ¿Dónde veo playbooks autónomos fuera del sidebar?**  
`/command/playbooks`.

### Gestión de usuarios y tenant (41–55)

**41. ¿Cómo creo un usuario desde UI siendo Admin?**  
`/Users/Create` → OnPostAsync con `CreateUserCommand`.

**42. ¿Cómo creo un usuario vía API siendo Admin?**  
`POST api/users` con body CreateUserCommand y JWT Admin.

**43. ¿Puedo importar usuarios masivamente?**  
Sí, `/Users/Import` (Admin,Manager).

**44. ¿Dónde asigno roles?**  
`/Users/Roles` y `/Users/Edit/{id}` con `AssignRole`.

**45. ¿Qué roles existen en el sistema?**  
Admin, Manager, Sales, Support, Viewer — `DemoRoleUsers` y seed.

**46. ¿Cómo desactivo un usuario?**  
`/Users/Edit` — `ToggleUserStatus` command.

**47. ¿Cómo habilito MFA para un usuario?**  
`POST api/users/{id}/enable-mfa?tenantId={guid}`.

**48. ¿Cómo creo un nuevo tenant?**  
`POST api/tenants` con CreateTenantCommand — solo Admin.

**49. ¿Puedo exportar configuración del tenant?**  
Sí, handlers export en `/Settings`.

**50. ¿Puedo restaurar Settings por defecto?**  
Sí, `OnPostRestoreDefaultsAsync` en Settings con valores incluyendo `KillSwitch = false`.

**51. ¿Qué es el kill-switch en Settings?**  
Bandera que detiene ciclo autónomo cuando está activa (junto con `AutonomousPlatformGate`).

**52. ¿Dónde edito email del tenant?**  
`/Settings` — datos tenant en SystemSettings.

**53. ¿BulkActions en Users qué permite?**  
Acciones masivas sobre usuarios (`/Users/BulkActions`).

**54. ¿El Admin debe crear el primer usuario de un tenant nuevo?**  
Operativamente sí, tras `POST api/tenants`.

**55. ¿Puedo ver lista paginada de usuarios?**  
Sí, `/Users` con `FilteredUsers` paginado.

### Trust Studio y gobernanza IA (56–70)

**56. ¿Qué es Trust Studio para el Admin?**  
Buzón HITL en `/TrustInbox` para aprobar decisiones autónomas.

**57. ¿Qué acciones puedo ejecutar en Trust?**  
Approve, Reject, Rollback, Simulate, SetThreshold (50–95).

**58. ¿Approve ejecuta la decisión automáticamente?**  
Sí. `ApproveAsync(..., executeDecision: true)`.

**59. ¿Qué es Simulate en Trust?**  
Vista preview con `?preview=simulate` sin ejecutar.

**60. ¿Qué severidades muestra la cola?**  
critical, high, medium, low — mapeadas por SLA y RiskLevel.

**61. ¿Qué es Outcome Fabric en Trust?**  
`IOutcomeFabricService.GetStatusAsync` — impacto de la decisión seleccionada.

**62. ¿Puedo cambiar el umbral de aprobación automática?**  
Sí, `OnPostSetThresholdAsync` — rango 50–95.

**63. ¿Trust vacío es un error?**  
No necesariamente — puede indicar sin decisiones pendientes HITL.

**64. ¿Dónde escalo desde operación a Trust?**  
`/FlowActions` puede redirigir a TrustInbox con decisión pendiente.

**65. ¿El Admin debe revisar Trust diariamente?**  
Sí, es responsabilidad documentada en guía Manager/Admin.

**66. ¿Qué métricas SLA muestra Trust?**  
`ITrustSlaService` — aprobaciones overdue con severidad.

**67. ¿Rollback cuándo se usa?**  
Cuando una decisión aprobada debe revertirse con nota obligatoria.

**68. ¿Reject requiere nota?**  
Opcional (`note` nullable en handler).

**69. ¿Las aprobaciones quedan en Audit?**  
Sí, el sistema de eventos registra operaciones de dominio y trust.

**70. ¿Dónde veo historial completo de decisiones?**  
`/command/decisions` complementa Trust.

### Automatizaciones y operaciones (71–85)

**71. ¿El Admin configura workflows?**  
Sí, `/Workflows` con triggers, condiciones y acciones.

**72. ¿Qué acciones de workflow están limitadas?**  
`Communicate` y `ActivateAgent` solo log.

**73. ¿Cómo sé si un worker falló?**  
`/FailedEvents` y logs `autonomuscrm-workers`.

**74. ¿Cada cuánto corre el revenue scan?**  
Cada 15 minutos por tenant en `Worker.cs`.

**75. ¿Qué hace LeadIntelligenceAgent?**  
Score en LeadCreated → LeadScoreUpdated.

**76. ¿Puedo replay eventos DLQ?**  
Sí, desde `/FailedEvents` según UI disponible.

**77. ¿OperationalAutomation qué hace al Qualify?**  
Customer + deal borrador + tarea alta prioridad.

**78. ¿Cómo detengo automatizaciones autónomas en emergencia?**  
Kill-switch en Settings + revisar AutonomousPlatformGate.

**79. ¿Dónde monitoreo tareas generadas por IA?**  
`/Tasks` con filtros overdue y assignee.

**80. ¿RetentionAutomation cuándo actúa?**  
CustomerCreated, DealClosed, RiskScore≥70.

**81. ¿BusinessMemoryConsolidationWorker frecuencia?**  
Cada 6 horas.

**82. ¿El Admin debe revisar Integrations si hay emails fallidos?**  
Sí, salud OAuth/sync en `/Integrations`.

**83. ¿Qué playbook usar para lead inbound?**  
Playbook 1 en manual operativo: crear → SLA → contactar → Qualify.

**84. ¿Tres caminos Qualify/Convert/Create Deal — cuál imponer?**  
Admin define estándar único para el equipo; código permite los tres.

**85. ¿Dónde veo limitación AutomationOptimizerAgent?**  
Solo log — documentado en catálogo automatizaciones.

### Reportes, auditoría e integraciones (86–100)

**86. ¿Cuál es el reporte principal del Admin?**  
Executive OS en `/executive` con export HTML.

**87. ¿Cómo exporto auditoría?**  
`/Audit` — JSON hasta 10.000 eventos.

**88. ¿Qué métricas de Deals debe revisar el Admin semanalmente?**  
Forecast 30/60/90, Win Rate, Pipeline Open.

**89. ¿Dónde está el forecast ML avanzado?**  
`GET api/ai/ml/revenue` además de forecast SQL en `/Deals`.

**90. ¿Cómo interpreto HighRiskCount en Customers?**  
Clientes con RiskScore > 70 — riesgo churn/retención.

**91. ¿Revenue OS detecta fugas cómo?**  
`IGraphReasoningEngine.DetectRevenueLeakAsync`.

**92. ¿Dónde veo gobernanza IA agregada?**  
`GET api/ai/governance`.

**93. ¿Billing muestra qué?**  
`BillingDashboardDto` — suscripción tenant.

**94. ¿Memory dashboard qué incluye?**  
Timeline memoria + estado embedding provider.

**95. ¿Puedo exportar Executive board?**  
Sí, `?handler=Export` en Executive.

**96. ¿Audit muestra eventos de dominio?**  
Sí, event store paginado con conteos por tipo.

**97. ¿Integraciones soportadas documentadas?**  
HubSpot, Salesforce, email, Stripe en `/Integrations`.

**98. ¿Cómo valido salud del sistema?**  
Endpoint `/health` — 503 si Postgres/Redis down.

**99. ¿Qué hacer ante brecha API comercial Support/Viewer?**  
Admin restringe tokens API, política interna, monitoreo Audit.

**100. ¿Dónde está la evidencia técnica de este manual?**  
`DemoRoleUsers.cs`, `CommercialWriteAuthorizationMiddleware.cs`, `RoleHomeRedirect.cs`, `AuthorizationPolicies.cs`, `UsersController.cs`, `TenantsController.cs`, `_FlowSidebar.cshtml`, documentación enterprise en `docs/enterprise-manual/`.

---

*Documento generado para operación empresarial AutonomusCRM. Versión alineada al código del repositorio. Roles cubiertos: Admin únicamente.*
