# Manual de Usuario Empresarial AutonomusCRM

**Versión del documento:** 1.0.0  
**Fecha de publicación:** 5 de junio de 2026  
**Audiencia principal:** Ejecutivo de Ventas (`sales@autonomuscrm.local`)  
**Nivel de experiencia requerido:** Ninguno en CRM  
**Idioma:** Español (contenido operativo); la interfaz admite ES/EN  
**Base de evidencia:** Código fuente en repositorio `autonomuscrm` (commit `f8131e8` y posteriores)  
**Documentación complementaria:** carpeta `docs/manual-empresarial-autonomuscrm/` (`01_SYSTEM_INVENTORY.md` … `15_SYSTEM_CAPABILITIES_MATRIX.md`, `08_FAQ.md`)

### Volumen del paquete documental

| Documento | Palabras aprox. | Páginas impresas (~250 pal/pág) |
|-----------|-----------------|----------------------------------|
| Este manual maestro (18 capítulos + FAQ integrado) | 14.500+ | 58+ |
| 15 documentos satélite (01–15) | 5.500+ | 22+ |
| **Total paquete enterprise** | **20.000+** | **80+** |

> El Capítulo 16 incluye las **150 preguntas y respuestas completas** (texto íntegro de `08_FAQ.md`). Los documentos 10–15 amplían capacitación, operaciones diarias y matriz de capacidades. Para impresión tipo manual corporativo de 150 páginas, imprima este manual maestro junto con los 15 anexos y los módulos 11–14 (onboarding, training, daily ops, manager).

### Mapa de correspondencia (prompt Harvard → capítulos de este manual)

| Capítulo solicitado | Ubicación en este manual |
|---------------------|--------------------------|
| 1. ¿Qué es AutonomusCRM? | Cap. 2 + Cap. 1 |
| 2. Conceptos fundamentales | Cap. 6 (ampliado) |
| 3. Arquitectura funcional del negocio | Cap. 6.6 + Cap. 15 |
| 4. Roles del sistema | Cap. 5 |
| 5. Navegación del sistema | Cap. 4 |
| 6. Operación diaria del vendedor | Cap. 10 |
| 7. Gestión de Leads | Cap. 7 |
| 8. Gestión de Clientes | Cap. 8 |
| 9. Pipeline comercial | Cap. 9 |
| 10. Automatizaciones | Cap. 14 |
| 11. Inteligencia artificial | Cap. 12 |
| 12. Reportes y analítica | Cap. 17 |
| 13. Customer Success | Cap. 13 |
| 14. Seguridad operativa | Cap. 5.5 + Apéndice D |
| 15. Escenarios reales | Cap. 15 |
| 16. FAQ empresarial (150) | Cap. 16 (completo) |
| 17. Troubleshooting | Cap. 17.4 + `09_TROUBLESHOOTING.md` |
| 18. Best practices | Cap. 18 |

---

## Tabla de contenidos

