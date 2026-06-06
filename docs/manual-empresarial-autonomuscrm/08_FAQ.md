# 08 — Preguntas Frecuentes (FAQ) AutonomusCRM

**Audiencia:** Ejecutivo de ventas sin experiencia previa en CRM  
**Fuente:** funcionalidades reales documentadas en el código y manuales enterprise de AutonomusCRM  
**Total:** 150 preguntas numeradas de forma continua (1–150)

---

## Categoría 1: Conceptos CRM

### 1. ¿Qué es AutonomusCRM?
AutonomusCRM es una plataforma de operaciones de ingresos y clientes que centraliza prospectos (leads), clientes, oportunidades de venta (deals), tareas y analítica de ingresos en un solo sistema web autenticado.

### 2. ¿Qué significa CRM en la práctica diaria?
CRM significa Customer Relationship Management: una herramienta para registrar contactos comerciales, seguir oportunidades, asignar tareas y medir resultados de ventas sin depender de hojas de cálculo dispersas.

### 3. ¿Existe una entidad separada llamada "Prospecto"?
No. En AutonomusCRM el prospecto inicial es un **Lead** con estado `New`. No hay una entidad independiente llamada Prospecto en el dominio del sistema.

### 4. ¿Cuál es la diferencia entre Lead, Customer y Deal?
Un **Lead** es un contacto potencial aún no consolidado como cuenta. Un **Customer** es la cuenta o cliente en el directorio. Un **Deal** es una oportunidad de venta concreta vinculada obligatoriamente a un Customer.

### 5. ¿Qué es un tenant en AutonomusCRM?
Un tenant es la organización o empresa aislada dentro del sistema. Todos los leads, clientes y deals pertenecen a un `TenantId`; los usuarios solo ven datos de su tenant.

### 6. ¿Qué es el pipeline comercial?
El pipeline es el recorrido de una oportunidad desde prospección hasta cierre. En AutonomusCRM se representa principalmente en `/Deals` con etapas desde Prospecting hasta ClosedWon o ClosedLost.

### 7. ¿Qué son los estados de un Lead frente a los de un Customer?
Son ciclos distintos. El Lead usa estados como New, Contacted, Qualified, Converted, Lost y Unqualified. El Customer usa Prospect, Lead, Qualified, Customer, VIP, Churned e Inactive.

### 8. ¿Qué es Revenue OS?
Revenue OS es el módulo de ingresos accesible en `/revenue`. Muestra dashboard unificado de ingresos, fugas de pipeline y explicaciones del grafo de razonamiento para priorizar acciones comerciales.

### 9. ¿Qué es Command en la interfaz?
Command es la pantalla de inicio operativo en `/` (también llamada Command Center). Presenta decisiones de IA, métricas de flujo, cuentas en riesgo y un snapshot del workforce autónomo.

### 10. ¿Qué es Trust Studio?
Trust Studio (`/TrustInbox`) es el buzón de aprobaciones humanas (HITL) donde Admin y Manager pueden aprobar o rechazar decisiones autónomas de IA antes de que se ejecuten.

### 11. ¿AutonomusCRM reemplaza el correo electrónico o las llamadas?
No lo reemplaza. Registra la actividad comercial, crea tareas de seguimiento y conecta datos; las comunicaciones reales las realiza el ejecutivo o automatizaciones configuradas (email/WhatsApp en módulos de retención).

### 12. ¿Necesito conocimientos técnicos para usar el CRM como vendedor?
No. Como ejecutivo de ventas puede operar desde `/revenue`, `/Leads`, `/Deals`, `/Customers` y `/Tasks` con formularios web. La administración avanzada queda para Admin y Manager.

---

## Categoría 2: Leads

### 13. ¿Dónde gestiono mis prospectos?
En la ruta `/Leads`, accesible desde el menú lateral en la sección Commerce. Ahí verá listado, filtros, métricas y acceso a crear, editar y ver detalle.

### 14. ¿Cuáles son los estados posibles de un Lead?
New (nuevo), Contacted (contactado), Qualified (calificado), Converted (convertido a cliente), Lost (perdido) y Unqualified (no calificado).

