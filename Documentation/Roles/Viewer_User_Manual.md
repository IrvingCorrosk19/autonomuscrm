# Manual de Usuario — Rol Viewer

**Perfil demo:** `viewer@autonomuscrm.local` / `Viewer123!`  
**Pantalla de inicio:** `/` (Command Center)  
**Modo de operación:** Solo lectura en interfaz comercial  
**Idioma:** Español (selector ES/EN disponible)

---

## Capítulo 1 — Introducción al rol Viewer

### 1.1 Propósito

El rol **Viewer** está diseñado para stakeholders que necesitan **consultar** información comercial y operativa sin modificar datos. Ejemplos: dirección financiera, auditores internos, consultores externos, equipo de marketing en modo consulta.

### 1.2 Posición en la organización

| Aspecto | Detalle |
|---------|---------|
| Rol en sistema | `Viewer` (uno de cinco roles definidos) |
| Autenticación | Requerida — no es acceso anónimo |
| Escritura comercial UI | Bloqueada por `CommercialWriteAuthorizationMiddleware` |
| API comercial | Autenticado — sin filtro de rol en todos los endpoints* |

\*Riesgo documentado: la UI protege al Viewer, pero algunos endpoints API aceptan POST de cualquier usuario autenticado. Operación estándar es solo lectura vía UI.

### 1.3 Usuario demo

| Campo | Valor |
|-------|-------|
| Email | viewer@autonomuscrm.local |
| Contraseña | Viewer123! |
| Nombre | Laura Consulta |
| Tenant | AutonomusCRM Demo (seed local) |

---

## Capítulo 2 — Acceso e inicio de sesión

### 2.1 Primer acceso

1. Abrir el navegador en la URL de AutonomusCRM.
2. Ir a `/Account/Login`.
3. Ingresar email y contraseña.
4. Si MFA está habilitado (`MfaRequired` en Settings), ingresar código TOTP.
5. Tras login exitoso, el sistema redirige a `/` (Command).

### 2.2 Redirección por rol

`RoleHomeRedirect.cs` define el destino post-login:

| Rol | Destino |
|-----|---------|
| Admin | `/executive` |
| Manager | `/executive` |
| Sales | `/revenue` |
| Support | `/Customer360` |
| **Viewer** | **`/`** |

El Viewer **no** es redirigido a Revenue OS ni Customer 360 automáticamente.

### 2.3 Cerrar sesión

Ir a `/Account/Logout` o usar el menú de usuario en la barra superior.

### 2.4 Cambio de idioma

Selector ES/EN en la interfaz. Los recursos de localización están en `localization-es.json` y `localization-en.json`.

---

## Capítulo 3 — Navegación y menú lateral

### 3.1 Estructura del menú (19 ítems)

| # | Sección | Ruta | Acceso Viewer |
|---|---------|------|:-------------:|
| 1 | Command | `/` | ✅ Lectura |
| 2 | Trust Studio | `/TrustInbox` | ✅ Lectura* |
| 3 | Workforce | `/Agents` | ✅ Lectura |
| 4 | Revenue OS | `/revenue` | ✅ Lectura |
| 5 | Executive | `/executive` | ✅ Lectura |
| 6 | Pipeline | `/Deals` | ✅ Lectura |
| 7 | Directory | `/Customers` | ✅ Lectura |
| 8 | Customer 360 | `/Customer360` | ✅ Lectura |
| 9 | Customer Success | `/customer-success` | ✅ Lectura |
| 10 | Leads | `/Leads` | ✅ Lectura |
| 11 | Memory | `/Memory` | ✅ Lectura |
| 12 | Tasks | `/Tasks` | ✅ Lectura |
| 13 | Integrations | `/Integrations` | ✅ Lectura |
| 14 | Voice | `/VoiceCalls` | ✅ Lectura |
| 15 | Users | `/Users` | ❌ Admin/Manager |
| 16 | Policies | `/Policies` | ✅ Lectura** |
| 17 | Audit | `/Audit` | ✅ Lectura |
| 18 | Settings | `/Settings` | ❌ Admin/Manager |
| 19 | Billing | `/billing` | ✅ Lectura |