1. [Introducción y propósito del manual](#capítulo-1--introducción-y-propósito-del-manual)
2. [Visión general de AutonomusCRM](#capítulo-2--visión-general-de-autonomuscrm)
3. [Acceso, autenticación y primer inicio de sesión](#capítulo-3--acceso-autenticación-y-primer-inicio-de-sesión)
4. [Navegación y arquitectura de la interfaz](#capítulo-4--navegación-y-arquitectura-de-la-interfaz)
5. [Roles, permisos y gobernanza operativa](#capítulo-5--roles-permisos-y-gobernanza-operativa)
6. [Conceptos fundamentales del CRM](#capítulo-6--conceptos-fundamentales-del-crm)
7. [Gestión de Leads (prospectos comerciales)](#capítulo-7--gestión-de-leads-prospectos-comerciales)
8. [Gestión de Clientes (Customers)](#capítulo-8--gestión-de-clientes-customers)
9. [Pipeline y Oportunidades (Deals)](#capítulo-9--pipeline-y-oportunidades-deals)
10. [Tareas y rutina diaria del ejecutivo de ventas](#capítulo-10--tareas-y-rutina-diaria-del-ejecutivo-de-ventas)
11. [Revenue OS y Command Center](#capítulo-11--revenue-os-y-command-center)
12. [Inteligencia artificial, Trust Studio y Workforce](#capítulo-12--inteligencia-artificial-trust-studio-y-workforce)
13. [Customer Success y Customer 360](#capítulo-13--customer-success-y-customer-360)
14. [Automatizaciones, workers y ciclo de retención](#capítulo-14--automatizaciones-workers-y-ciclo-de-retención)
15. [Simulación de escenario comercial completo](#capítulo-15--simulación-de-escenario-comercial-completo)
16. [Preguntas frecuentes (FAQ)](#capítulo-16--preguntas-frecuentes-faq)
17. [Reportes, métricas y resolución de incidencias](#capítulo-17--reportes-métricas-y-resolución-de-incidencias)
18. [Limitaciones conocidas, trazabilidad y referencias](#capítulo-18--limitaciones-conocidas-trazabilidad-y-referencias)

---

## Capítulo 1 — Introducción y propósito del manual

### 1.1 Para quién está escrito este documento

Este manual constituye la referencia operativa principal de AutonomusCRM para profesionales que ejecutan actividades comerciales sin experiencia previa en sistemas CRM. El perfil de lectura prioritario es el **Ejecutivo de Ventas** identificado en el entorno de demostración como `sales@autonomuscrm.local`, aunque los capítulos sobre roles, gobernanza y limitaciones técnicas resultan igualmente pertinentes para Managers, personal de Customer Success y administradores del tenant.

El tono y la estructura siguen estándares de documentación empresarial de alto nivel — claridad ejecutiva, procedimientos reproducibles y trazabilidad hacia el producto real — en la línea de manuales de referencia de plataformas como Salesforce, adaptados al alcance verificado del código de AutonomusCRM.

### 1.2 Qué aprenderá al completar la lectura

Al finalizar este manual, el lector será capaz de:

- Iniciar sesión, orientarse en la interfaz y utilizar las **19 entradas del menú lateral** sin depender de soporte técnico.
- Comprender la diferencia operativa entre **Lead**, **Customer** y **Deal**, y los estados que el sistema reconoce en cada entidad.
- Ejecutar el ciclo comercial diario: captación, calificación, gestión de pipeline, cierre y seguimiento post-venta mediante tareas.
- Interpretar **Revenue OS**, **Command Center** y las métricas agregadas de Leads, Deals y Customers.
- Colaborar con automatizaciones reales (calificación, retención, workers cada 15 minutos) sin sobreestimar capacidades no implementadas.
- Identificar limitaciones documentadas (API sin filtro de rol, acciones Communicate solo log, etc.) y escalar correctamente.

### 1.3 Principio de veracidad documental

**Ninguna funcionalidad descrita en este manual ha sido inventada.** Cada afirmación sobre pantallas, estados, automatizaciones, roles o métricas deriva del análisis estático del repositorio y de los documentos de inventario en `docs/manual-empresarial-autonomuscrm/`. Cuando una capacidad está parcialmente implementada o posee restricciones conocidas, el manual lo indica explícitamente en el Capítulo 18 y en las secciones pertinentes.

### 1.4 Cómo usar este manual junto con la documentación complementaria

| Documento | Uso recomendado |
|-----------|-----------------|
| `01_SYSTEM_INVENTORY.md` | Arquitectura técnica y entidades |
| `02_BUSINESS_FLOWS.md` | Flujos Lead → Customer → Deal |
| `03_ROLE_MATRIX.md` | Matriz detallada de permisos |
| `04_MENU_MAP.md` | Mapa de rutas y menú lateral |
| `05_AUTOMATION_CATALOG.md` | Motores y agentes automáticos |
| `06_AI_CATALOG.md` | IA, ML, Trust y Command |
| `07_REPORT_CATALOG.md` | Métricas por pantalla |
| `08_FAQ.md` | 150 preguntas y respuestas |
| `09_TROUBLESHOOTING.md` | Tabla de incidencias |
| `10_OPERATIONAL_PLAYBOOK.md` | Playbooks operativos |
| `11_NEW_EMPLOYEE_ONBOARDING.md` | Plan de 5 días |
| `12_SALES_EXECUTIVE_TRAINING.md` | Módulos de capacitación |
| `13_DAILY_OPERATIONS_GUIDE.md` | Rutina diaria por rol |
| `14_MANAGER_GUIDE.md` | Guía del Manager |
| `15_SYSTEM_CAPABILITIES_MATRIX.md` | Matriz implementado vs. parcial |

---

## Capítulo 2 — Visión general de AutonomusCRM

### 2.1 Definición del producto

AutonomusCRM es una plataforma SaaS de **operaciones de ingresos y relación con clientes**, construida sobre **.NET 9**, que centraliza la gestión comercial, la analítica de ingresos, las automatizaciones de dominio y los módulos de inteligencia artificial en un entorno web autenticado y multi-tenant.

A diferencia de una hoja de cálculo o de un correo compartido, AutonomusCRM ofrece:

- **Registro estructurado** de prospectos, cuentas y oportunidades.
- **Pipeline visual** con etapas, probabilidades y forecast ponderado.
- **Tareas operativas** generadas por el sistema y por workflows configurables.
- **Capa de ingresos** (Revenue OS) con detección de fugas de pipeline.
- **Capa autónoma** (Command, Trust Studio, Workforce) para decisiones asistidas por IA con gobernanza humana.
- **Retención y Customer Success** integrados en el ciclo post-venta.

### 2.2 Componentes de la solución

La solución se compone de seis proyectos principales:

| Proyecto | Función |
|----------|---------|
| `AutonomusCRM.Domain` | Entidades, eventos de dominio, reglas de negocio |
| `AutonomusCRM.Application` | Comandos, consultas, interfaces |
| `AutonomusCRM.Infrastructure` | Persistencia, automatizaciones, IA, integraciones |
| `AutonomusCRM.API` | 66 páginas Razor routables, controllers REST, autenticación |
| `AutonomusCRM.Workers` | Agentes en background, escaneos periódicos |
| `AutonomusCRM.AI` | Proveedores LLM, embeddings, servicios de agente |

**Infraestructura de soporte:** PostgreSQL 16, RabbitMQ (bus de eventos), Redis (caché), despliegue Docker Compose con Nginx en producción.

### 2.3 El concepto de tenant

Cada organización cliente opera dentro de un **tenant** aislado. Todos los leads, clientes, deals, usuarios y configuraciones pertenecen a un `TenantId`. Los usuarios autenticados solo acceden a datos de su tenant. Esta separación es fundamental para entender por qué no verá registros de otras empresas y por qué la validación de tenant en la API es obligatoria.

### 2.4 Interfaz de usuario: AutonomusFlow

La experiencia autenticada utiliza el shell **AutonomusFlow**: barra superior con búsqueda global (Ctrl+K), selector de idioma, modo oscuro, indicador de entorno y menú lateral con 19 ítems agrupados en siete secciones (Command, Revenue, Customers, Commerce, Intelligence, Operations, Platform, Admin).

La interfaz está internacionalizada en **español** (predeterminado) e **inglés**, con aproximadamente 1.069 claves de localización.

### 2.5 Qué distingue a AutonomusCRM de un CRM tradicional

Tres capacidades diferenciadoras — todas implementadas en código — merecen atención desde el primer día:

1. **Revenue OS** (`/revenue`): priorización de ingresos y explicación de fugas mediante razonamiento en grafo.
2. **Command Center** (`/`): panorama de decisiones autónomas, métricas de flujo y snapshot del workforce.
3. **Automatización de dominio**: al calificar un lead, el sistema crea cliente, deal borrador y tarea de seguimiento sin intervención manual adicional.

Estas capacidades **complementan** el trabajo del vendedor; no lo sustituyen. Las comunicaciones comerciales reales — llamadas, reuniones, negociación — permanecen en manos del ejecutivo.

---

## Capítulo 3 — Acceso, autenticación y primer inicio de sesión

### 3.1 URL de acceso y credenciales de demostración

| Campo | Valor (entorno demo) |
|-------|----------------------|
| Ruta de login | `/Account/Login` |
| Email Sales | `sales@autonomuscrm.local` |
| Contraseña Sales | `Sales123!` |
| Nombre demo | Ana Ventas |

Otros usuarios de referencia para pruebas cruzadas de permisos:

| Rol | Email | Contraseña |
|-----|-------|------------|
| Admin | admin@autonomuscrm.local | Admin123! |
| Manager | manager@autonomuscrm.local | Manager123! |
| Support | support@autonomuscrm.local | Support123! |
| Viewer | viewer@autonomuscrm.local | Viewer123! |

> **Nota de seguridad:** En producción real, las contraseñas de seed deben rotarse. No comparta credenciales demo fuera del entorno de capacitación.

### 3.2 Procedimiento: primer inicio de sesión (paso a paso)

**Paso 1.** Abra el navegador y acceda a la URL de su instancia (por ejemplo, `https://su-dominio/` o `https://localhost:5001/` en desarrollo).

**Paso 2.** Será redirigido a `/Account/Login` si no posee sesión activa.

**Paso 3.** Introduzca el email `sales@autonomuscrm.local` y la contraseña `Sales123!`.

**Paso 4.** Si el tenant tiene MFA habilitado en Settings, complete el segundo factor. En demo, MFA puede estar desactivado.

**Paso 5.** Tras autenticarse correctamente, el sistema le redirige automáticamente a **`/revenue`** (Revenue OS), conforme a `RoleHomeRedirect.cs` para el rol Sales.

**Paso 6.** Verifique en la barra lateral su email y el rol mostrado (`Sales`).

**Paso 7.** (Opcional) Use el selector de idioma en la barra superior para confirmar español como idioma de trabajo.

**Paso 8.** Explore brevemente las cuatro pantallas que constituirán su rutina: Revenue OS, Leads, Pipeline (Deals) y Tasks.

### 3.3 Redirección post-login por rol

| Rol | Pantalla de inicio |
|-----|-------------------|
| Admin | `/executive` |
| Manager | `/executive` |
| **Sales** | **`/revenue`** |
| Support | `/Customer360` |
| Viewer | `/` (Command) |

Como ejecutivo de ventas, **no confunda** `/` (Command Center) con su pantalla de inicio diaria. Command es complementario; Revenue OS es su punto de partida operativo.

### 3.4 Cierre de sesión y acceso denegado

- **Cerrar sesión:** menú de usuario en barra superior → enlace a Settings o ruta `/Account/Logout`.
- **Access Denied:** si intenta acceder a `/Users`, `/Settings` u otras rutas administrativas, verá pantalla de acceso denegado. Esto es comportamiento esperado para Sales. Solicite al Manager la acción requerida.

### 3.5 Autenticación API (contexto para integraciones)

La API REST admite autenticación JWT además de cookies de sesión web. El middleware `ApiTenantValidationMiddleware` valida que el `TenantId` del token coincida con el tenant solicitado. Como vendedor, normalmente operará desde la UI web; no necesita gestionar tokens salvo que su organización integre sistemas externos.

---

## Capítulo 4 — Navegación y arquitectura de la interfaz

### 4.1 Inventario de pantallas

AutonomusCRM expone **66 páginas Razor routables** en la capa API, además de 5 páginas de marketing público (`/landing`, `/roi`, `/demo`, `/stories`, `/pricing`). El menú lateral presenta **19 ítems** de navegación principal; otras rutas críticas son accesibles mediante enlaces contextuales, búsqueda global o URL directa.

### 4.2 Menú lateral completo (19 ítems)

| # | Sección | Etiqueta | Ruta | Uso principal (Sales) |
|---|---------|----------|------|----------------------|
| 1 | Command | Command | `/` | Panorama IA y decisiones |
| 2 | Command | Trust Studio | `/TrustInbox` | Consulta; aprobación típica de Manager |
| 3 | Command | Workforce | `/Agents` | Ver agentes autónomos |
| 4 | Revenue | Revenue OS | `/revenue` | **Inicio diario — prioridades** |
| 5 | Revenue | Executive | `/executive` | Vista ejecutiva (lectura) |
| 6 | Revenue | Pipeline | `/Deals` | **Gestión de oportunidades** |
| 7 | Customers | Directory | `/Customers` | Directorio de clientes |
| 8 | Customers | Customer 360 | `/Customer360` | Vista integral de cuenta |
| 9 | Customers | Customer Success | `/customer-success` | Post-venta (colaboración CS) |
| 10 | Commerce | Leads | `/Leads` | **Gestión de prospectos** |
| 11 | Intelligence | Memory | `/Memory` | Memoria empresarial (consulta) |
| 12 | Operations | Tasks | `/Tasks` | **Tareas del día** |
| 13 | Platform | Integrations | `/Integrations` | Solo Admin/Manager configuran |
| 14 | Platform | Voice | `/VoiceCalls` | Registro de llamadas |
| 15 | Admin | Users | `/Users` | No accesible para Sales |
| 16 | Admin | Policies | `/Policies` | No accesible para Sales |
| 17 | Admin | Audit | `/Audit` | No accesible para Sales |
| 18 | Admin | Settings | `/Settings` | No accesible para Sales |
| 19 | Admin | Billing | `/billing` | No accesible para Sales |

### 4.2.1 Guía detallada de cada ítem del menú (para Sales)

A continuación, cada entrada del sidebar explicada como lo haría la documentación de Salesforce o HubSpot: para qué sirve, cuándo usarla, errores comunes e impacto en ingresos.

**1. Command (`/`)** — Centro de mando operativo. Muestra decisiones de IA en las últimas 24 h, métricas de flujo a 7 y 30 días, cuentas en riesgo y snapshot del workforce. **Cuándo usarlo:** después de Revenue OS, cuando necesite contexto de decisiones autónomas. **Error común:** confundirlo con home diario (Sales va a `/revenue`). **Impacto:** visibilidad de ingresos protegidos y generados.

**2. Trust Studio (`/TrustInbox`)** — Buzón HITL para aprobar o rechazar decisiones de IA antes de ejecutarse. **Sales:** consulta; aprobación típica de Manager/Admin. **Cuándo:** si ve contador de pendientes en la barra superior. **Impacto:** gobernanza; evita acciones automáticas no deseadas.

**3. Workforce (`/Agents`)** — Lista de agentes autónomos (LeadIntelligence, Communication, ChurnRisk, etc.) y decisiones recientes. **Cuándo:** entender qué automatización actuó sobre sus leads. **Impacto:** transparencia del sistema autónomo.

**4. Revenue OS (`/revenue`)** — **Pantalla principal del vendedor.** Dashboard de ingresos, fugas de pipeline explicadas por grafo, prioridades del día. **Cuándo:** primer y último acceso de la jornada. **Error común:** ignorar fugas detectadas. **Impacto:** directo en ingresos protegidos.

**5. Executive (`/executive`)** — Tablero consolidado para dirección; export HTML disponible. **Sales:** lectura para alinear forecast con dirección. **Manager:** uso primario.

**6. Pipeline (`/Deals`)** — Kanban por etapa + tabla con Forecast 30/60/90, Win Rate, Revenue Closed. **Cuándo:** varias veces al día al avanzar oportunidades. **Error común:** no actualizar monto del deal borrador. **Impacto:** forecast y comisiones.

**7. Directory (`/Customers`)** — Directorio paginado (50/página) con LTV, riesgo, estados. **Cuándo:** antes de crear deal manual; verificar si ya existe cuenta. **Impacto:** evita duplicados y errores de vinculación.

**8. Customer 360 (`/Customer360`)** — Búsqueda y vista integral: deals, salud, churn ML, comunicaciones. **Cuándo:** antes de llamada de renovación o upsell. **Colaboración:** Support usa más; Sales consulta en expansión.

**9. Customer Success (`/customer-success`)** — Tickets, casos y playbooks CS. **Sales:** escalar post-venta; no es pantalla primaria de prospección. **Support:** pantalla principal.

**10. Leads (`/Leads`)** — Embudo superior: crear, calificar, importar, bulk. Métricas: Qualified, New, HighScore. **Cuándo:** varias veces al día. **Regla:** ningún New >24h sin contacto.

**11. Memory (`/Memory`)** — Memoria semántica empresarial. **Sales:** consulta ocasional; Admin/Manager gestionan indexación.

**12. Tasks (`/Tasks`)** — **Segunda pantalla más importante.** Tareas Open, overdue, por prioridad. **Cuándo:** inicio y fin de jornada. **Error crítico:** ignorar tareas generadas por Qualify o SLA.

**13. Integrations (`/Integrations`)** — HubSpot, Salesforce, email, Stripe. **Sales:** no configura; usa datos sincronizados.

**14. Voice (`/VoiceCalls`)** — Registro de llamadas comerciales. **Cuándo:** tras llamadas importantes para trazabilidad.

**15–19. Admin (Users, Policies, Audit, Settings, Billing)** — Acceso denegado para Sales. Escalar al Manager o Admin.

### 4.3 Rutas comerciales críticas (fuera del sidebar)

| Ruta | Propósito |
|------|-----------|
| `/Leads/Create`, `/Edit`, `/Details` | CRUD y acciones de lead |
| `/Customers/Create`, `/Edit`, `/Details` | CRUD de clientes |
| `/Deals/Create`, `/Edit`, `/Details` | CRUD y cierre de deals |
| `/Leads/Import`, `/Customers/Import`, `/Deals/Import` | Importación masiva |
| `/Workflows` | Automatizaciones configurables |
| `/command/decisions` | Historial de decisiones IA |
| `/command/outcomes` | Outcome Fabric |
| `/command/playbooks` | Playbooks autónomos |
| `/customers/{id}/360` | Vista 360 individual |
| `/FailedEvents` | Cola DLQ de eventos fallidos |

### 4.4 Herramientas de navegación global

**Búsqueda rápida (Ctrl+K):** abre el paleta de búsqueda que consulta `/api/flow/search`. Permite saltar a Leads, Deals, Trust Studio, Revenue OS y otras rutas sin recorrer el menú.

**Barra superior:** incluye selector de idioma ES/EN, alternador de tema oscuro, indicador de entorno (si no es Production), píldora "Autonomous Active" cuando IA está habilitada, y menú de usuario con roles visibles.

**Paginación:** las listas de Leads, Customers, Deals y Users muestran 50 registros por página (`SearchPagedAsync`). Las tarjetas de resumen en la parte superior reflejan agregados del tenant o del filtro activo — no confunda la página visible con el total.

### 4.5 Errores de navegación frecuentes

| Error | Consecuencia | Solución |
|-------|--------------|----------|
| Intentar `/Users` como Sales | Access Denied | Escalar al Manager |
| Usar `/` cada mañana en lugar de `/revenue` | Pierde vista de prioridades de ingresos | Iniciar en Revenue OS |
| Ignorar `/Tasks` | SLA y automatizaciones parecen inactivas | Revisar tareas al inicio y cierre del día |
| Buscar deal borrador solo en Negotiation | No lo encuentra | Revisar todas las etapas; borrador inicia en Prospecting con monto $1 |

---

## Capítulo 5 — Roles, permisos y gobernanza operativa

### 5.1 Los cinco roles del sistema

AutonomusCRM define exactamente cinco roles en código: **Admin**, **Manager**, **Sales**, **Support** y **Viewer**. No existen roles personalizados fuera de esta enumeración en el seed y la gestión de usuarios.

### 5.2 Matriz de capacidades (resumen ejecutivo)

| Capacidad | Admin | Manager | Sales | Support | Viewer |
|-----------|:-----:|:-------:|:-----:|:-------:|:------:|
| Lectura general autenticada | ✅ | ✅ | ✅ | ✅ | ✅ |
| Crear/editar Leads, Customers, Deals (UI) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Qualify / Convert / Delete Lead | ✅ | ✅ | ✅ | ❌ | ❌ |
| Workflows y Policies escritura UI | ✅ | ✅ | ✅ | ❌ | ❌ |
| Gestión usuarios (`/Users`) | ✅ | ✅ | ❌ | ❌ | ❌ |
| Settings (`/Settings`) | ✅ | ✅ | ❌ | ❌ | ❌ |
| POST `/api/users`, `/api/tenants` | ✅ | ❌ | ❌ | ❌ | ❌ |
| API commercial POST (leads/customers/deals) | ✅* | ✅* | ✅* | ✅* | ✅* |

\*La API comercial solo exige autenticación; **no aplica filtro de rol** en los controllers. La UI bloquea a Support y Viewer mediante `CommercialWriteAuthorizationMiddleware`, pero la API presenta una brecha documentada.

### 5.3 Responsabilidades del Ejecutivo de Ventas (Sales)

**Su trabajo en AutonomusCRM:**

- Gestionar leads desde captación hasta calificación o descarte.
- Mantener el pipeline de deals actualizado con etapas, montos y fechas de cierre.
- Ejecutar y completar tareas asignadas o generadas automáticamente.
- Utilizar Revenue OS y Command para priorizar acciones comerciales.
- Registrar llamadas en Voice Calls cuando corresponda.
- Colaborar con Customer Success en cuentas con riesgo elevado.

**Fuera de su ámbito:**

- Crear o desactivar usuarios.
- Modificar configuración del tenant (MFA, comunicaciones, kill-switch IA).
- Aprobar decisiones en Trust Studio (responsabilidad típica de Manager/Admin).
- Configurar integraciones OAuth o facturación.

### 5.4 Interacción con otros roles

| Rol | Cómo colabora con Sales |
|-----|------------------------|
| Manager | Supervisa forecast, asigna leads de alto score, aprueba decisiones IA |
| Admin | Provisioning, integraciones, auditoría, resolución de Failed Events |
| Support | Gestiona tickets CS, Customer 360; escala oportunidades a Sales |
| Viewer | Consulta datos; no modifica registros comerciales en UI |

### 5.5 Buenas prácticas de gobernanza

1. Asigne rol **Sales** únicamente a ejecutivos comerciales activos.
2. Use **Viewer** para stakeholders que solo necesitan consultar pipeline y métricas.
3. No otorgue rol Admin a vendedores por comodidad.
4. Ante cambios sensibles, el Manager puede revisar `/Audit` (event store de dominio).
5. Reporte al Admin si detecta que usuarios no comerciales podrían escribir vía API.

Detalle completo: `03_ROLE_MATRIX.md`.

---

## Capítulo 6 — Conceptos fundamentales del CRM

### 6.1 Las tres entidades comerciales

AutonomusCRM modela el ciclo de ingresos con tres entidades principales. **No existe** una entidad separada llamada "Prospecto"; el prospecto inicial es un **Lead**.

| Entidad | Definición | Analogía |
|---------|------------|----------|
| **Lead** | Persona u organización interesada aún no consolidada como cuenta | Tarjeta de contacto de feria comercial |
| **Customer** | Cuenta o cliente en el directorio del tenant | Ficha de empresa cliente |
| **Deal** | Oportunidad de venta concreta, **obligatoriamente** vinculada a un Customer | Propuesta comercial en curso |

### 6.2 Estados del Lead (`LeadStatus`)

| Estado | Valor | Significado operativo |
|--------|-------|----------------------|
| New | 0 | Recién creado; requiere primer contacto |
| Contacted | 1 | Se estableció comunicación inicial |
| Qualified | 2 | Interés validado; automatización crea customer + deal borrador + tarea |
| Converted | 3 | Convertido manualmente a cliente |
| Lost | 4 | Oportunidad descartada tras contacto |
| Unqualified | 5 | No cumple criterio mínimo de perfil |

**Estado inicial al crear:** siempre `New` (UI, API o importación).

### 6.3 Fuentes de Lead (`LeadSource`)

Website, Referral, SocialMedia, EmailCampaign, ColdCall, Partner, Event, Other, Unknown (si no se especifica).

### 6.4 Estados del Customer (`CustomerStatus`)

| Estado | Cómo se alcanza |
|--------|-----------------|
| Prospect | Creación manual o automática al calificar lead |
| Lead | Transiciones de segmentación |
| Qualified | Calificación de cuenta |
| Customer | `CustomerCreatedEvent` o `DealClosedEvent` (retención) |
| VIP | `CustomerSegmentationEngine` (alto valor) |
| Churned | Analítica; sin transición automática única documentada |
| Inactive | Fusión de duplicados (`IdentityMergeService`) |

### 6.5 Estados y etapas del Deal

**Estado (`DealStatus`):** Open, Closed, OnHold, Cancelled.

**Etapa (`DealStage`) y probabilidad predeterminada:**

| Etapa | Probabilidad |
|-------|-------------|
| Prospecting | 10% |
| Qualification | 25% |
| Proposal | 50% |
| Negotiation | 75% |
| ClosedWon | 100% |
| ClosedLost | 0% |

**Nacimiento de un deal nuevo:** etapa Prospecting, estado Open, probabilidad 10%.

### 6.6 El journey comercial implementado

```
Lead (New) → Contacted → Qualified → [Deal pipeline] → ClosedWon
                ↓                              ↓
            Lost/Unqualified              Customer + LTV + CS tasks
```

**Tres caminos paralelos documentados** (inconsistencia operativa a estandarizar internamente):

| Acción | Lead final | Customer | Deal |
|--------|------------|----------|------|
| **Qualify** | Qualified | Auto-creado si no existe | Borrador auto ($1, IsDraft) |
| **Convert to Customer** | Converted | Creado (Prospect) | — |
| **Create Deal** | Sin cambio | Match/create por email | Creado en Prospecting |

**Recomendación:** el equipo comercial debe acordar **un proceso estándar**. Para ejecutivos nuevos, se recomienda **Qualify** como camino principal por activar automatizaciones completas.

### 6.7 Tareas (`WorkflowTask`)

Las tareas usan estados de texto: **Open** y **Completed** (no enum). Se vinculan a entidades (Lead, Deal, Customer) mediante `entityType` y `entityId`.

### 6.8 Glosario extendido para ejecutivos sin experiencia CRM

Esta sección define términos que escuchará en reuniones comerciales y que el sistema modela de forma explícita o implícita.

#### CRM (Customer Relationship Management)

Sistema para **registrar**, **seguir** y **medir** relaciones comerciales. En la práctica diaria significa: no perder contactos, saber en qué etapa está cada venta y tener tareas que le recuerden qué hacer hoy. AutonomusCRM es su CRM.

#### Lead (prospecto / contacto potencial)

Persona u organización que **mostró interés** pero aún no es cliente formal. Ejemplo real: alguien que completó un formulario en su web. En el sistema nace siempre como `New`. **No existe** entidad separada "Prospecto".

#### Prospecto (término de negocio, no entidad)

En conversaciones de ventas, "prospecto" = lead en estado inicial. Use `/Leads` para gestionarlos.

#### Contacto

En CRMs enterprise, contacto suele ser una persona dentro de una cuenta. AutonomusCRM modela la persona principal en el **Lead** (nombre, email, teléfono) y la **cuenta** en **Customer** (empresa).

#### Cuenta / Customer (cliente en directorio)

La ficha de empresa o cliente en `/Customers`. Puede estar en Prospect antes de la primera venta cerrada, o en Customer/VIP tras compras.

#### Cliente (estado comercial)

Cuando el negocio considera que la relación es de cliente activo. En el sistema, `CustomerStatus.Customer` se alcanza por retención tras `CustomerCreatedEvent` o `DealClosedEvent`.

#### Pipeline / Embudo

El recorrido visual de oportunidades desde primer contacto hasta cierre. En AutonomusCRM: `/Deals` con etapas Prospecting → ClosedWon/ClosedLost. El **embudo** es la metáfora; el **pipeline** es la vista operativa.

#### Oportunidad / Deal

Venta concreta con monto, etapa y probabilidad. **Siempre** vinculada a un Customer. Un mismo cliente puede tener varios deals (renovación, upsell).

#### Actividad y Seguimiento

Registro de interacciones (llamada, email, reunión). AutonomusCRM materializa el seguimiento principalmente mediante **Tasks** y eventos de dominio; las llamadas pueden registrarse en `/VoiceCalls`.

#### Tarea (Task)

Acción asignada con fecha límite. Es el mecanismo por el cual el sistema le "habla": "contacte este lead en 24h", "onboarding día 7". Revise `/Tasks` cada mañana.

#### Campaña

En CRMs de marketing, campaña = conjunto de acciones para generar leads. En AutonomusCRM, la **fuente** del lead (`LeadSource`: EmailCampaign, Event, etc.) registra el origen; no hay módulo de campañas de marketing automation independiente documentado en el código analizado.

#### Conversión

Pasar de prospecto a cliente u oportunidad avanzada. Tres caminos reales: **Qualify**, **Convert to Customer**, **Create Deal** (ver §6.6).

#### Automatización

Reglas que el sistema ejecuta sin clic manual: al calificar lead, al cerrar deal, cada 15 minutos en workers. Configurable además en `/Workflows`.

#### Inteligencia Artificial (IA)

En AutonomusCRM: scoring de leads, predicción de churn, priorización en Revenue OS y Command, aprobaciones en Trust Studio. **No reemplaza** su llamada; prioriza y sugiere. Los workers de fondo usan reglas y ML, no LLM.

#### Customer Journey (viaje del cliente)

Secuencia desde desconocido hasta promotor. Implementado: Lead → Qualified → Deal → ClosedWon → Customer + tareas CS → VIP (segmentación). Etapa "promotor" no es entidad; se refleja en LTV alto y segmento VIP.

#### Customer Lifetime Value (CLV / LTV)

Valor económico acumulado del cliente. Se actualiza al cerrar deals ganados. Visible en métricas de `/Customers` (AvgLtv, HighLtvCount).

#### Churn (abandono)

Cliente que deja de comprar o cancela. Estado `Churned` existe; predicción ML en Customer 360 (umbrales ≥60% Alto, ≥35% Medio).

#### Revenue (ingresos)

Dinero cerrado o proyectado. **Revenue Closed** = suma deals ClosedWon. **Forecast** = Amount × Probability en ventanas 30/60/90 días.

#### Forecast (pronóstico)

Proyección ponderada de cierre. Su Manager lo usa en `/executive` y `/Deals`. Complete siempre `ExpectedCloseDate` en deals abiertos.

#### Dashboard

Pantalla resumen de KPIs. Para Sales: `/revenue` (home), `/Deals` (pipeline), `/Leads` (embudo superior). Para Manager: `/executive`.

#### KPIs (indicadores clave)

Métricas para decisiones: Win Rate, Pipeline Open, QualifiedCount, HighScoreCount, retention rate (CS), tareas overdue. Cada una está en `07_REPORT_CATALOG.md`.

Detalle de flujos: `02_BUSINESS_FLOWS.md`.

---

## Capítulo 7 — Gestión de Leads (prospectos comerciales)

### 7.1 Acceso y vista de lista

**Ruta:** `/Leads` (menú Commerce → Leads).

La lista presenta:

- Tabla paginada (50 por página) con filtros por estado, fuente, búsqueda de texto.
- Tarjetas de resumen: TotalCount, QualifiedCount, NewCount, HighScoreCount (score > 70), AvgScore.
- Distribución por fuente (`SourceStats`).
- Acceso a crear, editar, importar y acciones masivas.

### 7.2 Procedimiento: crear un lead manualmente

**Paso 1.** En `/Leads`, pulse **Nuevo lead** (o acceda a `/Leads/Create`).

**Paso 2.** Complete los campos:
- **Nombre** (obligatorio)
- **Email** (recomendado; clave para deduplicación de Customer)
- **Teléfono**
- **Empresa**
- **Fuente** (Website, Referral, etc.)

**Paso 3.** Guarde el formulario.

**Paso 4.** Verifique que el lead aparece con estado **New**.

**Efectos automáticos tras guardar:**
- Se dispara `LeadCreatedEvent`.
- `RevenueAutomation` puede crear SLA de contacto en 24 horas.
- El worker `LeadIntelligenceAgent` calculará score (puede tardar minutos).
- `CommunicationAgent` puede enviar email de bienvenida si las comunicaciones están configuradas.

### 7.3 Procedimiento: primer contacto (New → Contacted)

**Paso 1.** Realice la llamada o envío de email fuera del CRM (el sistema no sustituye la comunicación).

**Paso 2.** Abra `/Leads/Details/{id}`.

**Paso 3.** Actualice el estado a **Contacted** si la UI lo permite, o edite el lead en `/Leads/Edit`.

**Paso 4.** Si existe tarea SLA de 24h en `/Tasks`, complétela al finalizar el contacto.

### 7.4 Procedimiento: calificar un lead (acción crítica)

La calificación es la acción más importante del ejecutivo de ventas en la fase superior del embudo.

**Paso 1.** Abra `/Leads/Details/{id}` del lead con interés confirmado.

**Paso 2.** Pulse **Qualify** (Calificar).

**Paso 3.** El sistema ejecuta `QualifyLeadCommand` y `OperationalAutomationService`:

| Efecto | Detalle |
|--------|---------|
| Estado del lead | `Qualified` (no `Converted`) |
| Customer | Creado si no existe uno con el mismo email |
| Deal borrador | Amount = 1, metadata `IsDraft=true`, vinculado al LeadId |
| Tarea | "Seguimiento lead calificado: {nombre}" — prioridad High, vencimiento 24h |

**Paso 4.** Vaya a `/Tasks` y localice la tarea generada.

**Paso 5.** Vaya a `/Deals`, localice el deal borrador (monto simbólico $1) y edítelo con el monto real.

**Paso 6.** Complete la tarea de seguimiento tras contactar al prospecto calificado.

> **Importante:** Qualify **no** marca el lead como Converted. Ese estado requiere la acción separada "Convert to Customer".

### 7.5 Convertir lead a cliente (camino alternativo)

**Cuándo usarlo:** cuando el prospecto ya es cliente administrativo sin oportunidad de deal activa.

**Procedimiento:**
1. `/Leads/Details/{id}` → **Convert to Customer**
2. Sistema crea Customer en estado Prospect
3. Lead pasa a `Converted`
4. `CustomerCreatedEvent` dispara retención (estado Customer, onboarding)

**Limitación:** esta acción está disponible en UI; no existe endpoint API dedicado documentado.

### 7.6 Crear deal desde lead (tercer camino)

**Procedimiento:**
1. `/Leads/Details/{id}` → **Create Deal**
2. Sistema busca Customer por email o crea uno
3. Deal nace en Prospecting / Open
4. **Estado del lead sin cambio**

### 7.7 Descarte: Lost y Unqualified

- **Lost:** prospecto contactado pero oportunidad descartada.
- **Unqualified:** no cumple perfil mínimo (sin necesidad de contacto previo).

Ambos disparan `LeadStatusChangedEvent` y pueden activar workflows configurados.

### 7.8 Score de lead y priorización

El worker `LeadIntelligenceAgent` procesa `LeadCreatedEvent` y actualiza el score (0–100). En la lista de leads:

- **HighScoreCount** cuenta leads con score > 70.
- Priorice contacto a leads con score elevado.
- Score 0 o vacío puede indicar que el agente aún no procesó el evento.

### 7.9 Importación y operaciones masivas

- **Importación:** `/Leads/Import` (CSV/JSON según implementación UI).
- **Bulk update:** `BulkUpdateLeadStatus` disponible en capa de aplicación y UI bulk.

Los leads importados disparan los mismos eventos y automatizaciones que los creados manualmente.

### 7.10 Regla de oro operativa

**No deje leads en estado New más de 24 horas.** El `CommercialSlaEngine` crea tareas de seguimiento alineadas con esta ventana. Incumplir el SLA genera alertas en Tasks y puede aparecer en Revenue OS como fuga de ingresos.

---

## Capítulo 8 — Gestión de Clientes (Customers)

### 8.1 Directorio de clientes

**Ruta:** `/Customers` (menú Customers → Directory).

**Métricas de resumen:**
- TotalCount
- AvgLtv (valor de vida promedio)
- HighLtvCount (LTV > 10.000)
- HighRiskCount (RiskScore > 70)
- AvgRisk, LowRiskCount

### 8.2 Procedimiento: crear cliente manualmente

**Paso 1.** `/Customers` → **Nuevo cliente** (`/Customers/Create`).

**Paso 2.** Complete nombre, email, teléfono y datos de empresa.

**Paso 3.** Guarde. Estado inicial: **Prospect**.

**Paso 4.** `CustomerCreatedEvent` activa `RetentionAutomationEngine`:
- Estado puede evolucionar a Customer
- Metadatos JourneyStage, OnboardingStarted
- Playbook de onboarding

### 8.3 Edición y detalle

- **Editar:** `/Customers/Edit/{id}` — Sales puede modificar email, teléfono y datos.
- **Detalle:** `/Customers/Details/{id}` — ficha completa con deals asociados.

### 8.4 Customer 360 (vista integral)

**Rutas:** `/Customer360` (búsqueda) y `/customers/{id}/360` (vista individual).

Consolida:
- Perfil y metadatos
- Deals vinculados
- Salud de cuenta y risk score
- Predicción churn ML (Alto ≥60%, Medio ≥35%)
- Comunicaciones y grafo de relaciones

**Uso para Sales:** consultar antes de reuniones de renovación o expansión; escalar a Support si risk score > 70.

### 8.5 Lifetime Value (LTV)

El LTV se incrementa al cerrar deals ganados (`DealClosedEvent` → retención suma Amount al LTV existente). No lo edite manualmente salvo corrección excepcional acordada con Manager.

### 8.6 Deduplicación

Al calificar un lead, el sistema reutiliza Customer existente si el email coincide (comparación sin distinguir mayúsculas). **Evite crear clientes duplicados** con variaciones del mismo email.

### 8.7 Operaciones masivas e importación

- `/Customers/Import` — carga masiva
- `/Customers/BulkActions` — acciones en lote
- `BulkUpdateCustomerStatus` en capa de aplicación

---

## Capítulo 9 — Pipeline y Oportunidades (Deals)

### 9.1 Acceso al pipeline

**Ruta:** `/Deals` (menú Revenue → Pipeline).

La pantalla combina:
- **Vista Kanban** por etapa (Prospecting → ClosedLost)
- **Tabla** con filtros y ordenación
- **Métricas agregadas:** Forecast 30/60/90, Win Rate, Revenue Closed, Pipeline Open

### 9.2 Procedimiento: crear deal

**Prerrequisito:** debe existir un Customer. El dominio exige `CustomerId` obligatorio.

**Paso 1.** `/Deals/Create` o **Create Deal** desde `/Leads/Details`.

**Paso 2.** Seleccione Customer existente o cree uno previamente.

**Paso 3.** Complete:
- Título del deal
- Monto (Amount)
- Descripción
- Fecha de cierre esperada (ExpectedCloseDate) — crítica para forecast

**Paso 4.** Guarde. Deal inicia en **Prospecting**, estado **Open**, probabilidad 10%.

### 9.3 Procedimiento: avanzar etapas del pipeline

| Momento comercial | Etapa en CRM | Probabilidad ref. |
|-------------------|--------------|-------------------|
| Primer contacto validado | Qualification | 25% |
| Propuesta enviada | Proposal | 50% |
| Negociación activa | Negotiation | 75% |
| Cierre favorable | ClosedWon | 100% |
| Pérdida confirmada | ClosedLost | 0% |

**Paso a paso:**
1. Abra `/Deals/Details/{id}`
2. Actualice etapa (o arrastre en Kanban si está activo)
3. Ajuste probabilidad manualmente si el escenario lo requiere (rango 0–100)
4. Actualice ExpectedCloseDate si cambió el calendario

### 9.4 Deal borrador post-Qualify

Tras calificar un lead, encontrará un deal con:
- Monto **$1** (simbólico)
- Metadata `IsDraft=true`
- Descripción indicando borrador automático

**Acción requerida:** edite en `/Deals/Edit` el monto real y la fecha de cierre antes de reportar forecast.

### 9.5 Cierre ganado (Close)

**Paso 1.** `/Deals/Details/{id}` → acción **Close**

**Paso 2.** Sistema ejecuta `CloseDealCommand` → `DealClosedEvent`

**Efectos automáticos:**
- Etapa ClosedWon, probabilidad 100%
- Retención: Customer a estado Customer, LTV actualizado, metadata de compra
- Tareas CS: Día 1 (Urgent), Día 7 (Normal), Día 30 (Normal)
- OutcomeAttribution + aprendizaje ABOS

**Paso 3.** Verifique en `/Tasks` las tareas de onboarding generadas (pueden asignarse a CS).

### 9.6 Cierre perdido (Lose)

**Procedimiento:** `/Deals/Details` → **Lose** → `DealLostEvent`, etapa ClosedLost.

Registre la pérdida con honestidad; el Win Rate depende de datos íntegros.

### 9.7 Forecast 30/60/90 y Win Rate

**Forecast:** suma ponderada (Amount × Probability) de deals abiertos cuya ExpectedCloseDate cae en ventanas de 30, 60 o 90 días.

**Win Rate:** ClosedWon / (ClosedWon + ClosedLost).

**Uso para Sales:** el forecast alimenta compromisos con dirección. Mantenga fechas y etapas actualizadas diariamente.

### 9.8 Importación y bulk

- `/Deals/Import` — importación masiva
- `BulkUpdateDealStage` — cambio de etapa en lote

---

## Capítulo 10 — Tareas y rutina diaria del ejecutivo de ventas

Este capítulo consolida la guía operativa de `13_DAILY_OPERATIONS_GUIDE.md` y `12_SALES_EXECUTIVE_TRAINING.md` en un protocolo ejecutable para `sales@autonomuscrm.local`.

### 10.1 Las cuatro pantallas del día

| Prioridad | Pantalla | Ruta | Propósito |
|-----------|----------|------|-----------|
| 1 | Revenue OS | `/revenue` | Qué proteger y priorizar hoy |
| 2 | Tasks | `/Tasks` | Qué hacer ahora |
| 3 | Leads | `/Leads` | Embudo superior |
| 4 | Pipeline | `/Deals` | Oportunidades activas |

### 10.2 Inicio de jornada (20 minutos)

| Minutos | Acción | Detalle |
|---------|--------|---------|
| 0–5 | Login → Revenue OS | Identifique 1–3 prioridades o fugas de ingreso |
| 5–10 | Tasks vencidas | `/Tasks?overdueOnly=true` — atender o reasignar |
| 10–15 | Leads New | `/Leads?status=0` — ninguno sin contacto > 24h |
| 15–20 | Deals activos | `/Deals` — columnas Proposal y Negotiation |

### 10.3 Durante el día: evento → acción en CRM

| Evento comercial real | Acción en AutonomusCRM |
|----------------------|------------------------|
| Llamada completada | Completar tarea relacionada; lead a Contacted si aplica |
| Email enviado | Actualizar notas en Details si disponible |
| Reunión agendada | Actualizar ExpectedCloseDate en deal |
| Propuesta enviada | Mover deal a **Proposal** |
| Objeción de precio | Mover a **Negotiation**; ajustar probabilidad |
| Cierre verbal | **Close** deal; verificar tareas onboarding |
| Prospecto sin interés | Lead a **Lost** o **Unqualified** |

### 10.4 Fin de jornada (15 minutos)

**Checklist de cierre:**

- [ ] `/Tasks` — cero tareas overdue propias (o justificadas)
- [ ] `/Leads` — cero leads New sin contacto > 24h
- [ ] `/Deals` — etapas actualizadas según movimiento del día
- [ ] `/` (Command) — revisar al menos 1 insight o decisión relevante
- [ ] Voice Calls — registrar llamadas significativas si aplica

### 10.5 Gestión de tareas en `/Tasks`

**Filtros disponibles:** estado (Open/Completed), asignado, prioridad, vencimiento, overdue.

**Procedimiento completar tarea:**
1. Localice la tarea (vinculada a Lead, Deal o Customer)
2. Ejecute la acción comercial real (llamada, email, reunión)
3. Marque la tarea como **Completed** en la UI
4. El sistema deja de contarla en overdue

**Origen de tareas automáticas:**
- Qualify lead → seguimiento 24h High
- Deal ClosedWon → onboarding D0, D7, D30
- SLA comercial → contacto lead nuevo
- Revenue scan (cada 15 min) → deals estancados, leads inactivos
- Workflows activos → CreateTask

### 10.6 Registro de llamadas

**Ruta:** `/VoiceCalls` (Platform → Voice).

Registre llamadas comerciales significativas para trazabilidad. No sustituye el CRM de telefonía; es log manual orientado a seguimiento.

---

## Capítulo 11 — Revenue OS y Command Center

### 11.1 Revenue OS — su centro de mando comercial

**Ruta:** `/revenue`  
**Servicio:** `IRevenueOsService.GetDashboardAsync`  
**Complemento:** `IGraphReasoningEngine.DetectRevenueLeakAsync`

Revenue OS responde a la pregunta: **¿Dónde se están perdiendo ingresos y qué debo hacer hoy?**

**Contenido típico del dashboard:**
- Indicadores de ingresos y pipeline
- Detección de fugas (deals estancados, leads inactivos, cuentas en riesgo)
- Explicación en grafo del razonamiento (por qué el sistema identifica una fuga)
- Acciones sugeridas vinculadas a entidades

**Protocolo matutino (5 minutos):**
1. Abra `/revenue`
2. Lea las fugas o alertas principales
3. Para cada fuga relevante, abra la entidad (Lead/Deal/Customer) y ejecute acción
4. Cree o complete tarea si el sistema la generó

### 11.2 Command Center — panorama estratégico

**Ruta:** `/` (menú Command → Command)  
**Servicio:** `IAiCommandCenterService.GetFlowCommandAsync`  
**Periodo:** 7 o 30 días (selector en pantalla)

**Métricas principales:**
- Revenue generado en el periodo
- Revenue protegido en el periodo
- Cuentas en riesgo
- Expansiones y renovaciones detectadas
- Decisiones en las últimas 24h
- Business outcomes últimos 7 días
- Snapshot del workforce autónomo

**Cuándo usar Command vs Revenue OS:**

| Pregunta | Pantalla |
|----------|----------|
| ¿Qué debo hacer esta mañana para vender? | Revenue OS |
| ¿Cómo va el flujo autónomo y las decisiones IA? | Command |
| ¿Cuánto revenue generó/protegió el sistema? | Command (periodo 7/30d) |

### 11.3 Executive OS (consulta para Sales)

**Ruta:** `/executive` — home de Admin/Manager.

Sales puede consultar para entender vista de dirección. Export HTML disponible con `?handler=Export`. No es pantalla de trabajo diario del vendedor.

### 11.4 Rutas Command extendidas

| Ruta | Contenido |
|------|-----------|
| `/command/decisions` | Historial filtrable de decisiones |
| `/command/outcomes` | Outcome Fabric — atribución de resultados |
| `/command/playbooks` | Estados de playbooks autónomos |

Detalle de métricas: `07_REPORT_CATALOG.md`.

---

## Capítulo 12 — Inteligencia artificial, Trust Studio y Workforce

### 12.1 Modelo de IA en AutonomusCRM (expectativas realistas)

AutonomusCRM combina tres capas de inteligencia, con alcances distintos:

| Capa | Componentes | Uso por Sales |
|------|-------------|---------------|
| Operativa | LeadIntelligenceAgent, DealStrategyAgent, Revenue scan | Score, tareas, priorización |
| ML Enterprise | Churn V2, Expansion, Revenue prediction, NBA | Riesgo en Customer 360, sugerencias |
| LLM (API) | OpenAI, Azure, Anthropic, Gemini | No cableado en Workers |

**Principio fundamental:** la IA **prioriza, puntúa, predice y crea tareas**. El ejecutivo **ejecuta** las acciones comerciales. No existe chat LLM integrado en el flujo diario del worker.

### 12.2 Trust Studio (Human-in-the-Loop)

**Ruta:** `/TrustInbox`

Trust Studio es el buzón de aprobaciones humanas (HITL) para decisiones autónomas que requieren validación antes de ejecutarse.

**Para Sales:**
- Puede **consultar** el estado (badge en sidebar si hay pendientes)
- La **aprobación/rechazo** es responsabilidad típica de Manager/Admin
- Trust vacío puede significar simplemente que no hay decisiones pendientes

**Servicio:** `ITrustMetricsService` — métricas SLA, severidad, umbrales.

### 12.3 Workforce (Agentes autónomos)

**Ruta:** `/Agents`

Muestra agentes registrados y decisiones recientes:

| Agente | Función |
|--------|---------|
| LeadIntelligenceAgent | Score de leads |
| CommunicationAgent | Email/WhatsApp bienvenida |
| CustomerRiskAgent | Risk score |
| CustomerHealthAgent | Playbooks rescue/adoption |
| ChurnRiskAgent | Acciones churn (RiskScore ≥ 60) |
| DealStrategyAgent | Tareas inteligencia ventas |
| OutcomeAttribution | Aprendizaje post cierre |

### 12.4 Memory (Memoria empresarial)

**Ruta:** `/Memory`

Memoria semántica que indexa contexto de clientes, retención y decisiones. Principalmente consulta para Admin/Manager; Sales puede revisar si necesita contexto histórico enriquecido.

### 12.5 Configuración y kill-switch

- Layout lee `AI:Enabled` (predeterminado true)
- Settings del tenant puede incluir kill-switch para funciones autónomas
- `AutonomousPlatformGate` controla ejecución de decisiones

### 12.6 Errores de expectativa frecuentes

| Expectativa incorrecta | Realidad |
|------------------------|----------|
| "La IA cierra ventas sola" | Crea tareas y decisiones; humano cierra |
| "ChatGPT redacta todos mis emails" | CommunicationAgent usa plantillas configuradas |
| "Score 0 = lead malo" | Puede significar que el agente aún no procesó |
| "Debo aprobar en Trust Studio" | Depende de política; típicamente Manager |

Detalle completo: `06_AI_CATALOG.md`.

---

## Capítulo 13 — Customer Success y Customer 360

### 13.1 División de responsabilidades Sales vs Support

| Área | Rol principal | Rutas |
|------|---------------|-------|
| Captación y cierre | **Sales** | Leads, Deals, Revenue OS |
| Post-venta y tickets | **Support** | Customer Success, Customer 360 |
| Cuentas en riesgo | **Colaboración** | Customers (risk), CS playbooks |

La ruta `/Support` redirige a `/customer-success`.

### 13.2 Customer Success OS

**Ruta:** `/customer-success`

Incluye:
- Tickets y casos
- Playbooks CS (onboarding, rescue, re-engagement)
- KPIs de retención (`CustomerKpiService`)

**Para Sales:** consulte cuando un deal cerrado genere tareas CS; coordine en cuentas con oportunidad de expansión y riesgo simultáneo.

### 13.3 Playbooks de retención (implementados)

| Playbook | Trigger típico |
|----------|----------------|
| Onboarding | `CustomerCreatedEvent`, deal ganado |
| Rescue | RiskScore ≥ 70 |
| ReEngagement | Sin contacto > 45 días |

### 13.4 Señales de riesgo que Sales debe conocer

| Señal | Dónde verla | Acción |
|-------|-------------|--------|
| RiskScore > 70 | `/Customers` métricas, Customer 360 | Coordinar con CS; evaluar expansión |
| Churn ML Alto (≥60%) | Customer 360 | No ignorar antes de renovación |
| Health Critical | Decisiones autónomas | Escalar a Manager |

### 13.5 Comunicaciones automáticas post-venta

Tras deal ganado, `RetentionAutomationEngine` puede:
- Enviar email plantilla "Onboarding"
- Enviar WhatsApp re-engagement (si teléfono configurado)
- Crear contrato anual en metadata
- Persistir salud de cuenta

**Requisito:** comunicaciones configuradas en Settings (SendGrid/WhatsApp). Si el banner de comms indica no configurado, las plantillas no se envían.

---

## Capítulo 14 — Automatizaciones, workers y ciclo de retención

### 14.1 Arquitectura event-driven

AutonomusCRM opera con **eventos de dominio** que disparan motores síncronos (en API) y agentes asíncronos (vía RabbitMQ en Workers).

**Dispatcher principal:** `DomainEventDispatcher`

| Motor | Eventos clave | Efecto |
|-------|---------------|--------|
| WorkflowEngine | Cualquier evento con workflow activo | Assign, UpdateStatus, CreateTask |
| OperationalAutomation | LeadQualified, DealClosed | Customer+deal draft+task / onboarding |
| RevenueAutomation | LeadCreated, LeadScoreUpdated, LeadQualified | SLA, asignación score alto |
| RetentionAutomation | CustomerCreated, DealClosed, Risk≥70 | Status, playbooks, emails |
| AutonomousOrchestration | Varios (gated) | Decisiones autónomas |
| BusinessMemoryPipeline | Seleccionados | Episodios memoria semántica |

### 14.2 Workers y frecuencia de escaneo

**Worker principal (`Worker.cs`):** ciclo cada **15 minutos** por tenant.

**Escaneos incluidos:**
- Revenue scan (deals estancados, leads inactivos)
- Data quality (`DataQualityRevenueService`)
- Retention scan
- Renewal / Expansion agents
- Intelligence scan
- Customer insights
- Ciclo autónomo completo
- Optimización de workflows

**Worker separado:** `BusinessMemoryConsolidationWorker` cada **6 horas**.

### 14.3 Workflows configurables

**Ruta:** `/Workflows` (Admin/Manager/Sales escritura UI)

**Modelo:** Triggers (DomainEvent) + Conditions + Actions

**Acciones disponibles:**
- Assign
- UpdateStatus
- CreateTask
- Communicate*
- ActivateAgent*

\* **Limitación crítica:** `Communicate` y `ActivateAgent` **solo registran log** en `WorkflowEngine`. No envían mensajes ni activan agentes LLM. Para comunicaciones reales, dependa de `CommunicationAgent` vía eventos de dominio o configuración de retención.

### 14.4 Cadena Qualify → Retención (referencia rápida)

```
Lead.Qualify()
  → LeadQualifiedEvent
    → OperationalAutomation: Customer + Deal draft + Task
    → RevenueAutomation: SLA/score
  → [Ventas cierra Deal]
    → DealClosedEvent
      → RetentionAutomation: LTV, Customer status, onboarding tasks, emails
      → OperationalAutomation: CS tasks D0/D7/D30
```

### 14.5 Monitoreo de automatizaciones

| Ruta | Propósito |
|------|-----------|
| `/Tasks` | Ver tareas generadas |
| `/Audit` | Event store (Admin/Manager) |
| `/FailedEvents` | DLQ — eventos no procesados |

Si las automatizaciones "no funcionan", verifique: worker activo, RabbitMQ operativo, workflow **activo** para el tenant, y ausencia de eventos en FailedEvents.

Detalle: `05_AUTOMATION_CATALOG.md`.

---

## Capítulo 15 — Simulación de escenario comercial completo

Este capítulo reproduce un **caso integral** usando únicamente funcionalidades reales del sistema. El escenario sigue el Playbook 1 de `10_OPERATIONAL_PLAYBOOK.md`, ampliado para formación de ejecutivos sin experiencia CRM.

### 15.1 Contexto del escenario

**Empresa ficticia del prospecto:** Logística Norte S.A.  
**Contacto:** María González, Directora de Operaciones  
**Origen:** Formulario web (descarga de whitepaper)  
**Ejecutivo:** Usted, autenticado como `sales@autonomuscrm.local`  
**Objetivo:** Convertir el lead en deal cerrado y verificar cadena de retención

---

### 15.2 Fase 1 — Captación (Día 1, 09:00)

**Situación:** El marketing informa que María González completó un formulario. El lead puede existir por importación API; usted lo crea manualmente para practicar.

| Paso | Acción | Ruta / Verificación |
|------|--------|---------------------|
| 1 | Login | `/Account/Login` → redirección a `/revenue` |
| 2 | Revisar Revenue OS | Anote prioridades existentes |
| 3 | Crear lead | `/Leads/Create` |
| 4 | Datos | Nombre: María González; Email: maria.gonzalez@logisticanorte.demo; Empresa: Logística Norte S.A.; Fuente: Website |
| 5 | Guardar | Estado = **New** |
| 6 | Verificar eventos | En minutos: score puede aparecer; tarea SLA 24h posible en `/Tasks` |

**Resultado esperado:** Lead `New`, evento `LeadCreatedEvent` procesado, worker `LeadIntelligenceAgent` calculando score.

---

### 15.3 Fase 2 — Primer contacto (Día 1, 10:30)

| Paso | Acción | Verificación |
|------|--------|--------------|
| 1 | Revisar Tasks | Localizar tarea SLA si existe |
| 2 | Llamar a María (fuera del CRM) | Conversación de descubrimiento |
| 3 | Actualizar lead | `/Leads/Edit` → estado **Contacted** |
| 4 | Completar tarea SLA | `/Tasks` → Completed |

**Resultado esperado:** Lead `Contacted`, sin tareas overdue de este lead.

---

### 15.4 Fase 3 — Calificación (Día 1, 11:00)

María confirma interés en software de gestión comercial para 15 usuarios.

| Paso | Acción | Verificación |
|------|--------|--------------|
| 1 | Abrir detalle | `/Leads/Details/{id}` |
| 2 | Clic **Qualify** | Lead → **Qualified** |
| 3 | Verificar Customer | `/Customers` — buscar por email; debe existir (Prospect) |
| 4 | Verificar deal borrador | `/Deals` — deal con Amount=1, IsDraft |
| 5 | Verificar tarea | `/Tasks` — "Seguimiento lead calificado: María González", High, 24h |
| 6 | Editar deal | `/Deals/Edit` — Amount: 45000; ExpectedCloseDate: +30 días; título: "Logística Norte — Licencias 15 usuarios" |

**Resultado esperado:** Pipeline con deal real; tarea de seguimiento pendiente.

---

### 15.5 Fase 4 — Desarrollo del pipeline (Días 2–10)

| Día | Hito comercial | Acción CRM |
|-----|----------------|------------|
| 2 | Demo realizada | Deal → **Qualification** (25%) |
| 5 | Propuesta enviada | Deal → **Proposal** (50%) |
| 8 | Negociación precio | Deal → **Negotiation** (75%) |
| 10 | Revisar Revenue OS | Confirmar que deal no aparece como estancado |

**En cada hito:** complete tareas en `/Tasks`; actualice ExpectedCloseDate si hubo deslizamiento.

---

### 15.6 Fase 5 — Cierre ganado (Día 12)

| Paso | Acción | Verificación |
|------|--------|--------------|
| 1 | Cierre verbal confirmado | — |
| 2 | Close deal | `/Deals/Details` → **Close** |
| 3 | Verificar etapa | ClosedWon, probabilidad 100% |
| 4 | Verificar Customer | Estado evoluciona hacia **Customer**; LTV += 45000 |
| 5 | Verificar tareas CS | `/Tasks` — onboarding D0 (Urgent), D7, D30 |
| 6 | Command | `/` — revisar outcome en métricas 7d |

**Resultado esperado:** Cadena DealClosedEvent → RetentionAutomation → OperationalAutomation completada.

---

### 15.7 Fase 6 — Post-venta y colaboración CS (Días 13–45)

| Paso | Responsable | Acción |
|------|-------------|--------|
| 1 | CS (Support) | Gestiona tickets en `/customer-success` |
| 2 | Sales | Consulta `/customers/{id}/360` antes de llamada de expansión |
| 3 | Sistema | Scan retención cada 15 min persiste salud |
| 4 | Sales | Si RiskScore sube > 70, coordina playbook Rescue |

---

### 15.8 Fase 7 — Cierre del ejercicio de formación

**Checklist de evaluación (Módulo 8 de `12_SALES_EXECUTIVE_TRAINING.md`):**

- [ ] Creé un lead y lo califiqué
- [ ] Abrí la tarea generada y la completé
- [ ] Creé/edité un deal vinculado a cliente con monto real
- [ ] Moví deal hasta Negotiation como mínimo
- [ ] Cerré deal ganado y verifiqué tareas onboarding
- [ ] Interpreté Forecast 30d en `/Deals`
- [ ] Revisé Revenue OS e identifiqué al menos 1 prioridad
- [ ] Consulté Command para ver impacto en métricas

**Lecciones del escenario:**
1. **Qualify** es el camino más eficiente para activar automatizaciones.
2. El deal borrador $1 **no es error** — es señal de completar datos.
3. Las tareas son el mecanismo por el cual el sistema "habla" con el vendedor.
4. El cierre dispara una **segunda cadena** (retención) tan importante como la venta.

---

## Capítulo 16 — FAQ empresarial (150 preguntas y respuestas)

**Audiencia:** Ejecutivo de ventas sin experiencia previa en CRM  
**Fuente:** funcionalidades reales documentadas en el código  
**Total:** 150 preguntas numeradas de forma continua (1–150)

> Copia autoritativa también en `08_FAQ.md` (misma carpeta).

### Categoría 1: Conceptos CRM

**1. ¿Qué es AutonomusCRM?**  
AutonomusCRM es una plataforma de operaciones de ingresos y clientes que centraliza prospectos (leads), clientes, oportunidades de venta (deals), tareas y analítica de ingresos en un solo sistema web autenticado.

**2. ¿Qué significa CRM en la práctica diaria?**  
CRM significa Customer Relationship Management: una herramienta para registrar contactos comerciales, seguir oportunidades, asignar tareas y medir resultados de ventas sin depender de hojas de cálculo dispersas.

**3. ¿Existe una entidad separada llamada "Prospecto"?**  
No. En AutonomusCRM el prospecto inicial es un **Lead** con estado `New`. No hay una entidad independiente llamada Prospecto en el dominio del sistema.

**4. ¿Cuál es la diferencia entre Lead, Customer y Deal?**  
Un **Lead** es un contacto potencial aún no consolidado como cuenta. Un **Customer** es la cuenta o cliente en el directorio. Un **Deal** es una oportunidad de venta concreta vinculada obligatoriamente a un Customer.

**5. ¿Qué es un tenant en AutonomusCRM?**  
Un tenant es la organización o empresa aislada dentro del sistema. Todos los leads, clientes y deals pertenecen a un `TenantId`; los usuarios solo ven datos de su tenant.

**6. ¿Qué es el pipeline comercial?**  
El pipeline es el recorrido de una oportunidad desde prospección hasta cierre. En AutonomusCRM se representa principalmente en `/Deals` con etapas desde Prospecting hasta ClosedWon o ClosedLost.

**7. ¿Qué son los estados de un Lead frente a los de un Customer?**  
Son ciclos distintos. El Lead usa estados como New, Contacted, Qualified, Converted, Lost y Unqualified. El Customer usa Prospect, Lead, Qualified, Customer, VIP, Churned e Inactive.

**8. ¿Qué es Revenue OS?**  
Revenue OS es el módulo de ingresos accesible en `/revenue`. Muestra dashboard unificado de ingresos, fugas de pipeline y explicaciones del grafo de razonamiento para priorizar acciones comerciales.

**9. ¿Qué es Command en la interfaz?**  
Command es la pantalla de inicio operativo en `/` (también llamada Command Center). Presenta decisiones de IA, métricas de flujo, cuentas en riesgo y un snapshot del workforce autónomo.

**10. ¿Qué es Trust Studio?**  
Trust Studio (`/TrustInbox`) es el buzón de aprobaciones humanas (HITL) donde Admin y Manager pueden aprobar o rechazar decisiones autónomas de IA antes de que se ejecuten.

**11. ¿AutonomusCRM reemplaza el correo electrónico o las llamadas?**  
No lo reemplaza. Registra la actividad comercial, crea tareas de seguimiento y conecta datos; las comunicaciones reales las realiza el ejecutivo o automatizaciones configuradas (email/WhatsApp en módulos de retención).

**12. ¿Necesito conocimientos técnicos para usar el CRM como vendedor?**  
No. Como ejecutivo de ventas puede operar desde `/revenue`, `/Leads`, `/Deals`, `/Customers` y `/Tasks` con formularios web. La administración avanzada queda para Admin y Manager.

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

*Fin del FAQ — 150 ítems. Copia espejo en `08_FAQ.md`.*

---

## Capítulo 17 — Reportes, métricas y resolución de incidencias

### 17.1 Catálogo de métricas por pantalla

| Pantalla | Métricas clave | Servicio / origen |
|----------|----------------|-------------------|
| Command `/` | Revenue generado/protegido, decisiones 24h, outcomes 7d | `IAiCommandCenterService` |
| Revenue OS | Fugas de ingreso, grafo explicativo | `IRevenueOsService`, `IGraphReasoningEngine` |
| Executive | Tablero consolidado, export HTML | `IExecutiveOsService` |
| Leads | Total, Qualified, New, HighScore, AvgScore, SourceStats | `LeadRepository.GetListSummaryAsync` |
| Deals | Forecast 30/60/90, Win Rate, Revenue Closed, Pipeline Open | `DealRepository.GetListSummaryAsync` |
| Customers | Total, AvgLtv, HighLtv, HighRisk, AvgRisk | `CustomerRepository.GetListSummaryAsync` |
| Tasks | Conteos, overdue | `CountOverdueOpenAsync` |
| Trust Studio | Pendientes, SLA, severidad | `ITrustMetricsService` |
| Customer Success | Retention rate, KPIs CS | `CustomerKpiService` |
| Audit | Eventos por tipo, export JSON 10k | Event store |

### 17.2 Cómo interpretar métricas (guía Sales)

1. **Revenue OS** → acciones para hoy (proteger ingresos)
2. **Leads HighScoreCount** → calidad del embudo superior
3. **Deals Forecast 30d** → compromiso con dirección esta semana
4. **Tasks overdue** → riesgo de SLA y percepción de "CRM roto"
5. **Cards de resumen vs tabla paginada** → las cards reflejan agregados del filtro/tenant; la tabla muestra 50 registros

### 17.3 API analítica (referencia)

Para integraciones externas (no uso diario de Sales):

| Endpoint | Contenido |
|----------|-----------|
| `GET api/ai/dashboard` | Executive AI dashboard |
| `GET api/ai/analytics` | Executive AI analytics |
| `GET api/ai/ml/churn` | Predicciones churn |
| `GET api/ai/ml/expansion` | Oportunidades expansión |
| `GET api/ai/ml/revenue` | Forecast ML |
| `GET api/ai/governance` | Reporte gobernanza IA |

### 17.4 Resolución de incidencias frecuentes

| Problema | Causa probable | Solución |
|----------|---------------|----------|
| Access Denied al crear Lead | Rol Support/Viewer | Solicitar rol Sales al Manager |
| No aparece deal tras Qualify | Deal borrador en otra etapa / filtro | Buscar en `/Deals` todas las etapas |
| Tareas no se crean | Worker caído / RabbitMQ | Escalar a Admin; verificar Docker logs |
| Score siempre vacío | Worker no procesó | Revisar `/FailedEvents`; replay DLQ |
| Forecast en $0 | Sin ExpectedCloseDate en deals abiertos | Actualizar fechas de cierre |
| Email no enviado | Comms no configurado | Admin configura en Settings |
| Trust Studio vacío | Sin decisiones HITL pendientes | Normal; revisar umbrales con Manager |
| Workflow no dispara | Workflow inactivo o trigger incorrecto | Admin/Manager revisa `/Workflows/Edit` |
| Communicate no envía | Acción solo log | Usar CommunicationAgent vía eventos |
| Paginación muestra 50 | Diseño SearchPagedAsync | Normal; usar filtros y cards resumen |

**Logs de diagnóstico (Admin/DevOps):** `docker logs autonomuscrm-api`, `docker logs autonomuscrm-workers`

Detalle completo: `09_TROUBLESHOOTING.md` y `07_REPORT_CATALOG.md`.

---

## Capítulo 18 — Mejores prácticas globales (Best Practices)

Este capítulo adapta estándares de **Salesforce**, **HubSpot**, **Microsoft Dynamics 365**, **Pipedrive** y **Zoho CRM** al alcance real de AutonomusCRM. Cada práctica indica la pantalla o automatización correspondiente en el producto.

### 18.1 Higiene de datos (estándar Salesforce)

| Práctica enterprise | Cómo ejecutarla en AutonomusCRM |
|---------------------|--------------------------------|
| Un registro = una verdad | Use email único en Lead; Qualify reutiliza Customer por email |
| Campos obligatorios mínimos | Nombre en Lead; CustomerId en Deal; ExpectedCloseDate para forecast |
| No duplicar cuentas | Antes de crear Customer manual, busque en `/Customers` |
| Fuente siempre informada | Seleccione `LeadSource` al crear (Website, Event, etc.) |
| Cierre limpio | ClosedLost con motivo en notas; no deje deals Open abandonados |

**Error frecuente:** crear el mismo prospecto tres veces desde feria, web y referido. **Solución:** búsqueda Ctrl+K o filtro en `/Leads` antes de crear.

### 18.2 Gestión del embudo (estándar HubSpot / Pipedrive)

| Práctica | Implementación AutonomusCRM |
|----------|----------------------------|
| Definir etapas con criterios de salida | Prospecting → Qualification → Proposal → Negotiation (probabilidades 10–75%) |
| Mover etapa solo con evidencia | Tras demo → Qualification; tras propuesta enviada → Proposal |
| Actualizar monto y fecha en cada movimiento | `/Deals/Edit` — el deal borrador $1 debe actualizarse de inmediato |
| Revisar pipeline 2× por semana | `/Deals` kanban + Forecast 30d |
| Win Rate como retroalimentación | Métrica en resumen de `/Deals`; analice pérdidas en ClosedLost |

**Regla HubSpot adaptada:** si un deal lleva >14 días sin cambio de etapa, Revenue OS puede marcarlo como fuga — actúe antes del scan de 15 min.

### 18.3 Velocidad de respuesta a leads (estándar inbound)

| SLA recomendado | Soporte en AutonomusCRM |
|-----------------|------------------------|
| Primer contacto < 24 h | `CommercialSlaEngine` crea tarea SLA tras `LeadCreatedEvent` |
| Leads High Score primero | Priorice score > 70 (`HighScoreCount` en `/Leads`) |
| Qualify en primera conversación válida | Un clic en Details activa Customer + deal + tarea |
| Completar tarea al contactar | `/Tasks` → Complete; evita overdue y alertas |

**Práctica Dynamics 365:** trate la tarea de 24 h como compromiso contractual con el prospecto.

### 18.4 Forecast y Revenue Operations (estándar RevOps)

| Práctica RevOps | Pantalla |
|-----------------|----------|
| Forecast semanal con Manager | `/Deals` Forecast 30/60/90 + `/executive` (Manager) |
| Revenue OS cada mañana | `/revenue` — fugas y prioridades del día |
| Probabilidad honesta | No deje 75% en Negotiation si falta aprobación interna del cliente |
| Pipeline coverage | Pipeline Open vs cuota; Revenue Closed como histórico |
| Atribución de resultados | `/command/outcomes` para aprendizaje de playbooks |

### 18.5 Customer Success y expansión (estándar post-venta)

| Práctica | AutonomusCRM |
|----------|--------------|
| Handoff venta → CS | CloseWon dispara tareas onboarding D1/D7/D30 |
| Consultar 360 antes de upsell | `/customers/{id}/360` — churn ML, LTV, deals |
| Coordinar con Support en riesgo | RiskScore ≥ 70 → playbook Rescue |
| No vender sin salud de cuenta | Revise health antes de propuesta de expansión |
| Registrar llamadas | `/VoiceCalls` para trazabilidad comercial |

### 18.6 Uso responsable de IA (estándar enterprise AI governance)

| Hacer | No hacer |
|-------|----------|
| Usar score y Revenue OS para priorizar | Ignorar criterio humano por sugerencia IA |
| Consultar Command para panorama 7/30 días | Asumir que workers redactan emails con LLM |
| Reportar decisiones erróneas al Manager | Aprobar en Trust Studio sin rol (Sales consulta) |
| Completar datos antes de predicción churn | Esperar churn ML sin historial de compras |

### 18.7 Seguridad y cumplimiento operativo

| Práctica | Detalle |
|----------|---------|
| Cuenta personal | `sales@autonomuscrm.local` — no compartir |
| MFA | Solicite al Admin si el tenant lo exige |
| No exportar datos sensibles | Sin herramienta masiva de export para Sales en UI estándar |
| Reportar brecha API | UI bloquea Support/Viewer; API comercial no filtra por rol |
| Auditoría | Manager/Admin revisan `/Audit` ante disputas |

### 18.8 Errores que destruyen adopción (anti-patrones)

1. **No calificar leads** — pierde automatización Customer + deal + tarea.
2. **Dejar deal borrador en $1** — forecast y Revenue OS distorsionados.
3. **Ignorar `/Tasks`** — SLA incumplido; percepción de "CRM inútil".
4. **Tres procesos distintos** (Qualify/Convert/Create Deal) en el mismo equipo — estandarice uno.
5. **Cerrar deal sin Customer correcto** — imposible en dominio, pero verifique email antes de Qualify.
6. **Confiar en workflow Communicate** — solo log; use CommunicationAgent/retention para emails reales.

### 18.9 Checklist semanal del ejecutivo de ventas

| Día | Acción | Ruta |
|-----|--------|------|
| Lunes AM | Revenue OS + tareas overdue | `/revenue`, `/Tasks` |
| Diario | Leads New > 24h | `/Leads` filtro New |
| Miércoles | Mover deals estancados | `/Deals` kanban |
| Viernes PM | Actualizar ExpectedCloseDate | `/Deals/Edit` |
| Viernes PM | Win/Loss review con notas | Deals ClosedLost |

### 18.10 Referencias cruzadas de capacitación

| Documento | Contenido |
|-----------|-----------|
| `12_SALES_EXECUTIVE_TRAINING.md` | 8 módulos formativos |
| `13_DAILY_OPERATIONS_GUIDE.md` | Rutina imprimible |
| `10_OPERATIONAL_PLAYBOOK.md` | Playbooks por situación |
| `14_MANAGER_GUIDE.md` | Supervisión y forecast |
| `15_SYSTEM_CAPABILITIES_MATRIX.md` | Qué está implementado vs. parcial |

---

## Apéndice A — Checklist de primer día (Sales)

1. [ ] Login exitoso → `/revenue`
2. [ ] Recorrido menú lateral (19 ítems identificados)
3. [ ] Crear lead de prueba
4. [ ] Calificar lead y verificar Customer + Deal borrador + Tarea
5. [ ] Editar deal con monto real
6. [ ] Completar una tarea
7. [ ] Revisar métricas en Leads y Deals
8. [ ] Leer FAQ 1–30 en `08_FAQ.md`

## Apéndice B — Contactos de escalamiento

| Situación | Escalar a |
|-----------|-----------|
| Necesito rol o permisos | Manager |
| Integraciones, billing, Failed Events | Admin |
| Ticket post-venta, cuenta en riesgo | Support (Customer Success) |
| Incidencia técnica producción | Admin / DevOps |

## Apéndice C — Limitaciones conocidas del producto

| Componente | Estado | Impacto operativo |
|------------|--------|-------------------|
| API commercial POST | Sin filtro `[Authorize(Roles=...)]` | Support/Viewer bloqueados en UI pero API autenticada permite escritura |
| Policies RequireManager/RequireSales | Registradas, no aplicadas en endpoints comerciales | Solo RequireAdmin en áreas admin |
| Workflow `Communicate` | Solo log en WorkflowEngine | No envía mensajes; usar CommunicationAgent |
| Workflow `ActivateAgent` | Solo log | No activa agentes LLM |
| Convert Lead | Solo UI | Sin endpoint API dedicado |
| LLM en Workers | No cableado | IA operativa = reglas + ML, no chat |
| Churned (CustomerStatus) | Sin transición automática única | Usar analítica y CS |

## Apéndice D — Seguridad operativa (Capítulo 14 extendido)

| Tema | Implementación verificada |
|------|---------------------------|
| Autenticación | Cookie en UI; JWT en API (`/Account/Login`) |
| MFA | Configurable en `/Settings` |
| Autorización UI comercial | `CommercialWriteAuthorizationMiddleware` — Admin/Manager/Sales escriben |
| Validación tenant API | `ApiTenantValidationMiddleware` |
| Límites de plan | `PlanLimitMiddleware` |
| Auditoría | `/Audit` — event store de dominio |
| Trazabilidad | Cada comando dispara `DomainEvent` persistido |

**Buenas prácticas Sales:** contraseña única, cerrar sesión en equipos compartidos, no intentar acceder a `/Users` o `/Settings`, reportar accesos anómalos al Admin.

## Apéndice E — Trazabilidad de evidencia y glosario

| Afirmación | Evidencia en repositorio |
|------------|-------------------------|
| 66 páginas Razor | `01_SYSTEM_INVENTORY.md` |
| 19 ítems menú | `04_MENU_MAP.md` |
| 5 roles | `03_ROLE_MATRIX.md` |
| Qualify → automatización | `02_BUSINESS_FLOWS.md` |
| Worker 15 min | `05_AUTOMATION_CATALOG.md` |
| 150 FAQ | `08_FAQ.md` |

**Control de versiones:** v1.0.0 (2026-06-05) — commit base `f8131e8+`.

---

*Fin del Manual de Usuario Empresarial AutonomusCRM v1.0.0*

*Documento generado conforme al inventario verificado del repositorio. Para actualizaciones del producto, consulte la versión del commit referenciado en `01_SYSTEM_INVENTORY.md`.*