### 15. ¿Con qué estado nace un Lead al crearlo?
Siempre nace en estado **New**, tanto desde la UI `/Leads/Create` como desde la API `POST /api/leads` o importación CSV/JSON.

### 16. ¿Qué fuentes de origen puede tener un Lead?
Website, Referral, SocialMedia, EmailCampaign, ColdCall, Partner, Event, Other y Unknown si no se especifica.

### 17. ¿Qué es el score de un Lead?
Es una puntuación de 0 a 100 que refleja prioridad o calidad del prospecto. El worker `LeadIntelligenceAgent` puede actualizarlo tras `LeadCreatedEvent`, generando `LeadScoreUpdatedEvent`.

### 18. ¿Cómo califico un Lead desde la interfaz?
Abra `/Leads/Details/{id}` y use la acción **Qualify**. Solo roles Admin, Manager y Sales pueden ejecutarla; Support y Viewer no tienen permiso de escritura en la UI comercial.

### 19. ¿Qué ocurre automáticamente al calificar un Lead?
El sistema marca el Lead como Qualified y, mediante `OperationalAutomationService`, crea un Customer si no existe (por email), un deal borrador (`Amount=1`, `IsDraft=true`) y una tarea de seguimiento de alta prioridad en 24 horas.

### 20. ¿Calificar un Lead lo convierte automáticamente en Converted?
No. Qualify deja el Lead en **Qualified**. El estado Converted solo se alcanza con la acción manual **Convert to Customer** en la ficha del lead.

### 21. ¿Cómo convierto un Lead en cliente?
En `/Leads/Details` use **Convert to Customer**. El sistema crea un Customer en estado Prospect, marca el Lead como Converted y dispara `CustomerCreatedEvent`.

### 22. ¿Puedo crear un Deal desde un Lead sin convertirlo?
Sí. En `/Leads/Details` existe la acción **Create Deal**, que busca o crea Customer por email y genera un Deal en Prospecting sin cambiar el estado del Lead.

### 23. ¿Cuál es la diferencia entre Qualify, Convert y Create Deal?
Qualify → Lead Qualified + Customer auto + deal borrador + tarea. Convert → Lead Converted + Customer creado. Create Deal → Deal creado sin cambiar estado del Lead. Elija un proceso estándar para su equipo.

### 24. ¿Puedo asignar un Lead a un vendedor?
Sí. El Lead tiene campo `AssignedToUserId`. Puede asignarse manualmente o mediante workflows con acción `Assign` cuando se dispara un evento de dominio.

### 25. ¿Qué pasa si marco un Lead como Lost o Unqualified?
El estado cambia mediante `ChangeStatus` y se registra `LeadStatusChangedEvent`. Use Lost para oportunidades descartadas tras contacto y Unqualified para prospectos que no cumplen criterio mínimo.

### 26. ¿Existen operaciones masivas sobre Leads?
Sí. Existe `BulkUpdateLeadStatus` para actualizar estados en lote desde las capacidades de la aplicación (UI bulk según pantalla disponible).

---

## Categoría 3: Clientes

### 27. ¿Dónde veo el directorio de clientes?
En `/Customers`, sección Customers del menú. Muestra listado paginado, métricas agregadas (LTV, riesgo) y acceso a crear, editar y detalle.

### 28. ¿Cuáles son los estados de un Customer?
Prospect, Lead, Qualified, Customer, VIP, Churned e Inactive.

### 29. ¿Con qué estado se crea un Customer manualmente?
Al usar `Customer.Create` o el formulario de creación, el estado inicial es **Prospect**.

### 30. ¿Cuándo pasa un Customer a estado Customer?
Ocurre al dispararse `CustomerCreatedEvent` (retención automática) o tras `DealClosedEvent` cuando el deal se cierra ganado, según `RetentionAutomationEngine`.

### 31. ¿Qué es el estado VIP?
VIP es un segmento de clientes de alto valor asignado por `CustomerSegmentationEngine`, no un estado que el vendedor cambie manualmente en todos los flujos.

### 32. ¿Qué significa Churned e Inactive?
Churned indica cliente que abandonó o dejó de comprar; se usa en analítica y KPIs. Inactive puede asignarse en procesos como fusión de duplicados (`IdentityMergeService`).