\*Aprobación de decisiones IA requiere Admin/Manager en operación normal.  
\*\*Escritura de políticas bloqueada por middleware comercial.

### 3.2 Búsqueda global

Atajo `Ctrl+K` abre búsqueda que consulta `/api/flow/search`. Útil para localizar clientes, deals y leads rápidamente.

### 3.3 Rutas comerciales (solo lectura)

| Ruta | Contenido visible |
|------|-------------------|
| `/Leads/Details/{id}` | ✅ Detalle del lead |
| `/Leads/Create` | ❌ Access Denied |
| `/Leads/Edit/{id}` | ❌ Access Denied |
| `/Customers/Details/{id}` | ✅ Detalle del cliente |
| `/Customers/Create` | ❌ Access Denied |
| `/Deals/Details/{id}` | ✅ Detalle del deal |
| `/Deals/Create` | ❌ Access Denied |
| `/customers/{id}/360` | ✅ Vista 360 individual |

---

## Capítulo 4 — Command Center

### 4.1 Pantalla principal (`/`)

Command es el centro operativo de AutonomusCRM. Para el Viewer muestra:

- Decisiones de IA y métricas de flujo.
- Cuentas en riesgo priorizadas.
- Snapshot del workforce autónomo.
- Next Best Actions (NBA) recomendadas.

### 4.2 Qué puede hacer el Viewer

- Consultar métricas y paneles.
- Revisar decisiones pendientes y ejecutadas.
- Identificar prioridades para comunicar al equipo operativo.

### 4.3 Qué no puede hacer

- Aprobar o rechazar decisiones en Trust Studio (operación típica de Manager/Admin).
- Ejecutar acciones que modifiquen entidades comerciales.

### 4.4 Rutas relacionadas

| Ruta | Propósito |
|------|-----------|
| `/command/decisions` | Historial de decisiones |
| `/command/outcomes` | Outcome Fabric |
| `/command/playbooks` | Playbooks autónomos |

---

## Capítulo 5 — Revenue OS y Executive

### 5.1 Revenue OS (`/revenue`)

Dashboard de ingresos vía `IRevenueOsService`:

- KPIs de ingresos y pipeline.
- Forecast predictivo (30/60/90 días).
- Fugas de revenue identificadas.
- Insights priorizados por score.

**Uso Viewer:** monitorear salud comercial, preparar reportes para dirección, validar forecast antes de reuniones.

### 5.2 Executive (`/executive`)

Vista ejecutiva consolidada:

- Métricas de cierre (won/lost).
- Performance por vendedor.
- Resumen de operaciones del tenant.

**Uso Viewer:** reportes de board, análisis trimestral, auditoría de resultados.

### 5.3 API de consulta (referencia)

```http
GET /api/revenue/forecast?tenantId={guid}
```

Disponible para usuarios autenticados. El Viewer puede consumirla si tiene herramientas de reporting externas (con precaución de no ejecutar POST).

---

## Capítulo 6 — Leads, Customers y Deals (solo lectura)

### 6.1 Leads (`/Leads`)

**Visible:**

- Listado paginado con filtros.
- Estados: New, Contacted, Qualified, Converted, Lost, Unqualified.
- Fuentes: Website, Referral, SocialMedia, EmailCampaign, ColdCall, Partner, Event, Other.
- Estadísticas por fuente (`LeadSourceStat`).

**Bloqueado:**

- Crear lead (`/Leads/Create`).
- Editar lead (`/Leads/Edit/{id}`).
- Calificar, convertir, eliminar (acciones POST).
- Importación masiva.

### 6.2 Customers (`/Customers`)

**Visible:**

