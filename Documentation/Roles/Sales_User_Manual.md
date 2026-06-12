# Manual de Usuario — Rol Sales (Ejecutivo de Ventas)

**Versión:** 1.0.0  
**Fecha:** 5 de junio de 2026  
**Idioma:** Español  
**Rol:** Sales  
**Credenciales demo:** `sales@autonomuscrm.local` / `Sales123!`  
**Home post-login:** `/revenue` (Revenue OS)  
**Escritura comercial UI:** Sí  
**Restricciones:** Sin acceso a `/Users` ni `/Settings`  
**Base de evidencia:** Código fuente AutonomusCRM (`RoleHomeRedirect.cs`, `CommercialWriteAuthorizationMiddleware.cs`, inventario enterprise)

---

## Tabla de contenidos

1. [¿Qué es AutonomusCRM?](#capítulo-1--qué-es-autonomuscrm)
2. [Conceptos fundamentales](#capítulo-2--conceptos-fundamentales)
3. [Arquitectura funcional del negocio](#capítulo-3--arquitectura-funcional-del-negocio)
4. [Roles del sistema](#capítulo-4--roles-del-sistema)
5. [Navegación del sistema](#capítulo-5--navegación-del-sistema)
6. [Operación diaria del ejecutivo de ventas](#capítulo-6--operación-diaria-del-ejecutivo-de-ventas)
7. [Gestión de Leads](#capítulo-7--gestión-de-leads)
8. [Gestión de Clientes](#capítulo-8--gestión-de-clientes)
9. [Pipeline comercial (Deals)](#capítulo-9--pipeline-comercial-deals)
10. [Automatizaciones](#capítulo-10--automatizaciones)
11. [Preguntas frecuentes (100)](#capítulo-11--preguntas-frecuentes-100)

---

## Capítulo 1 — ¿Qué es AutonomusCRM?

### 1.1 Definición para el ejecutivo de ventas

AutonomusCRM es una plataforma web de **operaciones de ingresos y relación con clientes**. Como ejecutivo Sales, la usará para registrar prospectos, mover oportunidades por el pipeline, cumplir tareas de seguimiento y priorizar acciones desde Revenue OS.

No es una hoja de cálculo ni un buzón de correo: es el **sistema único de verdad** del ciclo comercial de su tenant (organización).

### 1.2 Qué resuelve en su día a día

| Necesidad | Módulo |
|-----------|--------|
| ¿A quién debo llamar hoy? | `/Tasks`, `/Leads` |
| ¿Dónde está mi pipeline? | `/Deals` |
| ¿Dónde pierdo ingresos? | `/revenue` |
| ¿Quién es este contacto? | `/Customers`, `/Customer360` |

### 1.3 Componentes que verá (sin administrar)

La interfaz autenticada usa el shell **AutonomusFlow**: barra superior (búsqueda Ctrl+K, idioma ES/EN, modo oscuro) y menú lateral con 19 ítems. Como Sales verá todos los enlaces, pero **Users** y **Settings** le devolverán Access Denied.

### 1.4 Su cuenta demo

| Campo | Valor |
|-------|-------|
| Email | `sales@autonomuscrm.local` |
| Contraseña | `Sales123!` |
| Nombre demo | Ana Ventas |
| Tras login | Redirección automática a `/revenue` |

### 1.5 Principio de veracidad

Este manual describe **solo funcionalidades verificadas en código**. No se documentan roles inexistentes (SuperAdmin, Marketing) ni capacidades no implementadas (por ejemplo, la acción de workflow `Communicate` solo registra log).

---

## Capítulo 2 — Conceptos fundamentales

### 2.1 Lead, Customer y Deal

| Entidad | Qué es | Ruta principal |
|---------|--------|----------------|
| **Lead** | Prospecto aún no consolidado como cuenta | `/Leads` |
| **Customer** | Cuenta o cliente en el directorio | `/Customers` |
| **Deal** | Oportunidad de venta vinculada a un Customer | `/Deals` |

**No existe** una entidad separada llamada "Prospecto": el prospecto inicial es un Lead en estado `New`.

### 2.2 Estados del Lead

`New` → `Contacted` → `Qualified` → `Converted` | `Lost` | `Unqualified`

### 2.3 Estados del Customer

`Prospect`, `Lead`, `Qualified`, `Customer`, `VIP`, `Churned`, `Inactive`

### 2.4 Etapas del Deal (pipeline)

`Prospecting` (10%) → `Qualification` (25%) → `Proposal` (50%) → `Negotiation` (75%) → `ClosedWon` (100%) / `ClosedLost` (0%)

Estados de deal independientes de la etapa: `Open`, `Closed`, `OnHold`, `Cancelled`.

### 2.5 Tareas (Tasks)

Las tareas en `/Tasks` usan estados `Open` y `Completed`. Se vinculan a Lead, Deal o Customer mediante tipo de entidad e `entityId`.

### 2.6 Tenant

Todos los datos pertenecen a su organización (`TenantId`). Solo verá registros de su tenant.

### 2.7 Revenue OS

Módulo en `/revenue` que agrega ingresos, detecta **fugas de pipeline** (deals estancados, leads inactivos) y explica prioridades mediante grafo de razonamiento.

### 2.8 Automatización de calificación (Qualify)

Al calificar un Lead, el sistema puede crear automáticamente Customer (por email), deal borrador y tarea de seguimiento 24 h — ver Capítulo 10.

---

## Capítulo 3 — Arquitectura funcional del negocio

### 3.1 Journey comercial implementado

```
Lead.New → Contacted → Qualified → [Deal borrador] → Pipeline → ClosedWon
                ↓
         Convert to Customer → Customer.Prospect/Customer → Retención CS
```

### 3.2 Flujo de creación de Lead

**Disparadores:** `/Leads/Create`, `POST /api/leads`, import CSV/JSON  
**Estado inicial:** `New`  
**Evento:** `LeadCreatedEvent`  
**Automatizaciones:** WorkflowEngine, RevenueAutomation (SLA 24 h), LeadIntelligenceAgent (score), CommunicationAgent (email si configurado)

### 3.3 Flujo de calificación (Qualify)

**UI:** `/Leads/Details` → **Qualify**  
**Command:** `QualifyLeadCommand`  
**OperationalAutomationService:**
1. Crea Customer si no existe (mismo email)
2. Crea Deal borrador (`Amount=1`, `IsDraft=true`)
3. Crea WorkflowTask de seguimiento alta prioridad

El Lead **no** pasa a `Converted` en este path; queda en `Qualified`.

### 3.4 Conversión manual Lead → Customer

**UI:** Convert to Customer en Details  
Crea Customer `Prospect`, marca Lead `Converted`, dispara `CustomerCreatedEvent` y retención.

### 3.5 Crear Deal desde Lead

**UI:** Create Deal — busca/crea Customer por email, crea Deal en `Prospecting`/`Open` **sin** cambiar estado del Lead.

### 3.6 Cierre ganado (ClosedWon)

`CloseDealCommand` → `DealClosedEvent` → retención (LTV, estado Customer), tareas onboarding CS D0/D7/D30, OutcomeAttribution.

### 3.7 Post-venta (colaboración con Support)

Tras ClosedWon, Support trabaja en `/customer-success` y Customer 360. Sales debe completar sus tareas comerciales y dejar datos limpios en el deal.

### 3.8 Capas del producto relevantes para Sales

| Capa | Uso Sales |
|------|-----------|
| Revenue OS | Priorización matutina |
| Command `/` | Panorama IA y workforce (consulta) |
| Workers (15 min) | Escaneos de revenue, deals estancados |
| Trust Studio | Solo lectura típica; aprobación HITL es Manager/Admin |

---

## Capítulo 4 — Roles del sistema

### 4.1 Los cinco roles reales

| Rol | Home | Escritura comercial UI |
|-----|------|------------------------|
| Admin | `/executive` | Sí + administración |
| Manager | `/executive` | Sí + Users/Settings |
| **Sales** | **`/revenue`** | **Sí** |
| Support | `/Customer360` | No (solo lectura comercial) |
| Viewer | `/` | No |

**No existen:** SuperAdmin, Marketing, Customer Success (como rol), Operations, Executive, Analyst.

### 4.2 Permisos del rol Sales

**Puede:**
- Crear, editar, calificar, convertir y eliminar Leads
- Crear y editar Customers y Deals
- Cerrar deals (won/lost), importar leads/deals
- Usar Revenue OS, Leads, Deals, Tasks, Customers, VoiceCalls
- Consultar Command, Trust Studio, Workforce (lectura)
- Completar tareas; crear tareas manuales

**No puede:**
- Gestionar usuarios (`/Users`) — Access Denied
- Configurar tenant (`/Settings`) — Access Denied
- Aprobar decisiones en Trust Studio (operación típica Manager/Admin)
- `POST /api/tenants` ni `POST /api/users`

### 4.3 Colaboración con otros roles

| Situación | Escalar a |
|-----------|-----------|
| Nuevo usuario o cambio de rol | Manager / Admin |
| Cliente en riesgo post-venta | Support (`/customer-success`) |
| Workflow roto o Failed Events | Admin |
| Política ABAC | Manager / Admin (`/Policies`) |

### 4.4 Prioridad de roles múltiples

Si un usuario tuviera varios roles, el home usa: Admin > Manager > Sales > Support > default.

---

## Capítulo 5 — Navegación del sistema

### 5.1 Inicio de sesión

1. Ir a `/Account/Login`
2. Email: `sales@autonomuscrm.local`
3. Contraseña: `Sales123!`
4. Llegará a **Revenue OS** (`/revenue`)

### 5.2 Menú lateral — ítems clave para Sales

| Sección | Ruta | Uso diario Sales |
|---------|------|------------------|
| Command | `/` | Panorama IA (secundario) |
| Revenue | **`/revenue`** | **Home — prioridades** |
| Revenue | `/Deals` | Pipeline kanban |
| Customers | `/Customers` | Directorio cuentas |
| Customers | `/Customer360` | Búsqueda 360 |
| Commerce | `/Leads` | Prospectos |
| Operations | `/Tasks` | Tareas del día |
| Platform | `/VoiceCalls` | Log de llamadas |
| Admin | `/Users` | ❌ Access Denied |
| Admin | `/Settings` | ❌ Access Denied |

### 5.3 Búsqueda global

**Ctrl+K** → `/api/flow/search` — localiza Leads, Deals, clientes y pantallas.

### 5.4 Rutas comerciales críticas (no siempre en sidebar)

| Ruta | Propósito |
|------|-----------|
| `/Leads/Create`, `/Edit`, `/Details` | CRUD lead |
| `/Customers/Create`, `/Edit`, `/Details` | CRUD cliente |
| `/Deals/Create`, `/Edit`, `/Details` | CRUD deal |
| `/Deals/Import` | Importación masiva |
| `/customers/{id}/360` | Vista 360 individual |
| `/Workflows` | Automatizaciones (Sales puede escribir) |

### 5.5 Errores de navegación frecuentes

| Error | Solución |
|-------|----------|
| Confundir `/` con `/revenue` | Empiece el día en `/revenue` |
| Intentar `/Users` | Solicitar a Manager |
| No revisar `/Tasks` | Revisar overdue cada mañana |

### 5.6 Idioma

Selector ES/EN en barra superior; contenido operativo de este manual en español.

---

## Capítulo 6 — Operación diaria del ejecutivo de ventas

### 6.1 Inicio de jornada (20 min)

| Minutos | Acción | Ruta |
|---------|--------|------|
| 0–5 | Login → Revenue OS | `/revenue` |
| 5–10 | Tareas vencidas | `/Tasks?overdueOnly=true` |
| 10–15 | Leads nuevos (`New`) | `/Leads?status=0` |
| 15–20 | Deals en Proposal/Negotiation | `/Deals` kanban |

### 6.2 Durante el día

| Evento | Acción CRM |
|--------|------------|
| Llamada completada | Completar tarea; Lead → `Contacted` si aplica |
| Propuesta enviada | Deal → **Proposal** |
| Objeción precio | Deal → **Negotiation** |
| Cierre verbal | **Close** deal; verificar tareas onboarding |
| Sin interés | Lead → `Lost` o `Unqualified` |

### 6.3 Fin de jornada (15 min)

1. `/Tasks` — cero overdue propios si es posible  
2. `/Leads` — ningún `New` sin contacto > 24 h  
3. `/Deals` — etapas actualizadas  
4. `/` — un insight relevante de Command (opcional)

### 6.4 Las cuatro pantallas diarias

1. **Revenue OS** — prioridades de ingresos  
2. **Leads** — nuevos contactos  
3. **Pipeline** — oportunidades activas  
4. **Tasks** — qué hacer hoy  

### 6.5 Registro de llamadas

`/VoiceCalls` — log manual de llamadas comerciales significativas.

---

## Capítulo 7 — Gestión de Leads

### 7.1 Pantalla principal

**Ruta:** `/Leads`  
**Métricas:** TotalCount, QualifiedCount, NewCount, HighScoreCount (score > 70), AvgScore, SourceStats

### 7.2 Crear lead

1. `/Leads` → Nuevo lead  
2. Campos: Nombre (obligatorio), Email, Teléfono, Empresa, Fuente  
3. Guardar → estado **New**

**Fuentes:** Website, Referral, SocialMedia, EmailCampaign, ColdCall, Partner, Event, Other, Unknown

### 7.3 Score del lead

Puntuación 0–100. `LeadIntelligenceAgent` actualiza tras `LeadCreatedEvent` → `LeadScoreUpdatedEvent`.

### 7.4 Calificar (Qualify) — acción central

1. `/Leads/Details/{id}` → **Qualify**  
2. Lead → `Qualified`  
3. **Automático:** Customer (si no existe), deal borrador, tarea 24 h High  

**Regla:** No deje leads `New` más de 24 h — existe SLA comercial.

### 7.5 Qualify vs Convert vs Create Deal

| Acción | Resultado |
|--------|-----------|
| **Qualify** | Qualified + Customer auto + deal borrador + tarea |
| **Convert to Customer** | Lead Converted + Customer creado |
| **Create Deal** | Deal sin cambiar estado del Lead |

### 7.6 Asignación

Campo `AssignedToUserId` manual o vía workflow `Assign`.

### 7.7 Operaciones masivas

`BulkUpdateLeadStatus` disponible según pantalla.

### 7.8 Importación

Import CSV/JSON dispara `LeadCreatedEvent` igual que creación manual.

---

## Capítulo 8 — Gestión de Clientes

### 8.1 Directorio

**Ruta:** `/Customers` — listado paginado, LTV, riesgo  
**Métricas:** TotalCount, AvgLtv, HighLtvCount, HighRiskCount (RiskScore > 70), AvgRisk

### 8.2 Crear cliente

`/Customers/Create` o formulario en listado — estado inicial **Prospect**.

### 8.3 Edición

Sales puede editar email, teléfono, empresa desde `/Customers/Edit`.

### 8.4 Customer 360

`/Customer360` — búsqueda; `/customers/{id}/360` — perfil, deals, salud, churn ML, comunicaciones.

### 8.5 LTV

Se incrementa al cerrar deal ganado (suma del monto al LTV existente).

### 8.6 Creación automática

Al **Qualify** un lead, si no hay Customer con el mismo email (case-insensitive), se crea uno.

---

## Capítulo 9 — Pipeline comercial (Deals)

### 9.1 Pantalla Pipeline

**Ruta:** `/Deals` — kanban por etapa + tabla con forecast

### 9.2 Crear deal

1. `/Deals/Create` o desde Lead Details  
2. **Customer obligatorio**  
3. Título, monto, descripción, Expected Close Date  

Nace en `Prospecting` / `Open` / probabilidad 10%.

### 9.3 Deal borrador tras Qualify

Monto simbólico **$1**, `IsDraft=true`, metadata `LeadId`. Actualice monto real en `/Deals/Edit`.

### 9.4 Mover etapas

Actualice etapa según avance real: Qualification → Proposal → Negotiation.

Probabilidades por defecto se ajustan por etapa; puede modificar manualmente 0–100%.

### 9.5 Forecast 30/60/90

Suma ponderada (Amount × Probability) por ventana de cierre esperada.

### 9.6 Cierre

| Resultado | Acción | Evento |
|-----------|--------|--------|
| Ganado | Close | `DealClosedEvent`, ClosedWon |
| Perdido | Lose | `DealLostEvent`, ClosedLost |

### 9.7 Post ClosedWon

Retención actualiza Customer, LTV, onboarding; tareas CS D0, D7, D30; emails onboarding si hay email.

### 9.8 Revenue OS y pipeline

Use `/revenue` para detectar deals estancados y fugas antes de revisar kanban.

### 9.9 Import y bulk

`/Deals/Import` — importación masiva  
`BulkUpdateDealStage` — cambio de etapa en lote

### 9.10 Win Rate

Métrica ClosedWon / (ClosedWon + ClosedLost) en agregados del listado.

---

## Capítulo 10 — Automatizaciones

### 10.1 Motores relevantes para Sales

| Motor | Cuándo actúa |
|-------|--------------|
| **OperationalAutomation** | LeadQualified, DealClosed |
| **RevenueAutomation** | LeadCreated, LeadScoreUpdated, LeadQualified |
| **WorkflowEngine** | Eventos con workflow activo |
| **CommercialSlaEngine** | SLA 24 h leads nuevos |

### 10.2 Al calificar un Lead (detalle)

1. `QualifyLeadCommand` → `LeadQualifiedEvent`  
2. OperationalAutomation: Customer + deal draft + tarea "Seguimiento lead calificado"  
3. Deduplicación: no crea segundo deal si ya existe con mismo `LeadId` en metadata  

### 10.3 Al cerrar deal ganado

- Tareas onboarding: Día 1 (Urgent), Día 7, Día 30  
- RetentionAutomation: estado Customer, LTV, playbook onboarding  
- Email plantilla Onboarding si Customer tiene email  

### 10.4 Workers cada 15 minutos

Revenue scan (deals estancados, leads inactivos), data quality, retención, renovación, expansión — pueden generar tareas en `/Tasks`.

### 10.5 Workflows configurables

**Ruta:** `/Workflows`  
**Triggers:** p. ej. `Lead.Created`  
**Acciones:** Assign, UpdateStatus, CreateTask, Communicate (solo log), ActivateAgent (solo log)

### 10.6 Agentes RabbitMQ (tiempo real)

LeadIntelligenceAgent, DealStrategyAgent, CommunicationAgent, RevenueAutomation, OutcomeAttribution.

### 10.7 Limitaciones honestas

- `Communicate` y `ActivateAgent` en workflows **no envían** mensajes ni activan LLM — solo log  
- Workers de fondo **no usan LLM**  
- IA en Command/Revenue prioriza y sugiere; no sustituye carga comercial inicial  

### 10.8 Monitoreo

Si una automatización falla: `/FailedEvents` (escalar Admin), `/Tasks` (ver tareas generadas), `/Audit` (solo lectura Sales).

---

## Capítulo 11 — Preguntas frecuentes (100)

**Audiencia:** Ejecutivo Sales (`sales@autonomuscrm.local`)  
**Fuente:** funcionalidades reales documentadas en código e inventario enterprise

---

### Categoría 1: Conceptos CRM (1–10)

**1. ¿Qué es AutonomusCRM para un vendedor?**  
Plataforma web que centraliza leads, clientes, deals, tareas y analítica de ingresos en un tenant aislado de su empresa.

**2. ¿Qué significa CRM en mi trabajo diario?**  
Herramienta para registrar contactos, seguir oportunidades, cumplir tareas y medir ventas sin hojas de cálculo dispersas.

**3. ¿Existe entidad "Prospecto" separada?**  
No. El prospecto es un **Lead** con estado `New`.

**4. ¿Diferencia entre Lead, Customer y Deal?**  
Lead = contacto potencial. Customer = cuenta en directorio. Deal = oportunidad vinculada obligatoriamente a un Customer.

**5. ¿Qué es un tenant?**  
Su organización aislada; todos los datos tienen `TenantId` y no ve registros de otras empresas.

**6. ¿Qué es el pipeline?**  
Recorrido de una oportunidad desde prospección hasta ClosedWon o ClosedLost, visible en `/Deals`.

**7. ¿Qué es Revenue OS?**  
Dashboard en `/revenue` con ingresos, fugas de pipeline y explicación en grafo para priorizar acciones.

**8. ¿Qué es Command Center?**  
Pantalla `/` con métricas de revenue generado/protegido, cuentas en riesgo y snapshot del workforce IA (periodo 7 o 30 días).

**9. ¿AutonomusCRM reemplaza email o teléfono?**  
No. Registra actividad y crea tareas; usted ejecuta la comunicación real.

**10. ¿Necesito conocimientos técnicos?**  
No. Opera desde formularios web en `/revenue`, `/Leads`, `/Deals`, `/Customers` y `/Tasks`.

---

### Categoría 2: Leads (11–25)

**11. ¿Dónde gestiono prospectos?**  
En `/Leads`, menú Commerce.

**12. ¿Estados de un Lead?**  
New, Contacted, Qualified, Converted, Lost, Unqualified.

**13. ¿Con qué estado nace un Lead?**  
Siempre **New** (UI, API o import).

**14. ¿Fuentes de origen?**  
Website, Referral, SocialMedia, EmailCampaign, ColdCall, Partner, Event, Other, Unknown.

**15. ¿Qué es el score?**  
Puntuación 0–100; `LeadIntelligenceAgent` puede actualizarla tras crear el lead.

**16. ¿Cómo califico un Lead?**  
`/Leads/Details/{id}` → **Qualify**. Solo Admin, Manager y Sales en UI.

**17. ¿Qué ocurre al calificar?**  
Lead → Qualified; puede crearse Customer, deal borrador ($1, IsDraft) y tarea 24 h High.

**18. ¿Qualify convierte el Lead a Converted?**  
No. Queda en **Qualified**. Converted requiere **Convert to Customer**.

**19. ¿Cómo convierto Lead en cliente?**  
**Convert to Customer** en Details → Customer Prospect, Lead Converted, `CustomerCreatedEvent`.

**20. ¿Puedo crear Deal sin convertir Lead?**  
Sí. **Create Deal** crea oportunidad sin cambiar estado del Lead.

**21. ¿Diferencia Qualify, Convert y Create Deal?**  
Qualify = pipeline automático. Convert = cliente administrativo. Create Deal = oportunidad directa.

**22. ¿Puedo asignar Lead a un vendedor?**  
Sí, campo `AssignedToUserId` o workflow Assign.

**23. ¿Lost vs Unqualified?**  
Lost = descartado tras contacto. Unqualified = no cumple criterio mínimo.

**24. ¿Operaciones masivas en Leads?**  
Existe `BulkUpdateLeadStatus` según UI disponible.

**25. ¿Import dispara automatizaciones?**  
Sí. Genera `LeadCreatedEvent` como creación manual (workflows, SLA, score).

---

### Categoría 3: Clientes (26–35)

**26. ¿Dónde veo clientes?**  
`/Customers`, sección Customers del menú.

**27. ¿Estados de Customer?**  
Prospect, Lead, Qualified, Customer, VIP, Churned, Inactive.

**28. ¿Estado al crear manualmente?**  
**Prospect**.

**29. ¿Cuándo pasa a Customer?**  
Tras `CustomerCreatedEvent` o `DealClosedEvent` (retención).

**30. ¿Qué es VIP?**  
Segmento alto valor vía `CustomerSegmentationEngine`, no cambio manual típico.

**31. ¿Qué es Customer 360?**  
Vista integral en `/Customer360` y `/customers/{id}/360`: perfil, deals, salud, churn, comunicaciones.

**32. ¿Qué es LTV?**  
Lifetime Value; se incrementa al cerrar deal ganado.

**33. ¿Puedo editar email y teléfono?**  
Sí, en `/Customers/Edit` como Sales.

**34. ¿Se crea Customer al calificar Lead?**  
Sí, si no existe Customer con el mismo email.

**35. ¿Metadatos de onboarding?**  
Retención puede escribir JourneyStage y OnboardingStarted tras `CustomerCreatedEvent`.

---

### Categoría 4: Deals y pipeline (36–50)

**36. ¿Dónde gestiono oportunidades?**  
`/Deals`, menú Revenue → Pipeline (kanban + tabla).

**37. ¿Etapas del Deal?**  
Prospecting, Qualification, Proposal, Negotiation, ClosedWon, ClosedLost.

**38. ¿Estados del Deal?**  
Open, Closed, OnHold, Cancelled (independientes de etapa visual).

**39. ¿Etapa y probabilidad al crear?**  
Prospecting, Open, probabilidad 10% por defecto.

**40. ¿Probabilidad por etapa?**  
10%, 25%, 50%, 75%, 100%, 0% — ajustable manualmente 0–100.

**41. ¿Deal sin Customer?**  
No. `CustomerId` es obligatorio.

**42. ¿Qué es deal borrador?**  
Tras Qualify: Amount=1, IsDraft=true, metadata LeadId. Actualice monto en Edit.

**43. ¿Cómo cierro ganado?**  
Close en UI → ClosedWon, `DealClosedEvent`, automatizaciones retención.

**44. ¿Cómo registro perdido?**  
Lose → ClosedLost, probabilidad 0%.

**45. ¿Forecast 30/60/90?**  
Suma ponderada por ventana de Expected Close Date.

**46. ¿Importar deals?**  
Sí, `/Deals/Import`.

**47. ¿Cambiar etapa en lote?**  
Sí, `BulkUpdateDealStage`.

**48. ¿Qué pasa tras ClosedWon?**  
Customer actualizado, LTV, tareas CS D0/D7/D30, emails onboarding posibles.

**49. ¿Asignar Deal?**  
`AssignToUser` o workflow Assign sobre agregado Deal.

**50. ¿Qué es Win Rate?**  
ClosedWon / (ClosedWon + ClosedLost) en métricas del listado.

---

### Categoría 5: Tareas (51–60)

**51. ¿Dónde veo tareas?**  
`/Tasks`, menú Operations.

**52. ¿Estados de tarea?**  
Open y Completed (strings, sin enum).

**53. ¿Quién crea tareas automáticas?**  
WorkflowEngine, OperationalAutomation, RevenueAutomation, RetentionAutomation, CommercialSlaEngine, worker 15 min.

**54. ¿Tarea al calificar Lead?**  
"Seguimiento lead calificado" — contactar en 24 h, prioridad High, tipo FollowUp.

**55. ¿Tareas al ganar deal?**  
Onboarding CS: Día 1 (Urgent), Día 7, Día 30.

**56. ¿Completar tarea en UI?**  
Sí, `CompleteWorkflowTask` en `/Tasks`.

**57. ¿Asignar tarea a otro usuario?**  
Sí, `AssignWorkflowTask`.

**58. ¿Consecuencia de ignorar tareas?**  
SLA incumplido, alertas en revenue scan cada 15 min, métricas overdue.

**59. ¿Tareas vinculadas a entidades?**  
Sí, tipo Lead/Deal/Customer + entityId; deduplicación `ExistsOpenTaskAsync`.

**60. ¿Crear tareas manualmente?**  
Sí, Sales puede crear desde `/FlowActions` y handlers CreateTask.

---

### Categoría 6: Revenue OS (61–70)

**61. ¿Por qué mi home es `/revenue`?**  
`RoleHomeRedirect.cs` envía rol Sales a Revenue OS tras login.

**62. ¿Qué muestra Revenue OS?**  
Dashboard ingresos, fugas (`DetectRevenueLeakAsync`), acciones sugeridas.

**63. ¿Protocolo matutino en Revenue OS?**  
Abrir fugas → abrir entidad → ejecutar acción → completar tarea si existe.

**64. ¿Revenue OS vs Command?**  
Revenue = qué vender hoy. Command = panorama IA y workforce 7/30 días.

**65. ¿Puedo ver Executive OS?**  
Sí en `/executive` (consulta); home de Manager/Admin, no pantalla diaria Sales.

**66. ¿Qué fugas detecta?**  
Deals estancados, leads inactivos, cuentas en riesgo según grafo de razonamiento.

**67. ¿Revenue scan cada 15 min?**  
Worker revisa tenants y puede crear tareas por deals/leads estancados.

**68. ¿Métricas Revenue Closed?**  
Suma Amount de deals ClosedWon.

**69. ¿Pipeline Open?**  
Suma montos deals estado Open en resumen kanban.

**70. ¿Debo usar Revenue OS cada mañana?**  
Sí, es la pantalla de inicio recomendada para priorizar ingresos.

---

### Categoría 7: Roles y permisos Sales (71–80)

**71. ¿Qué roles existen?**  
Admin, Manager, Sales, Support, Viewer — cinco roles reales.

**72. ¿Credenciales demo Sales?**  
sales@autonomuscrm.local / Sales123!

**73. ¿Qué puede hacer Sales?**  
CRUD comercial Leads/Customers/Deals, Qualify, Revenue OS, Tasks, VoiceCalls.

**74. ¿Puedo entrar a `/Users`?**  
No. Requiere Admin o Manager — recibirá Access Denied.

**75. ¿Puedo entrar a `/Settings`?**  
No. Configuración tenant es Admin/Manager.

**76. ¿Puedo aprobar Trust Studio?**  
Operación típica Manager/Admin; Sales suele tener lectura en `/TrustInbox`.

**77. ¿Existen SuperAdmin o Marketing como roles?**  
No. Admin es máximo privilegio; Marketing son páginas públicas sin rol.

**78. ¿Support puede calificar mis leads?**  
No en UI comercial. Leads son responsabilidad Sales.

**79. ¿Quién gestiona roles?**  
Admin y Manager en `/Users/Roles`.

**80. ¿Varios roles en un usuario?**  
Modelo admite colección; home usa prioridad Admin > Manager > Sales > Support.

---

### Categoría 8: Navegación (81–90)

**81. ¿A dónde voy tras login?**  
`/revenue` automáticamente.

**82. ¿Cuántos ítems tiene el menú?**  
19 ítems en siete secciones (Command, Revenue, Customers, Commerce, Intelligence, Operations, Admin).

**83. ¿Búsqueda rápida?**  
Ctrl+K → `/api/flow/search`.

**84. ¿Dónde está kanban?**  
`/Deals`.

**85. ¿Dónde configuro workflows?**  
`/Workflows` — Sales puede escribir según matriz de permisos.

**86. ¿Dónde veo auditoría?**  
`/Audit` — lectura; gestión Admin/Manager.

**87. ¿Integraciones?**  
`/Integrations` — configuración típica Admin/Manager; Sales usa datos sincronizados.

**88. ¿Rutas marketing públicas?**  
`/landing`, `/roi`, `/demo`, `/stories`, `/pricing` — sin login.

**89. ¿Dónde registro llamadas?**  
`/VoiceCalls`.

**90. ¿Dónde veo eventos fallidos?**  
`/FailedEvents` — escalar a Admin si afecta automatizaciones.

---

### Categoría 9: Automatizaciones (91–100)

**91. ¿Qué dispara un workflow?**  
Evento de dominio (p. ej. Lead.Created) con workflow activo en tenant.

**92. ¿Acciones de workflow?**  
Assign, UpdateStatus, CreateTask, Communicate, ActivateAgent.

**93. ¿Communicate envía emails?**  
No en implementación actual — solo log en WorkflowEngine.

**94. ¿Retención al crear Customer?**  
RetentionAutomation en CustomerCreatedEvent: estado, metadatos journey, playbook onboarding.

**95. ¿Frecuencia del worker?**  
Cada **15 minutos** por tenant en `Worker.cs`.

**96. ¿Qué ejecuta el scan 15 min?**  
Revenue, calidad datos, retención, renovación, expansión, inteligencia, ciclo autónomo.

**97. ¿Agentes en tiempo real?**  
LeadIntelligenceAgent, DealStrategyAgent, CommunicationAgent vía RabbitMQ.

**98. ¿SLA comercial leads nuevos?**  
CommercialSlaEngine — tarea seguimiento 24 h tras eventos lead.

**99. ¿Qualify duplica deal borrador?**  
No si ya existe deal con metadata LeadId igual.

**100. ¿Qué debo reportar como Sales en seguridad?**  
Use solo su cuenta; no comparta contraseñas demo; reporte a Admin si detecta que Support/Viewer escriben vía API sin control de rol (brecha documentada en UI vs API).

---

*Fin del manual Sales — 11 capítulos, 100 FAQ. Referencias: `Documentation/ROLE_PERMISSION_MATRIX.md`, `docs/enterprise-manual/02_BUSINESS_FLOWS.md`, `docs/enterprise-manual/05_AUTOMATION_CATALOG.md`.*