### 33. ¿Qué es Customer 360?
Es la vista integral en `/Customer360` y `/customers/{id}/360` que consolida perfil, deals, salud, riesgo de churn ML, comunicaciones y grafo de relaciones.

### 34. ¿Qué es LTV en AutonomusCRM?
Lifetime Value: valor acumulado del cliente. Se actualiza, por ejemplo, al cerrar un deal ganado sumando el monto del deal al LTV existente.

### 35. ¿Puedo editar email y teléfono de un cliente?
Sí, desde `/Customers/Edit` si su rol permite escritura comercial (Admin, Manager, Sales). Support y Viewer solo lectura en la UI.

### 36. ¿Se crea Customer automáticamente al calificar un Lead?
Sí, si no existe un Customer con el mismo email (comparación sin distinguir mayúsculas). Si ya existe, se reutiliza ese registro.

### 37. ¿Qué metadatos guarda el Customer en onboarding?
Tras `CustomerCreatedEvent`, retención puede escribir `JourneyStage=Customer` y `OnboardingStarted` con fecha UTC, además de ejecutar playbook de onboarding.

### 38. ¿Puedo eliminar un Customer?
Existe comando `DeleteCustomer` en la capa de aplicación. La eliminación desde UI depende de la pantalla; verifique permisos y políticas antes de borrar cuentas con historial.

---

## Categoría 4: Deals

### 39. ¿Dónde gestiono mis oportunidades de venta?
En `/Deals`, menú Revenue → Pipeline. Incluye vista kanban por etapa y tabla con forecast y métricas.

### 40. ¿Cuáles son las etapas (stages) de un Deal?
Prospecting, Qualification, Proposal, Negotiation, ClosedWon y ClosedLost.

### 41. ¿Cuáles son los estados (status) de un Deal?
Open, Closed, OnHold y Cancelled, independientes de la etapa visual del pipeline.

### 42. ¿Con qué etapa y estado nace un Deal nuevo?
Nace en etapa **Prospecting**, estado **Open** y probabilidad por defecto del 10%.

### 43. ¿Cuál es la probabilidad automática por etapa?
Prospecting 10%, Qualification 25%, Proposal 50%, Negotiation 75%, ClosedWon 100%, ClosedLost 0%. Puede ajustarse manualmente entre 0 y 100.

### 44. ¿Un Deal puede existir sin Customer?
No. Todo Deal requiere `CustomerId` obligatorio en el dominio; si crea desde Lead, el sistema busca o crea el Customer primero.

### 45. ¿Qué es un deal borrador (draft)?
Al calificar un Lead, el sistema crea un deal con monto simbólico de 1, descripción de borrador automático y metadata `IsDraft=true` vinculada al LeadId.

### 46. ¿Cómo cierro un deal ganado?
Use `CloseDealCommand` desde la UI de detalle o API. Genera `DealClosedEvent`, etapa ClosedWon y dispara automatizaciones de retención y tareas de onboarding CS (D0, D7, D30).

### 47. ¿Cómo registro un deal perdido?
Use `LoseDealCommand`, que produce `DealLostEvent` y etapa ClosedLost con probabilidad 0%.

### 48. ¿Qué es el forecast 30/60/90 en Deals?
Es la suma ponderada (Amount × Probability) de deals abiertos cuya fecha de cierre esperada cae en ventanas de 30, 60 o 90 días, calculada en `DealRepository.GetListSummaryAsync`.

### 49. ¿Puedo importar deals masivamente?
Sí. Existe página `/Deals/Import` para carga masiva según el flujo de importación implementado en la API/UI.

### 50. ¿Puedo cambiar etapa en lote?
Sí. Existe `BulkUpdateDealStage` para actualizar etapas de múltiples deals en una operación.

### 51. ¿Qué ocurre tras ClosedWon para el cliente?
Retención actualiza Customer a estado Customer, incrementa LTV, registra compra, puede crear contrato anual, enviar email de onboarding y persistir salud de cuenta.

### 52. ¿Puedo asignar un Deal a un vendedor?
Sí, mediante `AssignToUser` en la entidad Deal o acción `Assign` de workflows cuando el agregado es tipo Deal.