- Directorio paginado de clientes.
- Detalle en `/Customers/Details/{id}`.
- Estados: Prospect, Lead, Qualified, Customer, VIP, Churned, Inactive.

**Bloqueado:**

- Crear, editar, importar clientes.

### 6.3 Deals / Pipeline (`/Deals`)

**Visible:**

- Kanban por etapa: Prospecting, Qualification, Proposal, Negotiation, ClosedWon, ClosedLost.
- Listado con montos y probabilidades.
- Detalle en `/Deals/Details/{id}`.

**Bloqueado:**

- Crear, editar, cerrar deals.
- Acciones masivas de etapa.

### 6.4 Comportamiento del middleware

`CommercialWriteAuthorizationMiddleware` intercepta:

- POST a `/Leads`, `/Customers`, `/Deals`, `/Workflows`, `/Policies`.
- GET a rutas con segmentos `/Create` o `/Edit`.

Resultado: redirect a `/Account/AccessDenied`.

---

## Capítulo 7 — Customer 360 y Customer Success

### 7.1 Customer 360 (`/Customer360`)

Búsqueda unificada de clientes:

- Campo de búsqueda `Q` (nombre, email).
- Hasta 25 resultados por consulta.
- Detección de duplicados por email.
- Vista individual: `/customers/{id}/360`.

**Uso Viewer:** investigar historial de un cliente, verificar estado de cuenta, preparar informes de retención.

### 7.2 Customer Success (`/customer-success`)

Panel operativo de post-venta (lectura para Viewer):

| Panel | Contenido |
|-------|-----------|
| KPIs CS | Métricas del portafolio |
| Señales churn | Clientes en riesgo |
| Renovaciones | Próximas a vencer |
| Tickets | Abiertos y cerrados |
| Casos | Renewal, Recovery, Expansion, AtRisk |
| Health scores | Healthy, Warning, Critical |

**Bloqueado para Viewer:** crear tickets, casos, ejecutar playbooks (acciones POST).

### 7.3 Diferencia Support vs Viewer

| Capacidad | Support | Viewer |
|-----------|:-------:|:------:|
| Ver Customer 360 | ✅ | ✅ |
| Crear ticket CS | ✅ | ❌ |
| Ejecutar playbook | ✅ | ❌ |
| Crear lead/deal | ❌ | ❌ |

---

## Capítulo 8 — Tasks, Workflows y operaciones

### 8.1 Tasks (`/Tasks`)

Cola de tareas operativas y SLA:

- Filtros por estado, prioridad, vencidas.
- Tipos: SLA comercial, playbooks CS, tareas de workflow.
- Tareas SLA relevantes: `SLA_LeadContact24h`, `SLA_QualifiedFollowUp`, `SLA_DealAtRisk`.

**Uso Viewer:** monitorear cumplimiento de SLAs, identificar cuellos de botella.

### 8.2 Workflows (`/Workflows`)

Automatizaciones configurables del tenant. Viewer puede consultar reglas activas pero no crear ni editar.

### 8.3 Voice Calls (`/VoiceCalls`)

Registro de llamadas del tenant. Consulta de historial telefónico.

### 8.4 Agents (`/Agents`)

Workforce autónomo: agentes IA y decisiones recientes. Consulta del estado del sistema autónomo.

### 8.5 Memory (`/Memory`)

Memoria empresarial semántica. Consulta de conocimiento acumulado del tenant.

---

## Capítulo 9 — Auditoría, integraciones y plataforma

### 9.1 Audit (`/Audit`)

Event sourcing del tenant:

- Eventos de dominio con filtros por tipo y fecha.
- Conteo total y del día.
- Distribución por tipo de evento.

**Uso Viewer:** trazabilidad de cambios, soporte a auditorías de cumplimiento.

### 9.2 Integrations (`/Integrations`)

Estado de conexiones:

- HubSpot, Salesforce, Gmail, Outlook, Stripe.
- Health center de integraciones.
- Viewer consulta estado; no conecta ni sincroniza.

