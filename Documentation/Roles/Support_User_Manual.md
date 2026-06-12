# Manual de Usuario — Rol Support (Customer Success y Soporte)

**Versión:** 1.0.0  
**Fecha:** 5 de junio de 2026  
**Idioma:** Español  
**Rol:** Support  
**Credenciales demo:** `support@autonomuscrm.local` / `Support123!`  
**Home post-login:** `/Customer360`  
**Escritura comercial UI:** No (solo lectura en Leads/Customers/Deals)  
**Módulo principal:** `/customer-success` (Customer Success OS)  
**Base de evidencia:** Código fuente AutonomusCRM (`RoleHomeRedirect.cs`, `CommercialWriteAuthorizationMiddleware.cs`, `CustomerSuccess.cshtml`, inventario enterprise)

---

## Tabla de contenidos

1. [¿Qué es AutonomusCRM?](#capítulo-1--qué-es-autonomuscrm)
2. [Conceptos fundamentales](#capítulo-2--conceptos-fundamentales)
3. [Arquitectura funcional del negocio](#capítulo-3--arquitectura-funcional-del-negocio)
4. [Roles del sistema](#capítulo-4--roles-del-sistema)
5. [Navegación del sistema](#capítulo-5--navegación-del-sistema)
6. [Operación diaria de Support](#capítulo-6--operación-diaria-de-support)
7. [Customer 360](#capítulo-7--customer-360)
8. [Gestión de Clientes (lectura)](#capítulo-8--gestión-de-clientes-lectura)
9. [Customer Success y retención](#capítulo-9--customer-success-y-retención)
10. [Automatizaciones](#capítulo-10--automatizaciones)
11. [Preguntas frecuentes (100)](#capítulo-11--preguntas-frecuentes-100)

---

## Capítulo 1 — ¿Qué es AutonomusCRM?

### 1.1 Definición para el rol Support

AutonomusCRM es una plataforma de operaciones de ingresos y relación con clientes. Como usuario **Support**, su misión es el **post-venta**: retener clientes, gestionar tickets y casos, ejecutar playbooks de rescate y colaborar con ventas cuando detecte oportunidades — **sin modificar datos comerciales** desde la UI de Leads, Customers o Deals.

### 1.2 Qué resuelve en su día a día

| Necesidad | Módulo |
|-----------|--------|
| ¿Quién está en riesgo? | `/customer-success`, `/Customer360` |
| ¿Historial completo del cliente? | `/customers/{id}/360` |
| ¿Tickets abiertos? | `/customer-success` |
| ¿Qué tareas debo completar? | `/Tasks` |
| ¿Duplicados de identidad? | `/Customer360` |

### 1.3 Customer Success no es un rol

**Customer Success** es un **módulo** (`/customer-success`), no un rol RBAC. El rol que lo opera típicamente es **Support**. No existe rol Marketing ni SuperAdmin en el sistema.

### 1.4 Su cuenta demo

| Campo | Valor |
|-------|-------|
| Email | `support@autonomuscrm.local` |
| Contraseña | `Support123!` |
| Tras login | Redirección automática a `/Customer360` |
| Alias | `/Support` redirige a `/customer-success` |

### 1.5 Principio de veracidad

Este manual documenta únicamente capacidades verificadas. Se incluye la **brecha API** conocida: la UI bloquea escritura comercial a Support, pero ciertos `POST` REST solo exigen autenticación.

---

## Capítulo 2 — Conceptos fundamentales

### 2.1 Entidades que verá (mayormente lectura)

| Entidad | Su relación | Escritura UI Support |
|---------|-------------|----------------------|
| **Customer** | Foco principal | ❌ Create/Edit comercial |
| **Deal** | Contexto de compra | ❌ |
| **Lead** | Contexto pre-venta | ❌ |
| **Ticket / Case** | CS OS | ✅ en `/customer-success` |
| **Task** | Operativas CS | ✅ Completar; ⚠️ crear manual |

### 2.2 Estados del Customer relevantes para CS

`Prospect` → `Customer` → `VIP` | `Churned` | `Inactive`

Tras deal ganado o `CustomerCreatedEvent`, retención puede mover a estado **Customer** y ejecutar onboarding.

### 2.3 Salud de cuenta (Health)

`ICustomerHealthEngine` calcula puntuación y clasificación (incluida **Critical**). Visible en Customer Success OS y tablas de salud.

### 2.4 Riesgo y churn

- **RiskScore** en directorio de clientes (alerta > 70)  
- **Churn ML** (`IChurnPredictionV2`) en Customer 360: Alto ≥60%, Medio ≥35%  
- **Playbook Rescue** cuando RiskScore ≥ 70  

### 2.5 Playbooks de retención

| Playbook | Disparo típico |
|----------|----------------|
| Onboarding | CustomerCreated / deal ganado |
| Rescue | RiskScore ≥ 70 |
| ReEngagement | Sin contacto > 45 días |
| Renewal | Ventanas de renovación |
| Expansion | Oportunidades upsell |
| At Risk | Lista CS OS |

### 2.6 Tickets y casos

En `/customer-success`: tickets con prioridad Normal/High/Urgent, casos pendientes (`OpenCases`), cierre de tickets desde UI.

### 2.7 Tenant

Datos aislados por organización. Support solo ve su tenant.

---

## Capítulo 3 — Arquitectura funcional del negocio

### 3.1 Journey post-venta

```
Deal ClosedWon → Customer + LTV → RetentionAutomation
       ↓
Tareas onboarding D0/D7/D30 → Support en CS OS
       ↓
Health scan (15 min) → Rescue / Renewal / Expansion
```

### 3.2 Handoff desde Sales

Cuando Sales cierra un deal:
1. `DealClosedEvent` dispara retención  
2. Customer actualizado, LTV incrementado  
3. Tareas CS urgentes (Día 1) aparecen en `/Tasks`  
4. Email onboarding si Customer tiene email  
5. Support toma ownership en `/customer-success`  

### 3.3 RetentionAutomationEngine

En `CustomerCreatedEvent` y `DealClosedEvent`:
- Cambio estado Customer  
- Metadatos journey (`JourneyStage`, `OnboardingStarted`)  
- Playbook onboarding  
- Posible email plantilla Onboarding  

### 3.4 Scan de retención (cada 15 min)

`Worker.cs` por tenant:
- Persiste salud de todos los clientes  
- Ejecuta rescue en críticos  
- Emails de riesgo, ventanas renovación  
- Alertas churn, tareas expansión  
- WhatsApp re-engagement si hay teléfono (`IWhatsAppAutomationEngine`)  

### 3.5 Agentes CS en background

CustomerRiskAgent, CustomerHealthAgent, ChurnRiskAgent (RiskScore ≥ 60), RenewalEngine, ExpansionRevenueEngine.

### 3.6 Lo que Support no ejecuta

- Calificar leads (Sales)  
- Mover pipeline comercial (Sales)  
- Configurar tenant (`/Settings`)  
- Gestionar usuarios (`/Users`)  
- Aprobar Trust Studio (Manager/Admin)  

### 3.7 Brecha UI vs API (crítica)

| Capa | Support |
|------|---------|
| UI Razor Leads/Customers/Deals POST | **Bloqueado** (`CommercialWriteAuthorizationMiddleware`) |
| API `POST /api/leads`, `/customers`, `/deals` | **Autenticado sin filtro rol** ⚠️ |

**Política operativa:** Use solo la UI autorizada. Reporte a Admin cualquier uso indebido de API. No use la brecha como workaround.

---

## Capítulo 4 — Roles del sistema

### 4.1 Los cinco roles reales

| Rol | Home | Escritura comercial UI |
|-----|------|------------------------|
| Admin | `/executive` | Sí + admin |
| Manager | `/executive` | Sí + Users/Settings |
| Sales | `/revenue` | Sí |
| **Support** | **`/Customer360`** | **No** |
| Viewer | `/` | No |

### 4.2 Permisos del rol Support

**Puede:**
- Leer Leads, Customers, Deals (listas y detalle GET)  
- Usar Customer 360 (`/Customer360`, `/customers/{id}/360`)  
- Operar Customer Success OS (`/customer-success`): tickets, playbooks, casos  
- Completar tareas en `/Tasks`  
- Consultar Command, Trust Studio, Workforce (lectura)  
- Revenue OS y Executive (lectura 👁)  

**No puede (UI):**
- Crear/editar Leads, Customers, Deals  
- Qualify, Convert, Close deals  
- `/Users`, `/Settings`  
- Aprobar Trust HITL (típico Manager/Admin)  
- Configurar integraciones OAuth  

### 4.3 Matriz módulo × Support

| Módulo | Ruta | Support |
|--------|------|---------|
| Customer 360 | `/Customer360` | ✅ |
| Customer Success | `/customer-success` | ✅ |
| Tasks | `/Tasks` | ✅ |
| Customers Directory | `/Customers` | 👁 |
| Pipeline / Deals | `/Deals` | 👁 |
| Leads | `/Leads` | 👁 |
| Revenue OS | `/revenue` | 👁 |

### 4.4 Escalamiento

| Situación | Escalar a |
|-----------|-----------|
| Nueva oportunidad comercial | Sales |
| Cambio en deal o lead | Sales |
| Usuario bloqueado / MFA | Admin / Manager |
| Failed Events / workers caídos | Admin |
| Brecha API explotada | Admin (seguridad) |

---

## Capítulo 5 — Navegación del sistema

### 5.1 Inicio de sesión

1. `/Account/Login`  
2. `support@autonomuscrm.local` / `Support123!`  
3. Home: **`/Customer360`**  
4. Trabajo principal: **`/customer-success`**

### 5.2 Menú lateral — ítems clave Support

| Sección | Ruta | Uso Support |
|---------|------|-------------|
| Customers | **`/Customer360`** | **Home — búsqueda 360** |
| Customers | **`/customer-success`** | **CS OS — tickets, playbooks** |
| Customers | `/Customers` | Directorio (lectura) |
| Operations | `/Tasks` | Tareas CS y onboarding |
| Revenue | `/Deals` | Contexto pipeline (lectura) |
| Commerce | `/Leads` | Contexto pre-venta (lectura) |
| Revenue | `/revenue` | Métricas ingresos (lectura) |
| Command | `/` | Panorama IA (consulta) |
| Admin | `/Users`, `/Settings` | ❌ Access Denied |

### 5.3 Atajos

- **Ctrl+K** — búsqueda global `/api/flow/search`  
- **`/Support`** — redirect a `/customer-success`  
- **`/customers/{id}/360`** — vista detalle desde tickets o listas  

### 5.4 Errores de navegación

| Error | Consecuencia | Solución |
|-------|--------------|----------|
| Intentar crear Lead | Access Denied | Escalar a Sales |
| Editar Customer comercial | Bloqueado | Solo lectura; use CS OS para tickets |
| Ignorar `/Tasks` | SLA onboarding incumplido | Revisar D0/D7/D30 |
| Confundir home con CS OS | Pierde foco operativo | Home = 360; trabajo = customer-success |

---

## Capítulo 6 — Operación diaria de Support

### 6.1 Inicio de jornada (25 min)

| Minutos | Acción | Ruta |
|---------|--------|------|
| 0–5 | Login → Customer 360 | `/Customer360` |
| 5–10 | Revisar duplicados email | Panel duplicates en 360 |
| 10–15 | CS OS — clientes en riesgo | `/customer-success` |
| 15–20 | Tickets abiertos / overdue | CS OS sección Tickets |
| 20–25 | Tareas vencidas propias | `/Tasks?overdueOnly=true` |

### 6.2 Durante el día

| Evento | Acción |
|--------|--------|
| Cliente en riesgo crítico | Ejecutar playbook Rescue / At Risk |
| Ticket nuevo | Crear en CS OS, vincular Customer |
| Renovación próxima | Playbook Renewal en ventana |
| Oportunidad expansión | Playbook Expansion; escalar Sales si procede |
| Deal ganado ayer | Completar tarea D0 onboarding |

### 6.3 Fin de jornada (15 min)

1. Cerrar o actualizar tickets abiertos  
2. Completar tareas D0 urgentes  
3. Revisar casos pendientes (`OpenCases`)  
4. Documentar escalamientos a Sales  

### 6.4 KPIs visibles en CS OS

- AvgHealthScore  
- CustomersAtRisk  
- OpenTicketCount / OpenCaseCount  
- RenewalRatePercent  
- Tabla HealthSummary (Health, Adoption, Engagement, Classification)  

### 6.5 Colaboración con Sales

Support **lee** pipeline; Sales **escribe**. Si detecta upsell, notifique a Sales con CustomerId y contexto desde Customer 360 — no cree deals en UI (bloqueado).

---

## Capítulo 7 — Customer 360

### 7.1 Pantalla de búsqueda

**Ruta:** `/Customer360`  
**Servicio:** `ICustomer360Service.SearchAsync`  
**Parámetro:** `Q` — búsqueda por texto (hasta 25 resultados)

### 7.2 Contenido típico de resultados

Perfil consolidado: deals asociados, salud, riesgo churn ML, comunicaciones, grafo de relaciones, enlace a CS OS.

### 7.3 Vista detalle individual

**Ruta:** `/customers/{id}/360`  
Incluye bloque Customer Success con enlace **Abrir CS OS** si hay datos CS.

### 7.4 Detección de duplicados

`IIdentityResolutionService.FindDuplicatesByEmailAsync` — panel de grupos duplicados en `/Customer360`.  
Escalar fusión a Admin/Manager (`IdentityMergeService`); Support no fusiona desde UI comercial bloqueada.

### 7.5 Churn en 360

Modelo ML puede devolver:
- **Alto** ≥ 60%  
- **Medio** ≥ 35%  
- **Bajo** o sin predicción si historial insuficiente  

### 7.6 Uso operativo

1. Busque cliente por nombre/email  
2. Abra vista 360  
3. Revise health, deals cerrados, riesgo  
4. Salte a CS OS o cree ticket si procede  

### 7.7 Diferencia 360 vs Directorio

| Pantalla | Propósito |
|----------|-----------|
| `/Customers` | Lista paginada, métricas agregadas LTV/riesgo |
| `/Customer360` | Búsqueda unificada y duplicados |
| `/customers/{id}/360` | Ficha completa una cuenta |

---

## Capítulo 8 — Gestión de Clientes (lectura)

### 8.1 Directorio en lectura

**Ruta:** `/Customers`  
Support ve TotalCount, AvgLtv, HighRiskCount (RiskScore > 70), filtros y detalle — **sin** botones Create/Edit funcionales (middleware bloquea POST).

### 8.2 Estados y segmentos

Comprenda estados para priorizar: clientes **Churned** en analítica, **VIP** por segmentación automática, **Inactive** por procesos de merge.

### 8.3 LTV y riesgo

- **LTV** — valor acumulado; contexto de valor para playbooks Expansion  
- **RiskScore > 70** — alerta en directorio; activa playbooks Rescue  

### 8.4 Qué no debe intentar

- `/Customers/Create` → Access Denied  
- `/Customers/Edit` POST → Access Denied  
- Pedir a Sales crear cuenta si falta registro comercial  

### 8.5 Metadatos journey

Tras onboarding automático puede ver en detalle (lectura): `JourneyStage=Customer`, `OnboardingStarted` UTC.

### 8.6 Enlace con tickets

Al crear ticket en CS OS, seleccione Customer de lista desplegable (`Model.Customers`). Enlace desde ticket a `/customers/{id}/360`.

---

## Capítulo 9 — Customer Success y retención

### 9.1 Customer Success OS

**Ruta:** `/customer-success`  
**Servicio:** `ICustomerSuccessOsService`  
**Redirect:** `/Support` → aquí

### 9.2 Secciones de la pantalla

| Sección | Contenido |
|---------|-----------|
| KPIs hero | Salud media, en riesgo, tickets, casos, renovación % |
| At Risk | Hasta 8 clientes; playbook At Risk |
| Renewals | Ventanas renovación; playbook Renewal |
| Tickets | Crear, listar, cerrar tickets |
| Expansion | Recomendaciones upsell; playbook Expansion |
| Playbooks | Renewal, Rescue, Expansion, At Risk (manual por cliente) |
| Open Cases | Casos pendientes por tipo |
| Health Summary | Tabla 15 clientes con scores |

### 9.3 Crear ticket

Formulario POST `CreateTicket`:
- Customer (obligatorio)  
- Subject (obligatorio)  
- Priority: Normal, High, Urgent  

### 9.4 Cerrar ticket

POST `CloseTicket` con `ticketId`.

### 9.5 Ejecutar playbook

POST `RunPlaybook` con `customerId` y `playbookType`:
- `PlaybookAtRisk`  
- `PlaybookRenewal`  
- `PlaybookRescue`  
- `PlaybookExpansion`  

### 9.6 Playbooks automáticos (retención)

| Playbook | Motor |
|----------|-------|
| Onboarding | RetentionAutomation tras CustomerCreated / ClosedWon |
| Rescue | RiskScore ≥ 70, health Critical |
| ReEngagement | > 45 días sin contacto |
| Renewal | `RenewalEngine.EnforceRenewalWindowsAsync` |
| Expansion | `ExpansionRevenueEngine.CreateExpansionTasksAsync` |

### 9.7 Comunicaciones automáticas

- Email onboarding tras deal ganado (si email configurado)  
- Email riesgo en scan retención  
- WhatsApp re-engagement si teléfono presente  

**Nota:** Workflow `Communicate` en `/Workflows` solo log — no confundir con retención real.

### 9.8 Tareas CS tras deal ganado

Sales dispara; Support ejecuta:
- Día 1 — Urgent  
- Día 7 — Normal  
- Día 30 — Normal  

Visible y completable en `/Tasks`.

### 9.9 KPI retención

`CustomerKpiService` calcula retention rate entre retenidos y churned en periodo analizado.

---

## Capítulo 10 — Automatizaciones

### 10.1 Motores CS relevantes

| Motor | Evento / ciclo |
|-------|----------------|
| RetentionAutomation | CustomerCreated, DealClosed, RiskScore ≥ 70 |
| CustomerHealthAgent | CustomerCreated |
| ChurnRiskAgent | RiskScore ≥ 60 |
| RenewalEngine | Scan 15 min |
| ExpansionRevenueEngine | Scan 15 min |

### 10.2 Scan cada 15 minutos

Por tenant en `Worker.cs`:
- Persistir salud clientes  
- Rescue críticos  
- Emails riesgo, renovaciones, churn alerts  
- Tareas expansión  

### 10.3 BusinessMemoryConsolidationWorker

Cada **6 horas** — memoria semántica; Support lo percibe indirectamente en insights Command/Memory (lectura).

### 10.4 Workflows y Support

Support tiene lectura 👁 en workflows; escritura comercial de workflows limitada. Operación CS principal es CS OS + playbooks, no `/Workflows`.

### 10.5 Failed Events

Si playbook no ejecutó: revisar `/FailedEvents`, escalar Admin, verificar RabbitMQ/workers activos.

### 10.6 Limitaciones

- `Communicate` / `ActivateAgent` en WorkflowEngine: solo log  
- DataQualityGuardian registrado pero no invocado  
- ComplianceSecurityAgent no bloquea (TODO)  

### 10.7 API gap — recordatorio

La automatización de dominio corre en servidor con permisos de sistema. Support **no** debe replicar escritura comercial vía `POST /api/leads|customers|deals` — brecha de seguridad, no función soportada.

---

## Capítulo 11 — Preguntas frecuentes (100)

**Audiencia:** Support (`support@autonomuscrm.local`)  
**Fuente:** funcionalidades reales + brecha API documentada

---

### Categoría 1: Conceptos CS (1–10)

**1. ¿Cuál es mi misión como Support?**  
Retener clientes post-venta: tickets, casos, playbooks, tareas onboarding y monitoreo de salud — sin editar pipeline comercial en UI.

**2. ¿Customer Success es mi rol?**  
No. Es el **módulo** `/customer-success`. Su rol RBAC es **Support**.

**3. ¿Credenciales demo?**  
support@autonomuscrm.local / Support123!

**4. ¿A dónde voy tras login?**  
`/Customer360` según `RoleHomeRedirect.cs`.

**5. ¿Cuál es mi pantalla de trabajo principal?**  
`/customer-success` (Customer Success OS).

**6. ¿Qué es post-venta en AutonomusCRM?**  
Todo lo posterior a ClosedWon o CustomerCreated: onboarding, salud, renovación, rescate, expansión.

**7. ¿Debo gestionar Leads?**  
No como escritura. Leads son responsabilidad Sales; usted puede **leer** contexto en `/Leads`.

**8. ¿Existen roles SuperAdmin o Marketing?**  
No. Cinco roles: Admin, Manager, Sales, Support, Viewer.

**9. ¿Qué es un tenant?**  
Su organización aislada; solo ve datos de su TenantId.

**10. ¿Qué es salud de cuenta?**  
Puntuación de `ICustomerHealthEngine` con clasificación (incl. Critical) usada en CS OS y playbooks.

---

### Categoría 2: Customer 360 (11–25)

**11. ¿Qué es Customer 360?**  
Vista integral: perfil, deals, salud, churn ML, comunicaciones y relaciones.

**12. ¿Ruta de búsqueda?**  
`/Customer360` con parámetro `Q`.

**13. ¿Ruta detalle individual?**  
`/customers/{id}/360`.

**14. ¿Cuántos resultados devuelve búsqueda?**  
Hasta 25 (`SearchAsync`).

**15. ¿Dónde veo duplicados?**  
Panel duplicates en `/Customer360` vía `FindDuplicatesByEmailAsync`.

**16. ¿Puedo fusionar duplicados?**  
No desde su rol típico; escalar Admin (`IdentityMergeService`).

**17. ¿Churn Alto/Medio/Bajo?**  
ML: Alto ≥60%, Medio ≥35%; bajo o N/A sin historial.

**18. ¿Enlace desde 360 a CS?**  
Detalle incluye botón **Abrir CS OS** si hay bloque CustomerSuccess.

**19. ¿360 vs `/Customers`?**  
360 = búsqueda unificada y ficha rica; Customers = directorio paginado con métricas agregadas.

**20. ¿Puedo editar datos en 360?**  
No campos comerciales POST; operación CS vía tickets/playbooks en CS OS.

**21. ¿Qué servicio alimenta 360?**  
`ICustomer360Service` e identidad `IIdentityResolutionService`.

**22. ¿Veo deals del cliente en 360?**  
Sí, contexto de oportunidades en lectura.

**23. ¿Veo LTV?**  
Sí en contexto de perfil/directorio asociado.

**24. ¿Uso 360 cada mañana?**  
Sí, es su home; búsqueda y duplicados antes de CS OS.

**25. ¿Búsqueda global Ctrl+K?**  
Sí, `/api/flow/search` incluye rutas Customer 360.

---

### Categoría 3: Customer Success OS (26–40)

**26. ¿Ruta del CS OS?**  
`/customer-success`; `/Support` redirige aquí.

**27. ¿KPIs en hero?**  
AvgHealthScore, CustomersAtRisk, OpenTickets, OpenCases, RenewalRatePercent.

**28. ¿Sección At Risk?**  
Lista clientes en riesgo con severidad y playbook At Risk.

**29. ¿Sección Renewals?**  
Clientes en ventana renovación (días, valor anual, window).

**30. ¿Cómo creo ticket?**  
Formulario CreateTicket: Customer, Subject, Priority (Normal/High/Urgent).

**31. ¿Cómo cierro ticket?**  
Botón Close → POST CloseTicket con ticketId.

**32. ¿Tickets overdue?**  
Filas warn (`flow-row-warn`) si vencidos.

**33. ¿Sección Expansion?**  
Recomendaciones upsell con OpportunityType y playbook Expansion.

**34. ¿Playbooks manuales disponibles?**  
Renewal, Rescue (Protected), Expansion, At Risk — selector Customer + RunPlaybook.

**35. ¿Qué son Open Cases?**  
Casos pendientes con CaseTypeLabel, Customer, Title, Priority.

**36. ¿Tabla Health Summary?**  
Hasta 15 clientes: HealthScore, AdoptionScore, EngagementScore, Classification.

**37. ¿Insight actions en filas?**  
Partial `_FlowInsightActions` con returnUrl `/customer-success`.

**38. ¿Historial tickets cerrados?**  
Últimos 5 en sección History bajo tickets abiertos.

**39. ¿Servicio backend?**  
`ICustomerSuccessOsService.GetHomeAsync` (vía PageModel).

**40. ¿Puedo operar CS OS sin 360?**  
Sí, pero 360 es home ideal para contexto previo por cliente.

---

### Categoría 4: Clientes lectura (41–50)

**41. ¿Puedo crear Customer en UI?**  
No. POST bloqueado por middleware comercial.

**42. ¿Puedo editar Customer?**  
No en UI comercial. Solo lectura en `/Customers` y 360.

**43. ¿Estados Customer relevantes?**  
Prospect, Customer, VIP, Churned, Inactive.

**44. ¿RiskScore > 70?**  
Alerta en directorio; priorice playbook Rescue.

**45. ¿Qué es VIP?**  
Segmentación automática alto valor — no cambio manual Support.

**46. ¿Qué es Churned?**  
Cliente abandonado; usado en KPIs retención.

**47. ¿Métricas en `/Customers`?**  
TotalCount, AvgLtv, HighLtvCount, HighRiskCount, AvgRisk.

**48. ¿Cliente sin email?**  
Onboarding email automático no aplica; use teléfono/WhatsApp si disponible.

**49. ¿Falta Customer comercial?**  
Escalar Sales para alta en CRM, no API workaround.

**50. ¿Metadatos onboarding?**  
JourneyStage, OnboardingStarted tras eventos retención — lectura en detalle.

---

### Categoría 5: Tareas Support (51–60)

**51. ¿Dónde veo tareas?**  
`/Tasks`.

**52. ¿Puedo completar tareas?**  
Sí. Support tiene permiso Complete Task ✅.

**53. ¿Tareas onboarding D0/D7/D30?**  
Generadas al ClosedWon; Support las ejecuta.

**54. ¿Puedo crear tarea manual?**  
⚠️ Matriz indica posible brecha; operación estándar: completar existentes y tickets CS OS.

**55. ¿Tareas de expansión?**  
`ExpansionRevenueEngine` puede crearlas en scan 15 min.

**56. ¿Tareas rescue?**  
Generadas por retención/playbooks en clientes críticos.

**57. ¿Filtro overdue?**  
`/Tasks?overdueOnly=true` al inicio del día.

**58. ¿Estados tarea?**  
Open y Completed.

**59. ¿Vinculación entidad?**  
Tipo Customer/Deal + entityId para contexto.

**60. ¿Ignorar tareas onboarding?**  
Incumple SLA CS; cliente sin contacto D0 aumenta riesgo churn.

---

### Categoría 6: Retención y playbooks (61–75)

**61. ¿Playbook Onboarding cuándo?**  
CustomerCreatedEvent o post ClosedWon.

**62. ¿Playbook Rescue cuándo?**  
RiskScore ≥ 70 o health Critical.

**63. ¿ReEngagement cuándo?**  
Sin contacto > 45 días en scan retención.

**64. ¿Playbook Renewal?**  
`RenewalEngine` en ventanas de renovación.

**65. ¿Playbook Expansion?**  
Oportunidades upsell; coordinar Sales si cierra comercialmente.

**66. ¿Email onboarding automático?**  
Sí tras deal ganado si Customer tiene email.

**67. ¿WhatsApp automático?**  
Sí, plantillas re-engagement si teléfono en scan (`IWhatsAppAutomationEngine`).

**68. ¿Scan retención frecuencia?**  
Cada 15 minutos por tenant.

**69. ¿Qué hace scan retención?**  
Salud, rescue, emails riesgo, renovaciones, churn alerts, expansión.

**70. ¿RetentionAutomation en DealClosed?**  
Actualiza Customer, LTV, metadata compra, posible contrato anual.

**71. ¿CustomerRiskAgent?**  
Calcula risk score en CustomerCreated.

**72. ¿ChurnRiskAgent?**  
Acciones cuando RiskScore ≥ 60.

**73. ¿Ejecutar playbook desde CS OS?**  
POST RunPlaybook con customerId y playbookType.

**74. ¿Playbooks en `/command/playbooks`?**  
Vista estados autónomos — consulta; operación diaria en CS OS.

**75. ¿KPI retention rate?**  
`CustomerKpiService` en analítica CS.

---

### Categoría 7: Permisos y brecha API (76–85)

**76. ¿Por qué Access Denied al crear Lead?**  
`CommercialWriteAuthorizationMiddleware` bloquea POST comercial UI para Support.

**77. ¿Puedo calificar Lead?**  
No en UI. Solo Admin, Manager, Sales.

**78. ¿Puedo mover deal de etapa?**  
No en UI. Escalar Sales.

**79. ¿Puedo entrar `/Users`?**  
No. Admin/Manager only.

**80. ¿Puedo entrar `/Settings`?**  
No.

**81. ¿Qué es la brecha API?**  
`POST /api/leads`, `/customers`, `/deals` exigen autenticación **sin** filtro rol — UI bloquea, API no.

**82. ¿Debo usar API para escribir comercial?**  
**No.** Es riesgo de seguridad; reporte a Admin.

**83. ¿Support vs Viewer en API?**  
Ambos bloqueados en UI; ambos ⚠️ en API comercial POST.

**84. ¿RequireSales aplicado en API?**  
Registrado pero no aplicado en controllers comerciales según inventario.

**85. ¿Qué reportar a Admin?**  
Intentos de bypass API, Failed Events, integraciones rotas.

---

### Categoría 8: Navegación Support (86–93)

**86. ¿Cuántos ítems menú lateral?**  
19 — verá Users/Settings pero Access Denied al entrar.

**87. ¿Revenue OS para Support?**  
Lectura 👁 en `/revenue` — contexto ingresos, no edición.

**88. ¿Deals lectura?**  
Sí `/Deals` 👁 — contexto pipeline sin POST.

**89. ¿Leads lectura?**  
Sí `/Leads` 👁.

**90. ¿Trust Studio?**  
Lectura 👁; aprobación HITL Manager/Admin.

**91. ¿Command Center?**  
Consulta métricas IA 7/30 días en `/`.

**92. ¿Integraciones `/Integrations`?**  
Lectura 👁; OAuth típico Admin/Manager.

**93. ¿Failed Events?**  
`/FailedEvents` lectura — escalar Admin si eventos CS fallan.

---

### Categoría 9: Automatizaciones y errores (94–100)

**94. ¿Workflow Communicate envía email?**  
No — solo log. Retención real usa motores CS distintos.

**95. ¿Workers usan LLM?**  
No en `AutonomusCRM.Workers`; ML enterprise sí para churn/expansión.

**96. ¿Playbook no ejecutó?**  
Revise `/FailedEvents`, workers Docker, RabbitMQ; escale Admin.

**97. ¿No veo churn en 360?**  
ML sin historial suficiente — riesgo bajo o sin bullet.

**98. ¿Cliente en riesgo pero sin ticket?**  
Cree ticket Urgent en CS OS y ejecute playbook At Risk.

**99. ¿Oportunidad expansión detectada?**  
Playbook Expansion + notificar Sales con CustomerId — no crear deal usted.

**100. ¿Práctica seguridad Support?**  
Use solo UI autorizada; no explote brecha API; escale oportunidades a Sales y incidentes a Admin; complete tareas D0/D7/D30 y cierre tickets abiertos diariamente.

---

*Fin del manual Support — 11 capítulos, 100 FAQ. Referencias: `Documentation/ROLE_PERMISSION_MATRIX.md`, `docs/enterprise-manual/05_AUTOMATION_CATALOG.md`, `AutonomusCRM.API/Pages/CustomerSuccess.cshtml`.*