---

## Categoría 5: Tareas

### 53. ¿Dónde veo mis tareas pendientes?
En `/Tasks`, menú Operations → Tasks. Muestra tareas de workflow y operativas con filtros por estado, asignado, prioridad y vencimiento.

### 54. ¿Qué estados tiene una tarea?
Usa strings **Open** y **Completed**; no hay enum de estados de tarea en el dominio.

### 55. ¿Quién crea las tareas automáticamente?
WorkflowEngine (acción CreateTask), OperationalAutomation (qualify, deal cerrado), RevenueAutomation, RetentionAutomation, CommercialSlaEngine y escaneos periódicos del worker.

### 56. ¿Qué tarea se crea al calificar un Lead?
"Seguimiento lead calificado: {nombre}" con descripción de contactar en 24 horas, prioridad High, tipo FollowUp y vencimiento al día siguiente.

### 57. ¿Qué tareas se crean al ganar un deal?
Tres tareas de onboarding CS: Día 1 (Urgent), Día 7 (Normal) y Día 30 (Normal), asociadas al Deal.

### 58. ¿Puedo completar una tarea desde la UI?
Sí. Existen comandos `CompleteWorkflowTask` y handlers en `/Tasks` para marcar tareas como completadas.

### 59. ¿Puedo asignar una tarea a otro usuario?
Sí, mediante `AssignWorkflowTask` desde la interfaz de tareas o al crearla con `AssignedToUserId`.

### 60. ¿Qué pasa si ignoro las tareas?
Los SLA comerciales (por ejemplo 24 h tras lead nuevo) y automatizaciones de revenue/retención dependen de que alguien ejecute o complete tareas; el sistema seguirá generando alertas y escaneos cada 15 minutos.

### 61. ¿Las tareas están vinculadas a entidades?
Sí. Cada tarea referencia tipo de entidad (Lead, Deal, Customer, etc.) y `entityId` para contexto y deduplicación (`ExistsOpenTaskAsync`).

### 62. ¿Puedo crear tareas manualmente?
Sí. Desde `/FlowActions` y otras pantallas operativas existen handlers `OnPostCreateTaskAsync` que invocan `IOperationalTaskService.CreateTaskAsync`.

---

## Categoría 6: IA

### 63. ¿AutonomusCRM usa inteligencia artificial?
Sí. Combina Command Center (decisiones y priorización), Trust Studio (gobernanza HITL), modelos ML de churn y expansión, y módulo LLM en API (`AutonomusCRM.AI`) para ciertos servicios; los workers de fondo no invocan LLM.

### 64. ¿Qué muestra Command Center en `/`?
Métricas de revenue generado/protegido, cuentas en riesgo, expansiones, renovaciones, decisiones en vivo y snapshot del workforce, con periodo de 7 o 30 días vía `IAiCommandCenterService`.

### 65. ¿Qué es Trust Inbox?
Es `/TrustInbox`, el buzón donde se revisan aprobaciones pendientes de decisiones autónomas. El layout muestra contador `PendingApprovals` en la barra cuando hay ítems por revisar.

### 66. ¿Qué es predicción de churn?
Es un modelo ML (`IChurnPredictionV2`) que estima probabilidad de abandono por cliente. Customer 360 muestra riesgo Alto/Medio/Bajo según umbrales (≥60%, ≥35%).

### 67. ¿Qué es inteligencia de expansión?
`IExpansionIntelligence` analiza oportunidades de upsell/cross-sell. El motor autónomo de revenue usa `ReadinessLevel` y `OpportunityType` para proponer acciones.

### 68. ¿Los workers en background usan LLM?
No. `AutonomusCRM.Workers` ejecuta agentes basados en reglas, ML enterprise y orquestación; no hay referencias a LLM en el proyecto Workers según el análisis del código.

### 69. ¿Dónde sí puede usarse LLM?
En el módulo `AutonomusCRM.AI` (proveedores LLM, embeddings, agent service) integrado en API. Si no hay proveedor configurado, puede lanzarse `LlmNotConfiguredException`.