### 9.3 Billing (`/billing`)

Dashboard de suscripción y facturación del tenant (vía `IBillingDashboardService`).

### 9.4 Failed Events (`/FailedEvents`)

Cola de eventos no procesados. Viewer puede consultar para reportar incidentes a Admin.

### 9.5 Secciones restringidas

| Ruta | Roles permitidos |
|------|------------------|
| `/Users` | Admin, Manager |
| `/Settings` | Admin, Manager |
| `POST /api/users` | Admin |
| `POST /api/tenants` | Admin |

---

## Capítulo 10 — Buenas prácticas y flujo de trabajo

### 10.1 Rutina diaria recomendada

| Hora | Acción | Ruta |
|------|--------|------|
| Inicio | Revisar Command y prioridades | `/` |
| 09:00 | Consultar Revenue y forecast | `/revenue` |
| 10:00 | Revisar pipeline activo | `/Deals` |
| 11:00 | Verificar SLAs vencidos | `/Tasks` |
| 14:00 | Customer 360 — clientes clave | `/Customer360` |
| 17:00 | Preparar resumen para stakeholders | — |

### 10.2 Cuándo escalar

| Situación | Escalar a |
|-----------|-----------|
| Necesita crear/editar dato comercial | Sales o Manager |
| Ticket de soporte al cliente | Support |
| Cambio de configuración | Admin |
| Aprobación de decisión IA | Manager/Admin |
| Incidente técnico | Admin + `/FailedEvents` |

### 10.3 Informes desde rol Viewer

El Viewer es ideal para generar:

- Reportes de pipeline (exportar datos visualmente o vía API GET).
- Análisis de conversión por fuente de lead.
- Estado de salud del portafolio de clientes.
- Cumplimiento de SLAs operativos.

### 10.4 Seguridad

- No compartir credenciales Viewer con usuarios que necesiten escribir.
- Cerrar sesión al terminar consultas en equipos compartidos.
- Reportar acceso Access Denied inesperado a Admin (puede indicar cambio de rol necesario).

### 10.5 Solicitar elevación de permisos

Si el Viewer necesita operar (no solo consultar), solicitar a Admin:

| Necesidad | Rol recomendado |
|-----------|-----------------|
| Gestionar ventas | Sales |
| Soporte post-venta | Support |
| Administrar usuarios | Manager o Admin |
| Solo consulta ampliada | Mantener Viewer |

---

## Capítulo 11 — Preguntas frecuentes (100)

### Categoría A: Rol y acceso (1–15)

**1. ¿Qué es el rol Viewer?**  
Es un rol de solo lectura en la interfaz comercial de AutonomusCRM, diseñado para consulta sin modificación de leads, clientes ni deals.

**2. ¿Cuál es mi email y contraseña demo?**  
`viewer@autonomuscrm.local` / `Viewer123!`

**3. ¿A dónde me redirige el sistema al iniciar sesión?**  
A `/` (Command Center). El Viewer no va automáticamente a `/revenue` ni `/Customer360`.

**4. ¿Puedo cambiar mi contraseña?**  
Debe solicitarlo al Admin del tenant; no hay auto-servicio documentado para Viewer.

**5. ¿Necesito MFA?**  
Depende de la política del tenant (`MfaRequired` en `/Settings`). Si está activo, deberá configurar TOTP.

**6. ¿Cuántos roles existen en el sistema?**  
Cinco: Admin, Manager, Sales, Support, Viewer. No existe rol Marketing.

**7. ¿El Viewer es lo mismo que acceso anónimo?**  
No. Las páginas públicas (`/landing`, `/demo`) son anónimas. Viewer requiere autenticación.

**8. ¿Puedo ver datos de otro tenant?**  
No. Todos los datos están aislados por `TenantId`.

**9. ¿Puedo acceder a `/Users`?**  
No. Requiere rol Admin o Manager.