### 70. ¿Qué son los playbooks autónomos?
Secuencias predefinidas (onboarding, rescate, re-engagement) ejecutadas por `CustomerPlaybookService` y visibles en rutas como `/command/playbooks`.

### 71. ¿Qué es Workforce en `/Agents`?
Muestra agentes autónomos registrados (LeadIntelligence, CustomerRisk, DealStrategy, Communication, ChurnRisk, etc.) y decisiones recientes del ecosistema autónomo.

### 72. ¿Debo aprobar decisiones de IA manualmente?
Depende de la política del tenant. Trust Studio permite aprobación humana (HITL) para decisiones que requieren validación antes de ejecutarse.

### 73. ¿La IA crea deals o leads sola?
Los deals y leads se crean por usuarios, API o automatizaciones de dominio (qualify, workflows). La IA prioriza, puntúa, predice riesgo y sugiere acciones; no sustituye la carga comercial inicial sin trigger.

### 74. ¿Qué es Memory (`/Memory`)?
Memoria empresarial semántica que indexa contexto de clientes, retención y decisiones para enriquecer razonamiento y búsquedas inteligentes.

### 75. ¿Puedo desactivar la IA?
En Settings existe configuración de tenant; el layout lee `AI:Enabled` (por defecto true). Un kill-switch puede limitar funciones autónomas según configuración.

### 76. ¿Qué es Outcome Fabric?
Ruta `/command/outcomes` que documenta atribución de resultados (OutcomeAttributionService) para aprender qué decisiones y playbooks impactaron ingresos o retención.

---

## Categoría 7: Roles

### 77. ¿Qué roles existen en AutonomusCRM?
Admin, Manager, Sales, Support y Viewer, definidos en seed, gestión de usuarios y middleware de autorización.

### 78. ¿Cuál es el usuario demo de ventas?
Email **sales@autonomuscrm.local**, contraseña **Sales123!**, nombre demo Ana Ventas. Tras login, su home es `/revenue`.

### 79. ¿Qué puede hacer un usuario Sales?
Crear y editar Leads, Customers y Deals en UI; calificar y convertir leads; usar Revenue OS, Command, Tasks y VoiceCalls; no administrar usuarios ni Settings.

### 80. ¿Qué puede hacer un Manager?
Todo lo de Sales en escritura comercial, más gestión de usuarios (`/Users`), Settings, Policies, Trust Studio y home en `/executive`.

### 81. ¿Qué puede hacer un Admin?
Todo lo de Manager más `POST /api/users`, `POST /api/tenants` y control completo de auditoría y billing.

### 82. ¿Qué puede hacer Support?
Lectura general autenticada, Customer 360, Customer Success (`/customer-success`); la UI comercial bloquea sus POST en Leads/Customers/Deals. Home: `/Customer360`. Ruta `/Support` redirige a customer-success.

### 83. ¿Qué puede hacer Viewer?
Solo lectura en módulos comerciales de la UI. Home por defecto `/` (Command). No puede calificar leads ni crear deals desde formularios Razor.

### 84. ¿Support o Viewer pueden escribir vía API comercial?
La API comercial (`POST` leads/customers/deals) solo exige autenticación, sin filtro `[Authorize(Roles=...)]` en controllers. Es una brecha documentada: UI los bloquea, API no.

### 85. ¿Quién puede gestionar roles de usuario?
Admin y Manager en `/Users/Roles` y edición de usuario. Roles disponibles: Admin, Manager, Sales, Support, Viewer.

### 86. ¿Existen políticas RequireSales o RequireManager en endpoints?
Están registradas en `AuthorizationPolicies` pero no aplicadas en endpoints comerciales según inventario del sistema; solo RequireAdmin se usa en áreas admin.

### 87. ¿Puedo tener varios roles a la vez?
El modelo de usuario admite colección de roles (`User.Roles`). La redirección de home usa prioridad: Admin > Manager > Sales > Support > default.

### 88. ¿Qué contraseña usan los demás usuarios demo?
admin@autonomuscrm.local → Admin123!, manager@autonomuscrm.local → Manager123!, support@autonomuscrm.local → Support123!, viewer@autonomuscrm.local → Viewer123!.

---

## Categoría 8: Navegación

### 89. ¿A dónde me lleva el sistema tras iniciar sesión como Sales?
A `/revenue` (Revenue OS), según `RoleHomeRedirect.cs`.

### 90. ¿Cuántos ítems tiene el menú lateral?
19 ítems agrupados en Command, Revenue, Customers, Commerce, Intelligence, Operations y Admin.

### 91. ¿Cómo busco una pantalla rápidamente?
Use Ctrl+K para búsqueda global, que consulta `/api/flow/search` con rutas como Leads, Deals, Trust Studio y Revenue OS.

### 92. ¿Dónde está el pipeline visual?
En `/Deals`, con columnas kanban por etapa (Prospecting hasta ClosedLost) y tabla datatable debajo.

### 93. ¿Dónde configuro automatizaciones?
En `/Workflows`, ruta admin/operativa no siempre visible en sidebar principal pero documentada en el mapa de menús.

### 94. ¿Dónde veo auditoría de cambios?
En `/Audit`, menú Admin, con event sourcing y trazabilidad de eventos de dominio.

### 95. ¿Dónde gestiono integraciones?
En `/Integrations` (HubSpot, Salesforce, email, Stripe según hub implementado).

### 96. ¿Existe página de facturación?
Sí, `/billing` para suscripción y facturación del tenant, típicamente Admin.

### 97. ¿Qué rutas son públicas sin login?
Marketing: `/landing`, `/roi`, `/demo`, `/stories`, `/pricing`.

### 98. ¿Dónde registro llamadas de voz?
En `/VoiceCalls`, menú Platform → Voice, orientado a seguimiento comercial del equipo Sales.

### 99. ¿Confundí `/` con `/revenue`; cuál uso cada mañana?
Como Sales, empiece el día en `/revenue` para priorizar ingresos y fugas; use `/` (Command) cuando necesite panorama de decisiones IA y workforce.

### 100. ¿Dónde veo eventos fallidos del sistema?
En `/FailedEvents`, cola DLQ de eventos que no se procesaron correctamente en el bus de mensajes.

---

## Categoría 9: Automatización

### 101. ¿Qué dispara un workflow configurable?
Un trigger de evento de dominio (por ejemplo `Lead.Created`) evaluado por `WorkflowEngine` si el workflow está activo para el tenant.

### 102. ¿Qué acciones puede ejecutar un workflow?
Assign, UpdateStatus, CreateTask, Communicate y ActivateAgent según modelo `WorkflowAction`.

### 103. ¿La acción Communicate envía emails?
No en la implementación actual. `WorkflowEngine` solo registra log para Communicate y ActivateAgent; no envía mensajes ni activa agentes LLM.

### 104. ¿Qué hace la retención al crearse un Customer?
`RetentionAutomationEngine` en `CustomerCreatedEvent` cambia estado a Customer, escribe metadatos de journey/onboarding y ejecuta playbook de onboarding.

### 105. ¿Con qué frecuencia escanea el worker en background?
Cada **15 minutos** (`Task.Delay(TimeSpan.FromMinutes(15))` en `Worker.cs`), recorriendo todos los tenants.

### 106. ¿Qué ejecuta el scan periódico de 15 minutos?
Revenue scan, calidad de datos revenue, retención periódica, renovaciones, expansión, inteligencia, CustomerInsights, ciclo autónomo y optimización de workflows.

### 107. ¿Qué agentes escuchan eventos en tiempo real?
LeadIntelligenceAgent, RevenueAutomation, CustomerRiskAgent, DealStrategyAgent, CommunicationAgent, OutcomeAttribution y ComplianceSecurityAgent vía RabbitMQ.

### 108. ¿Qué SLA comercial existe para leads nuevos?
`CommercialSlaEngine` puede crear tarea de seguimiento 24 h tras eventos de lead, alineado con automatización de revenue.

### 109. ¿Puedo duplicar un workflow?
Sí. Existe comando `Duplicate` en workflows para reutilizar triggers y acciones en otro flujo.

### 110. ¿Qué es BusinessMemoryConsolidationWorker?
Worker separado que consolida memoria empresarial cada **6 horas**, distinto del heartbeat de 15 minutos del worker principal.