**10. ¿Puedo acceder a `/Settings`?**  
No. Requiere rol Admin o Manager.

**11. ¿Qué pasa si intento crear un lead?**  
Será redirigido a `/Account/AccessDenied` por el middleware comercial.

**12. ¿Puedo usar la API para crear datos?**  
Técnicamente algunos endpoints API aceptan POST de usuarios autenticados sin filtro de rol, pero la operación estándar del Viewer es solo lectura vía UI.

**13. ¿Puedo tener otro rol además de Viewer?**  
Un usuario puede tener múltiples roles. Si tiene Sales además de Viewer, heredará capacidades de escritura de Sales.

**14. ¿Cómo sé qué roles tengo?**  
Consultar al Admin en `/Users` o revisar claims del token JWT tras login.

**15. ¿El Viewer puede cerrar sesión de otros usuarios?**  
No. Esa es función de Admin/Manager.

### Categoría B: Navegación (16–30)

**16. ¿Cuántos ítems tiene el menú lateral?**  
19 ítems organizados en secciones: Command, Revenue, Customers, Commerce, Intelligence, Operations, Platform, Admin.

**17. ¿Cómo busco un cliente rápidamente?**  
Use `Ctrl+K` para búsqueda global o `/Customer360` con el campo Q.

**18. ¿Dónde veo el pipeline de ventas?**  
En `/Deals` — vista Kanban o listado.

**19. ¿Dónde veo los prospectos?**  
En `/Leads`.

**20. ¿Dónde veo el directorio de clientes?**  
En `/Customers`.

**21. ¿Qué es Command?**  
La pantalla de inicio operativo en `/` con decisiones IA, métricas y prioridades.

**22. ¿Puedo ver Revenue OS?**  
Sí, en `/revenue` — modo consulta.

**23. ¿Puedo ver la vista ejecutiva?**  
Sí, en `/executive`.

**24. ¿Dónde están las tareas del equipo?**  
En `/Tasks` con filtros de estado, prioridad y vencidas.

**25. ¿Dónde veo las integraciones activas?**  
En `/Integrations`.

**26. ¿Dónde consulto la auditoría?**  
En `/Audit`.

**27. ¿Dónde veo la facturación del tenant?**  
En `/billing`.

**28. ¿La ruta `/Support` funciona?**  
Redirige automáticamente a `/customer-success`.

**29. ¿Puedo ver Trust Studio?**  
Sí en lectura (`/TrustInbox`), pero aprobar/rechazar es para Admin/Manager.

**30. ¿Cómo cambio el idioma?**  
Selector ES/EN en la interfaz de la aplicación.

### Categoría C: Leads (31–45)

**31. ¿Qué es un Lead?**  
Un contacto potencial que aún no es cliente consolidado.

**32. ¿Puedo crear un lead?**  
No desde la UI. Solicite a Sales o Manager.

**33. ¿Cuáles son los estados de un lead?**  
New, Contacted, Qualified, Converted, Lost, Unqualified.

**34. ¿Con qué estado nace un lead?**  
Siempre en `New`.

**35. ¿Qué fuentes de origen existen?**  
Unknown, Website, Referral, SocialMedia, EmailCampaign, ColdCall, Partner, Event, Other.

**36. ¿Puedo calificar un lead?**  
No. La acción Qualify es para Sales/Manager/Admin.

**37. ¿Puedo ver el detalle de un lead?**  
Sí, en `/Leads/Details/{id}`.

**38. ¿Puedo importar leads?**  
No. La importación en `/Leads/Import` requiere rol de escritura.

**39. ¿Qué es el SLA de 24h para leads?**  
Tarea automática `SLA_LeadContact24h` que Sales debe completar en 24 horas.

**40. ¿Dónde veo estadísticas por fuente?**  
En `/Leads` — métricas `LeadSourceStat` (conteo y calificados por fuente).

**41. ¿Un lead calificado se convierte automáticamente en cliente?**  
El evento de calificación puede disparar creación de cliente y deal borrador vía automatizaciones.

**42. ¿Puedo ver leads perdidos?**  
Sí, filtrar por estado `Lost` en el listado.

**43. ¿Existe entidad "Prospecto" separada?**  
No. El prospecto inicial es un Lead con estado New.

**44. ¿Puedo eliminar un lead?**  
No desde rol Viewer.

**45. ¿Los leads de la landing entran automáticamente?**  
No automáticamente documentado. Entran vía importación, API o creación manual por Sales.

### Categoría D: Customers y Customer 360 (46–60)

**46. ¿Qué es un Customer?**  
La cuenta o cliente en el directorio del CRM.

**47. ¿Puedo crear un cliente?**  
No. Escritura bloqueada para Viewer.

**48. ¿Cuáles son los estados de un Customer?**  
Prospect, Lead, Qualified, Customer, VIP, Churned, Inactive.

**49. ¿Qué es Customer 360?**  
Vista unificada de un cliente: datos, deals, tickets, health score.

**50. ¿Cómo busco en Customer 360?**  
Ir a `/Customer360` e ingresar nombre o email en el campo Q.

**51. ¿Cuántos resultados muestra la búsqueda?**  
Hasta 25 por consulta.

**52. ¿Puedo ver duplicados de clientes?**  
Sí, el panel de duplicados por email aparece en `/Customer360`.

**53. ¿Puedo fusionar duplicados?**  
No. Escalar a Admin/Manager.

**54. ¿Qué es el health score?**  
Indicador de salud del cliente: Healthy, Warning o Critical.

**55. ¿Dónde veo el health de un cliente específico?**  
En `/customers/{id}/360` o en el panel de `/customer-success`.

**56. ¿Puedo ver tickets de un cliente?**  
Sí, en la vista 360 y en `/customer-success`.

**57. ¿Qué es un ticket CS?**  
Tarea con `TaskType = CS_Ticket`, vencimiento 3 días, prioridad configurable.

**58. ¿Puedo crear un ticket?**  
No. Esa acción es del rol Support.

**59. ¿Qué son los casos CS?**  
Tareas `CS_Case_Renewal`, `CS_Case_Recovery`, `CS_Case_Expansion`, `CS_Case_AtRisk`.

**60. ¿Puedo ejecutar playbooks de Customer Success?**  
No. Los playbooks (Onboarding, Rescue, ReEngagement, etc.) requieren acción POST de Support.

### Categoría E: Deals y pipeline (61–75)

**61. ¿Qué es un Deal?**  
Oportunidad de venta vinculada obligatoriamente a un Customer.

**62. ¿Cuáles son las etapas del pipeline?**  
Prospecting, Qualification, Proposal, Negotiation, ClosedWon, ClosedLost.

**63. ¿Puedo crear un deal?**  
No desde rol Viewer.

**64. ¿Puedo ver el Kanban de deals?**  
Sí, en `/Deals`.

**65. ¿Puedo ver el monto de un deal?**  
Sí, en detalle `/Deals/Details/{id}`.

**66. ¿Qué es ClosedWon?**  
Etapa de deal cerrado exitosamente.

**67. ¿Qué pasa después de ClosedWon?**  
Automatizaciones de retención pueden crear tareas de onboarding CS.

**68. ¿Puedo cerrar un deal?**  
No. Requiere Sales/Manager/Admin.

**69. ¿Qué es un deal estancado?**  
Deal sin movimiento que dispara tarea `SLA_DealAtRisk`.

**70. ¿Dónde veo deals ganados vs perdidos?**  
En `/Deals` filtrando por etapa, o en `/revenue` / `/executive`.

**71. ¿Un deal puede existir sin cliente?**  
No. Customer es obligatorio en la creación.

**72. ¿Puedo ver la probabilidad por etapa?**  
Sí, implícita en la etapa: Prospecting ~10%, Qualification ~25%, Proposal ~50%, Negotiation ~75%.