### 111. ¿La automatización de qualify crea siempre un nuevo deal?
Solo si no existe ya un deal con metadata `LeadId` igual al lead calificado; evita duplicar borradores.

### 112. ¿Qué optimiza AutomationOptimizerAgent?
Analiza rendimiento y optimiza workflows en cada ciclo de 15 minutos, registrando advertencias si falla sin detener el worker.

---

## Categoría 10: Errores

### 113. ¿Por qué recibo "Access Denied" al crear un Lead?
Su rol es Support o Viewer. El middleware `CommercialWriteAuthorizationMiddleware` bloquea POST en rutas comerciales de la UI para esos roles.

### 114. ¿Por qué Sales no puede entrar a `/Users`?
Gestión de usuarios requiere rol Admin o Manager (`[Authorize(Roles = "Admin,Manager")]` en páginas Users).

### 115. ¿Califiqué un Lead pero no veo deal borrador?
Verifique que no existiera ya un deal con el mismo LeadId en metadata, que el evento `LeadQualifiedEvent` se despachó y que filtra por tenant correcto en `/Deals`.

### 116. ¿El deal borrador muestra monto $1; es un error?
No. Es intencional: monto simbólico con `IsDraft=true` hasta que el vendedor actualice el importe real en `/Deals/Edit`.

### 117. ¿Por qué Support ve datos pero no puede guardar cambios?
Diseño actual: Support tiene lectura en UI comercial y foco en Customer Success; la escritura comercial UI está restringida a Admin, Manager y Sales.

### 118. ¿Qué hago si una automatización no corrió?
Revise `/FailedEvents`, logs del worker, que RabbitMQ y workers estén activos en despliegue, y que el workflow esté **activo** para su tenant.

### 119. ¿Por qué no aparece churn en Customer 360?
El servicio ML puede no devolver predicción para clientes sin historial suficiente; la vista muestra riesgo bajo o sin bullet de churn elevado.

### 120. ¿Puedo usar la API para evitar el bloqueo UI de Viewer?
Técnicamente la API comercial autenticada no filtra por rol; eso es un riesgo de seguridad operativa, no una función recomendada para Viewer.

---

## Categoría 11: Métricas

### 121. ¿Qué métricas veo en la lista de Leads?
TotalCount, QualifiedCount, NewCount, HighScoreCount (score > 70), AvgScore y SourceStats por fuente.

### 122. ¿Qué es Win Rate en Deals?
Proporción ClosedWon / (ClosedWon + ClosedLost) calculada en agregados del repositorio de deals.

### 123. ¿Qué es Revenue Closed?
Suma de `Amount` de todos los deals en etapa ClosedWon.

### 124. ¿Qué es Pipeline Open?
Suma de montos de deals con estado Open, visible en vista kanban y resumen de lista.

### 125. ¿Qué métricas tiene el directorio de Customers?
TotalCount, AvgLtv, HighLtvCount (LTV > 10.000), HighRiskCount (RiskScore > 70), AvgRisk y LowRiskCount.

### 126. ¿Qué muestra Revenue OS sobre fugas?
`IGraphReasoningEngine.DetectRevenueLeakAsync` explica dónde se pierde ingreso (deals estancados, leads inactivos, etc.).

### 127. ¿Qué KPIs hay en Customer Success?
`CustomerKpiService` calcula entre otros retention rate a partir de clientes retenidos y churned en el periodo analizado.

### 128. ¿Cómo filtro métricas de Command por periodo?
Query param de 7 o 30 días en la pantalla Command al cargar `GetFlowCommandAsync`.

### 129. ¿Qué cuentan las tareas vencidas en `/Tasks`?
`CountOverdueOpenAsync` del repositorio de tareas: tareas Open con fecha de vencimiento pasada.

### 130. ¿Dónde exporto reporte ejecutivo?
Executive OS en `/executive` permite export HTML con `?handler=Export` para tablero ejecutivo.

---

## Categoría 12: CS (Customer Success)

### 131. ¿Dónde trabaja el equipo de Customer Success?
En `/customer-success` (Customer Success OS) con tickets, casos y playbooks; `/Support` redirige aquí.

### 132. ¿Qué playbooks existen en retención?
Onboarding al crear cliente, Rescue cuando RiskScore ≥ 70, ReEngagement para cuentas sin contacto > 45 días, entre otros definidos en constantes CS.

### 133. ¿Qué hace el scan de retención cada 15 minutos?
Persiste salud de todos los clientes, ejecuta rescue en críticos, envía emails de riesgo, ventanas de renovación, alertas churn y tareas de expansión.

### 134. ¿Se envían emails automáticos de onboarding?
Sí, tras deal ganado `RetentionAutomationEngine` puede enviar plantilla "Onboarding" si el Customer tiene email configurado.

### 135. ¿Se usa WhatsApp en automatizaciones?
Sí. `IWhatsAppAutomationEngine` envía plantillas de re-engagement cuando el cliente tiene teléfono en scan de retención.

### 136. ¿Qué es salud de cuenta (health)?
`ICustomerHealthEngine` calcula puntuación y clasificación (incluida Critical) usada por decisiones autónomas y playbooks de rescate.

### 137. ¿Qué ocurre si un cliente está en riesgo crítico?
Se ejecuta playbook Rescue, puede enviarse email templado de riesgo y generarse tareas para el equipo CS o comercial según configuración.

### 138. ¿Qué es renovación automática?
`RenewalEngine.EnforceRenewalWindowsAsync` en scan periódico gestiona ventanas de renovación y tareas asociadas.

### 139. ¿Expansion genera tareas comerciales?
Sí. `ExpansionRevenueEngine.CreateExpansionTasksAsync` crea tareas de expansión/upsell en el scan de retención.

### 140. ¿Support debe usar Leads o Customer Success?
Para post-venta, tickets y retención use Customer Success y Customer 360; Leads y Deals son responsabilidad primaria de Sales.

---

## Categoría 13: Integraciones

### 141. ¿Qué integraciones admite la plataforma?
El hub en `/Integrations` contempla conectores hacia HubSpot, Salesforce, email y Stripe según implementación del `IntegrationsController`.

### 142. ¿Las integraciones requieren rol Admin?
Típicamente Admin y Manager acceden a configuración de plataforma; Sales usa los datos sincronizados pero no configura OAuth del tenant.

### 143. ¿Los leads importados disparan automatizaciones?
Sí. Un lead creado vía import genera `LeadCreatedEvent` igual que creación manual, activando workflows, SLA, LeadIntelligenceAgent y CommunicationAgent.

### 144. ¿Existe API REST para integraciones externas?
Sí. Controllers autenticados para Leads, Customers, Deals, Revenue, Trust, AI e Integrations exponen superficie REST documentada en Swagger de la API.

### 145. ¿Cómo valida la API el tenant en requests?
`ApiTenantValidationMiddleware` valida que el TenantId del JWT coincida con el tenant solicitado en la petición.

---

## Categoría 14: Seguridad

### 146. ¿Cómo inicio sesión?
En `/Account/Login` con autenticación por cookie y soporte JWT para API; credenciales demo documentadas en matriz de roles.

### 147. ¿Qué es MFA en AutonomusCRM?
Autenticación multifactor configurable en Settings del tenant para endurecer acceso de usuarios administrativos y operativos.

### 148. ¿Dónde reviso quién cambió qué?
En `/Audit` con event store y registro de eventos de dominio (`DomainEvent`) para trazabilidad.

### 149. ¿Qué es el middleware de límites de plan?
`PlanLimitMiddleware` aplica límites del plan SaaS (cantidad de usuarios, registros, etc.) según suscripción del tenant.

### 150. ¿Qué práctica de seguridad debo conocer como Sales?
Use solo su cuenta sales@autonomuscrm.local, no comparta contraseñas demo en producción, y reporte a Admin si detecta que usuarios Viewer/Support podrían escribir vía API sin control de rol en endpoints comerciales.

---

*Fin del FAQ — 150 ítems. Carpeta: `docs/manual-empresarial-autonomuscrm/`. Para flujos ver `02_BUSINESS_FLOWS.md`; para permisos ver `03_ROLE_MATRIX.md`.*