**73. ¿Puedo importar deals?**  
No desde rol Viewer.

**74. ¿Puedo hacer acciones masivas en deals?**  
No. `/Deals/BulkActions` requiere escritura.

**75. ¿Cómo reporto un deal con datos incorrectos?**  
Notificar al ejecutivo Sales asignado o al Manager.

### Categoría F: Revenue, forecast e IA (76–90)

**76. ¿Qué es Revenue OS?**  
Dashboard de ingresos en `/revenue` con KPIs, fugas y forecast.

**77. ¿Puedo ver el forecast?**  
Sí. Proyecciones a 30, 60 y 90 días.

**78. ¿Qué es el forecast ponderado?**  
Monto del pipeline ajustado por probabilidad de etapa.

**79. ¿Qué son las fugas de revenue?**  
Oportunidades o clientes identificados en riesgo de pérdida de ingreso.

**80. ¿Qué es Trust Studio?**  
Buzón HITL en `/TrustInbox` para aprobar decisiones autónomas de IA.

**81. ¿Puedo aprobar decisiones IA?**  
No en operación estándar. Es función de Admin/Manager.

**82. ¿Qué son los Agents?**  
Workforce autónomo visible en `/Agents` — agentes IA y decisiones recientes.

**83. ¿Qué es Memory?**  
Memoria empresarial semántica en `/Memory`.

**84. ¿Qué es el Outcome Fabric?**  
Registro de resultados de acciones autónomas en `/command/outcomes`.

**85. ¿Puedo ver playbooks autónomos?**  
Sí en consulta en `/command/playbooks`.

**86. ¿Qué es el kill-switch?**  
Configuración en Settings que desactiva operaciones autónomas. Viewer no puede modificarla.

**87. ¿Qué es el modo Supervised?**  
Modo de operación IA por defecto que requiere supervisión humana antes de impacto real.

**88. ¿Dónde veo el win rate?**  
En `/revenue` o `/executive`.

**89. ¿Puedo ver performance por vendedor?**  
Sí, en `/executive` vía `SalesPerformanceEngine`.

**90. ¿La IA puede crear tareas automáticamente?**  
Sí, vía automatizaciones de Revenue y Customer Success (SLA, playbooks, churn alerts).

### Categoría G: Operaciones, seguridad y escalamiento (91–100)

**91. ¿Dónde veo tareas SLA vencidas?**  
En `/Tasks` con filtro de overdue.

**92. ¿Qué es Failed Events?**  
Cola DLQ de eventos no procesados en `/FailedEvents`.

**93. ¿Puedo reprocesar un evento fallido?**  
La acción Replay está disponible autenticado; en la práctica es responsabilidad de Admin.

**94. ¿Dónde veo el historial de cambios?**  
En `/Audit` — event sourcing del tenant.

**95. ¿Qué integraciones soporta el sistema?**  
HubSpot, Salesforce, Gmail, Outlook, Stripe.

**96. ¿Puedo conectar una integración?**  
No. Configuración de Admin/Manager en `/Integrations`.

**97. ¿Cómo solicito permisos de escritura?**  
Contactar al Admin para evaluación de rol Sales, Support o Manager según necesidad.

**98. ¿Debo preocuparme por la brecha UI vs API?**  
Como Viewer, opere solo vía UI. No intente POST comerciales aunque esté autenticado.

**99. ¿A quién contacto si veo Access Denied?**  
Al Admin del tenant para verificar su rol asignado.

**100. ¿Dónde encuentro más documentación?**  
En `Documentation/`: guías por rol (Sales, Support, Admin), playbooks CS, onboarding y marketing.

---

*Manual basado en: `DemoRoleUsers.cs`, `RoleHomeRedirect.cs`, `CommercialWriteAuthorizationMiddleware.cs`, `Users/Roles.cshtml.cs`, `Lead.cs`, `Deal.cs`, `CustomerSuccessOsService.cs`.*
