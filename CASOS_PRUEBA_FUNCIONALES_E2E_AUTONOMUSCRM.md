# Casos de prueba funcionales E2E — AutonomusCRM

| Campo | Valor |
|-------|-------|
| **Modo** | Solo diseño de casos (sin ejecución, sin cambios código/BD) |
| **Fuente** | `ANALISIS_OPERACIONAL_REAL_CRM.md` |
| **Empresa simulada** | TechNova Solutions (Panamá, Costa Rica, Colombia) |
| **URL base** | `http://localhost:5154` |
| **Tenant** | TechNova / demo existente en BD |
| **Roles** | Admin, Manager, Sales, Support, Viewer |
| **Datos objetivo** | 100 Leads, 55 Customers, 28 Deals, 6 Workflows, 4 Policies, 14 usuarios |

**Convenciones**

- `Estado`: Pendiente (hasta ejecución humana en browser).
- `Resultado obtenido`: *(pendiente ejecución)*.
- `Tipo`: Funcional | Negativo | RBAC | Integración | E2E compuesto.
- Password demo: `{Rol}123!` (ej. `Sales123!`).

---

## Plantilla de caso (referencia)

Cada caso incluye: ID, Nombre, Módulo, Prioridad, Tipo, Precondiciones, Datos requeridos, Pasos detallados, Resultado esperado, Resultado obtenido, Estado, Riesgo, Evidencia requerida.

---

# SECCIÓN 1 — AUTENTICACIÓN (Auth)

---

**ID:** AUTH-001  
**Nombre:** Login exitoso Admin TechNova  
**Módulo:** Auth  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** API en marcha; usuario Admin TechNova activo; TenantId conocido.  

**Datos requeridos:** `admin@autonomuscrm.local` / `Admin123!`; TenantId TechNova.  

**Pasos detallados:**
1. Abrir `/Account/Login`.
2. Verificar Tenant ID precargado.
3. Ingresar email y contraseña Admin.
4. Clic **Entrar**.

**Resultado esperado:** Redirect a `/` (Dashboard); cookie de sesión; sidebar visible.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto — bloquea todos los flujos.  

**Evidencia requerida:** Captura URL post-login; cookie en DevTools.  

---

**ID:** AUTH-002  
**Nombre:** Login exitoso Manager (gerente PA)  
**Módulo:** Auth  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Usuario Manager activo en tenant TechNova.  

**Datos requeridos:** `manager@autonomuscrm.local` / `Manager123!`.  

**Pasos detallados:**
1. `/Account/Login`.
2. Completar TenantId, email Manager, contraseña.
3. **Entrar**.

**Resultado esperado:** Dashboard cargado; acceso posterior a `/Users` y `/Settings` permitido.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura dashboard + navegación a `/Users` sin AccessDenied.  

---

**ID:** AUTH-003  
**Nombre:** Login exitoso Sales (vendedor TechNova)  
**Módulo:** Auth  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Usuario Sales activo.  

**Datos requeridos:** `sales@autonomuscrm.local` / `Sales123!`.  

**Pasos detallados:**
1. Login con credenciales Sales.
2. Confirmar redirect a Dashboard.

**Resultado esperado:** Sesión Sales válida; `/Leads` accesible.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura sesión Sales en `/Leads`.  

---

**ID:** AUTH-004  
**Nombre:** Login exitoso Support  
**Módulo:** Auth  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Usuario Support activo.  

**Datos requeridos:** `support@autonomuscrm.local` / `Support123!`.  

**Pasos detallados:**
1. Login Support.
2. Navegar a `/Support`.

**Resultado esperado:** Login OK; página Support con health checks visible.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura `/Support` autenticado.  

---

**ID:** AUTH-005  
**Nombre:** Login exitoso Viewer  
**Módulo:** Auth  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Usuario Viewer activo.  

**Datos requeridos:** `viewer@autonomuscrm.local` / `Viewer123!`.  

**Pasos detallados:**
1. Login Viewer.
2. Abrir `/Leads` en solo lectura esperada.

**Resultado esperado:** Sesión Viewer; listado leads visible.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura listado leads Viewer.  

---

**ID:** AUTH-006  
**Nombre:** Logout cierra sesión  
**Módulo:** Auth  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Usuario autenticado (cualquier rol).  

**Datos requeridos:** Sesión activa.  

**Pasos detallados:**
1. Clic **Cerrar sesión** en sidebar (POST `/Account/Logout`).
2. Intentar abrir `/Leads`.

**Resultado esperado:** Redirect a `/Account/Login`; `/Leads` no accesible sin login.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura login tras logout; redirect al acceder `/Leads`.  

---

**ID:** AUTH-007  
**Nombre:** Contraseña inválida  
**Módulo:** Auth  
**Prioridad:** P0  
**Tipo:** Negativo  

**Precondiciones:** Pantalla login.  

**Datos requeridos:** Email válido; contraseña incorrecta.  

**Pasos detallados:**
1. Ingresar email Admin y contraseña errónea.
2. **Entrar**.

**Resultado esperado:** Permanece en login; mensaje de error visible (`role="alert"` o equivalente).  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura mensaje error en login.  

---

**ID:** AUTH-008  
**Nombre:** Acceso sin autenticación a módulo protegido  
**Módulo:** Auth  
**Prioridad:** P0  
**Tipo:** Negativo  

**Precondiciones:** Sin cookie de sesión (ventana incógnito).  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. Navegar directo a `/Leads`.
2. Navegar a `/Users`.

**Resultado esperado:** Redirect a `/Account/Login` en ambos.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura URL final Login.  

---

**ID:** AUTH-009  
**Nombre:** MFA requerido — mensaje UI  
**Módulo:** Auth  
**Prioridad:** P2  
**Tipo:** Negativo  

**Precondiciones:** Usuario con MFA habilitado (si existe en datos TechNova).  

**Datos requeridos:** Usuario MFA + API `/api/auth/verify-mfa`.  

**Pasos detallados:**
1. Login UI con usuario MFA.
2. Observar mensaje en página.

**Resultado esperado:** Mensaje indica MFA requerido y uso de API verify-mfa; no cookie completa.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo (flujo principal sin MFA en demo)  

**Evidencia requerida:** Captura mensaje MFA en login.  

---

**ID:** AUTH-010  
**Nombre:** Sesión expirada (cookie 8h)  
**Módulo:** Auth  
**Prioridad:** P2  
**Tipo:** Negativo  

**Precondiciones:** Documentación: cookie `ExpireTimeSpan` 8h.  

**Datos requeridos:** Sesión expirada o cookie eliminada manualmente.  

**Pasos detallados:**
1. Autenticarse.
2. Eliminar cookie `.AspNetCore.Cookies` en DevTools.
3. Refrescar `/Deals`.

**Resultado esperado:** Redirect a login.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura redirect login tras borrar cookie.  

---

**ID:** AUTH-011  
**Nombre:** Rate limiting API (200 req/min)  
**Módulo:** Auth  
**Prioridad:** P3  
**Tipo:** Negativo  

**Precondiciones:** API REST disponible.  

**Datos requeridos:** Script o herramienta >200 requests/min al mismo endpoint autenticado.  

**Pasos detallados:**
1. Ejecutar ráfaga contra endpoint API protegido.
2. Observar código HTTP.

**Resultado esperado:** Respuesta `429 Too Many Requests` según configuración global.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Log o captura status 429.  

---

# SECCIÓN 2 — DASHBOARD

---

**ID:** DASH-001  
**Nombre:** Visualización KPIs Dashboard TechNova  
**Módulo:** Dashboard  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** 100 leads y 28 deals cargados; login Admin.  

**Datos requeridos:** Dataset TechNova completo.  

**Pasos detallados:**
1. Login Admin.
2. Abrir `/` (Dashboard).
3. Revisar cards: leads 24h, conversión, deals en riesgo, revenue estimado, pipeline por etapa.

**Resultado esperado:** Todas las métricas visibles sin error; valores numéricos presentes.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura dashboard completo.  

---

**ID:** DASH-002  
**Nombre:** Validar conteos leads/deals vs listas  
**Módulo:** Dashboard  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Datos TechNova en BD.  

**Datos requeridos:** Total leads=100; total deals=28 (objetivo).  

**Pasos detallados:**
1. Anotar `TotalLeads` y `TotalDeals` en Dashboard.
2. Abrir `/Leads` y contar/registrar total listado.
3. Abrir `/Deals` y comparar.

**Resultado esperado:** Conteos dashboard coherentes con listados (mismo tenant resuelto).  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio — `GetDefaultTenantIdAsync` usa primer tenant.  

**Evidencia requerida:** Captura KPIs + totales en tablas.  

---

**ID:** DASH-003  
**Nombre:** Validar revenue estimado pipeline  
**Módulo:** Dashboard  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Deals Open con montos en TechNova.  

**Datos requeridos:** Suma conocida de deals Open.  

**Pasos detallados:**
1. Calcular manualmente suma `Amount` deals `Status=Open`.
2. Comparar con `EstimatedRevenue` en Dashboard.

**Resultado esperado:** Revenue estimado = suma deals abiertos del tenant.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Hoja cálculo + captura dashboard.  

---

**ID:** DASH-004  
**Nombre:** Dashboard tras crear lead nuevo  
**Módulo:** Dashboard  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Login Sales; anotar `NewLeadsLast24h` inicial.  

**Datos requeridos:** Lead nuevo "TechNova CR - Empresa X".  

**Pasos detallados:**
1. Crear lead en `/Leads`.
2. Volver a `/`.
3. Verificar incremento en leads 24h y total.

**Resultado esperado:** Métricas actualizadas tras creación.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Capturas antes/después dashboard.  

---

**ID:** DASH-005  
**Nombre:** Dashboard tras cerrar deal Won  
**Módulo:** Dashboard  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Deal Open en etapa Negotiation.  

**Datos requeridos:** Deal TechNova USD 45,000.  

**Pasos detallados:**
1. Cerrar deal Won en `/Deals/Details/{id}`.
2. Refrescar Dashboard.
3. Verificar pipeline y revenue.

**Resultado esperado:** Deal sale de Open; pipeline/revenue reflejan cierre.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Capturas dashboard antes/después.  

---

# SECCIÓN 3 — LEADS

---

**ID:** LEAD-001  
**Nombre:** Crear lead TechNova Panamá  
**Módulo:** Leads  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Login Sales; tenant TechNova.  

**Datos requeridos:** Nombre "Caribbean Logistics PA"; Email `contacto@caribbean.pa`; Fuente Website.  

**Pasos detallados:**
1. `/Leads` → crear lead (formulario lista o `/Leads/Create`).
2. Completar nombre, email, teléfono +507, empresa, fuente Website.
3. Guardar.

**Resultado esperado:** Redirect o `?created=True`; lead visible en tabla; evento `LeadCreatedEvent` en Audit.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura lista + URL; filtro Audit `LeadCreated`.  

---

**ID:** LEAD-002  
**Nombre:** Editar lead existente  
**Módulo:** Leads  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Lead TechNova con status New.  

**Datos requeridos:** Lead ID válido; nuevos teléfono y empresa.  

**Pasos detallados:**
1. `/Leads/Edit/{id}`.
2. Modificar phone y company.
3. Guardar POST.

**Resultado esperado:** Datos actualizados en detalle/lista; `LeadUpdatedEvent` en Audit.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura edición antes/después.  

---

**ID:** LEAD-003  
**Nombre:** Eliminar lead de prueba  
**Módulo:** Leads  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Lead descartable creado para prueba.  

**Datos requeridos:** Lead ID temporal.  

**Pasos detallados:**
1. `/Leads/Details/{id}`.
2. Acción eliminar → confirmar.
3. Verificar lista `/Leads`.

**Resultado esperado:** Lead no aparece en listado.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura lista sin registro.  

---

**ID:** LEAD-004  
**Nombre:** Calificar lead  
**Módulo:** Leads  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Lead status New o Contacted.  

**Datos requeridos:** Lead "TechNova Referral CO".  

**Pasos detallados:**
1. `/Leads/Details/{id}`.
2. Clic **Calificar** (POST `OnPostQualifyAsync`).
3. Confirmar si aplica.

**Resultado esperado:** Status Qualified; `QualifiedAt` poblado; `LeadQualifiedEvent` en Audit.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura detalle status + Audit.  

---

**ID:** LEAD-005  
**Nombre:** Asignar lead a vendedor Sales  
**Módulo:** Leads  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** 5 usuarios Sales en TechNova; lead sin asignar.  

**Datos requeridos:** `AssignedToUserId` de vendedor CR.  

**Pasos detallados:**
1. Editar lead o flujo asignación disponible en UI.
2. Asignar a usuario Sales Colombia.
3. Guardar.

**Resultado esperado:** `AssignedToUserId` guardado; `LeadAssignedEvent` en Audit.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio — validar si UI expone asignación.  

**Evidencia requerida:** Captura detalle con responsable.  

---

**ID:** LEAD-006  
**Nombre:** Convertir lead a cliente  
**Módulo:** Leads  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Lead calificado con email corporativo.  

**Datos requeridos:** Lead qualified TechNova.  

**Pasos detallados:**
1. `/Leads/Details/{id}`.
2. **Convertir a Cliente** → confirmar.
3. Esperar redirect.

**Resultado esperado:** Redirect `/Customers/Details/{customerId}`; lead status Converted; `LeadConvertedToCustomerEvent` + `CustomerCreatedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto — depende EventBus (InMemory/RabbitMQ).  

**Evidencia requerida:** Captura ficha cliente + Audit.  

---

**ID:** LEAD-007  
**Nombre:** Import leads CSV  
**Módulo:** Leads  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Login Manager; archivo CSV válido (20 filas PA/CR/CO).  

**Datos requeridos:** CSV columnas: name, email, phone, company, source.  

**Pasos detallados:**
1. `/Leads/Import`.
2. Subir CSV TechNova batch-20.
3. Enviar formulario.

**Resultado esperado:** Mensaje éxito `?imported=N`; leads visibles en `/Leads`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura mensaje import + conteo lista.  

---

**ID:** LEAD-008  
**Nombre:** Import leads JSON  
**Módulo:** Leads  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Archivo JSON válido según formato import página.  

**Datos requeridos:** JSON 5 leads TechNova.  

**Pasos detallados:**
1. `/Leads/Import`.
2. Subir JSON.
3. Confirmar importación.

**Resultado esperado:** Leads importados sin error.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura resultado import.  

---

**ID:** LEAD-009  
**Nombre:** Bulk update status leads  
**Módulo:** Leads  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** ≥10 leads seleccionables; login Manager.  

**Datos requeridos:** IDs leads; acción bulk cambio a Contacted.  

**Pasos detallados:**
1. Seleccionar leads en `/Leads`.
2. Enviar a `/Leads/BulkActions` con acción y status.
3. Verificar `?bulkUpdated=N`.

**Resultado esperado:** Status actualizado masivamente; eventos `LeadStatusChangedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura bulk result + muestra registros.  

---

**ID:** LEAD-010  
**Nombre:** Buscar lead por nombre/email  
**Módulo:** Leads  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** 100 leads TechNova cargados.  

**Datos requeridos:** Término "Caribbean" o email conocido.  

**Pasos detallados:**
1. `/Leads?search=Caribbean`.
2. Verificar resultados filtrados.

**Resultado esperado:** Solo leads coincidentes en tabla.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura URL y tabla filtrada.  

---

**ID:** LEAD-011  
**Nombre:** Filtrar lead por status y source  
**Módulo:** Leads  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Mix fuentes Website/Referral en datos.  

**Datos requeridos:** `?status=Qualified&source=Referral`.  

**Pasos detallados:**
1. Aplicar filtros en `/Leads`.
2. Validar filas mostradas.

**Resultado esperado:** Tabla coherente con filtros querystring.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura filtros activos.  

---

**ID:** LEAD-012  
**Nombre:** Lead inexistente (GUID inválido)  
**Módulo:** Leads  
**Prioridad:** P0  
**Tipo:** Negativo  

**Precondiciones:** Login Admin.  

**Datos requeridos:** GUID random `00000000-0000-0000-0000-000000000099`.  

**Pasos detallados:**
1. Navegar `/Leads/Details/{guid-invalido}`.

**Resultado esperado:** HTTP 404 o página error amigable con enlace a `/Leads`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio — análisis indica posible body vacío.  

**Evidencia requerida:** Captura pantalla + código red HTTP.  

---

**ID:** LEAD-013  
**Nombre:** Crear lead sin nombre (dato inválido)  
**Módulo:** Leads  
**Prioridad:** P0  
**Tipo:** Negativo  

**Precondiciones:** Formulario crear lead abierto.  

**Datos requeridos:** Nombre vacío.  

**Pasos detallados:**
1. Intentar guardar sin nombre.
2. Observar validación HTML5 o error servidor.

**Resultado esperado:** No se crea registro; mensaje validación visible.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura validación.  

---

**ID:** LEAD-014  
**Nombre:** Import CSV corrupto leads  
**Módulo:** Leads  
**Prioridad:** P1  
**Tipo:** Negativo  

**Precondiciones:** CSV malformado.  

**Datos requeridos:** Archivo .csv inválido.  

**Pasos detallados:**
1. `/Leads/Import` subir CSV corrupto.

**Resultado esperado:** Error visible; BD sin registros parciales corruptos.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura error + conteo leads sin cambio.  

---

**ID:** LEAD-015  
**Nombre:** Duplicados email en import  
**Módulo:** Leads  
**Prioridad:** P2  
**Tipo:** Negativo  

**Precondiciones:** Lead existente `alpha@technova.client`.  

**Datos requeridos:** CSV con mismo email duplicado.  

**Pasos detallados:**
1. Importar CSV con email ya existente.

**Resultado esperado:** Comportamiento documentado (rechazo o skip); sin duplicar inconsistente.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura resultado import + query email.  

---

# SECCIÓN 4 — CUSTOMERS

---

**ID:** CUST-001  
**Nombre:** Crear cliente TechNova manual  
**Módulo:** Customers  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Login Manager.  

**Datos requeridos:** "Andes Retail CO"; email; teléfono; empresa.  

**Pasos detallados:**
1. `/Customers/Create` o POST desde `/Customers`.
2. Guardar cliente.

**Resultado esperado:** Cliente en lista; `CustomerCreatedEvent` en Audit.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura ficha + Audit.  

---

**ID:** CUST-002  
**Nombre:** Editar cliente  
**Módulo:** Customers  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Cliente TechNova existente.  

**Datos requeridos:** Customer ID; nuevos datos contacto.  

**Pasos detallados:**
1. `/Customers/Edit/{id}`.
2. Actualizar email y company.
3. Guardar.

**Resultado esperado:** Cambios persistidos; `CustomerUpdatedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura edición.  

---

**ID:** CUST-003  
**Nombre:** Eliminar cliente  
**Módulo:** Customers  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Cliente de prueba sin deals críticos.  

**Datos requeridos:** Customer ID temporal.  

**Pasos detallados:**
1. `/Customers/Details/{id}` → Eliminar → confirmar.

**Resultado esperado:** Cliente removido del listado.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura lista post-delete.  

---

**ID:** CUST-004  
**Nombre:** Contactar cliente (RecordContact)  
**Módulo:** Customers  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Cliente activo TechNova.  

**Datos requeridos:** Customer ID.  

**Pasos detallados:**
1. `/Customers/Details/{id}`.
2. Clic **Contactar** (POST `OnPostRecordContactAsync`).

**Resultado esperado:** `LastContactAt` actualizado en UI/detalle.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura fecha último contacto.  

---

**ID:** CUST-005  
**Nombre:** Actualizar estado cliente a VIP  
**Módulo:** Customers  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Cliente status Customer.  

**Datos requeridos:** Customer ID; status VIP.  

**Pasos detallados:**
1. `/Customers/Edit/{id}`.
2. Cambiar status a VIP.
3. Guardar.

**Resultado esperado:** Badge/status VIP; `CustomerStatusChangedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura detalle status.  

---

**ID:** CUST-006  
**Nombre:** Import customers CSV  
**Módulo:** Customers  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** CSV 15 clientes TechNova.  

**Datos requeridos:** `/Customers/Import` o modal Importar en lista.  

**Pasos detallados:**
1. Subir CSV válido.
2. Confirmar import.

**Resultado esperado:** `?imported=N`; clientes visibles (total hacia 55).  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura import + conteo.  

---

**ID:** CUST-007  
**Nombre:** Bulk update status customers  
**Módulo:** Customers  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Selección múltiple en `/Customers`.  

**Datos requeridos:** IDs; acción bulk Inactive o Qualified.  

**Pasos detallados:**
1. POST `/Customers/BulkActions`.

**Resultado esperado:** `?bulkUpdated=N`; statuses coherentes.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura resultado bulk.  

---

**ID:** CUST-008  
**Nombre:** Crear deal desde cliente  
**Módulo:** Customers  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Cliente con relación comercial activa.  

**Datos requeridos:** Título "TechNova Impl Q3"; monto 85000.  

**Pasos detallados:**
1. `/Customers/Details/{id}`.
2. Modal crear deal → completar título y monto.
3. Guardar.

**Resultado esperado:** Deal creado; redirect o link a `/Deals/Details/{id}`; `DealCreatedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura deal + Audit.  

---

# SECCIÓN 5 — DEALS

---

**ID:** DEAL-001  
**Nombre:** Crear deal desde /Deals  
**Módulo:** Deals  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Cliente TechNova existente.  

**Datos requeridos:** customerId; título; amount > 0.  

**Pasos detallados:**
1. `/Deals` → crear deal.
2. Seleccionar cliente, título, monto USD 25000.
3. Guardar.

**Resultado esperado:** Deal en pipeline Prospecting; `DealCreatedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura pipeline + Audit.  

---

**ID:** DEAL-002  
**Nombre:** Editar deal (título, monto, fecha)  
**Módulo:** Deals  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Deal Open existente.  

**Datos requeridos:** Deal ID.  

**Pasos detallados:**
1. `/Deals/Edit/{id}`.
2. Cambiar título, amount, expectedCloseDate.
3. Guardar.

**Resultado esperado:** Cambios guardados; `DealUpdatedEvent` / `DealAmountUpdatedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura edición.  

---

**ID:** DEAL-003  
**Nombre:** Actualizar etapa a Proposal  
**Módulo:** Deals  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Deal en Qualification.  

**Datos requeridos:** Deal ID.  

**Pasos detallados:**
1. `/Deals/Details/{id}`.
2. POST `OnPostUpdateStageAsync` → stage Proposal.

**Resultado esperado:** Stage Proposal; probabilidad auto según reglas; `DealStageChangedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura detalle etapa.  

---

**ID:** DEAL-004  
**Nombre:** Actualizar probabilidad manual  
**Módulo:** Deals  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Deal Open.  

**Datos requeridos:** Probabilidad 65.  

**Pasos detallados:**
1. `/Deals/Details/{id}`.
2. Actualizar probabilidad a 65%.
3. Guardar.

**Resultado esperado:** Probability=65; `DealProbabilityUpdatedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura probabilidad.  

---

**ID:** DEAL-005  
**Nombre:** Cerrar deal Won  
**Módulo:** Deals  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Deal Negotiation; login Sales.  

**Datos requeridos:** Monto final opcional.  

**Pasos detallados:**
1. `/Deals/Details/{id}` → **Cerrar deal**.
2. Confirmar monto final.

**Resultado esperado:** Status Closed; Stage ClosedWon; `DealClosedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura deal cerrado + Audit.  

---

**ID:** DEAL-006  
**Nombre:** Cerrar deal Lost  
**Módulo:** Deals  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Deal Open descartable.  

**Datos requeridos:** Razón pérdida en metadata si UI lo permite.  

**Pasos detallados:**
1. Marcar deal como perdido (dominio `Lose()` si expuesto en UI).

**Resultado esperado:** Stage ClosedLost; `DealLostEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio — validar exposición UI Lose.  

**Evidencia requerida:** Captura status Lost.  

---

**ID:** DEAL-007  
**Nombre:** Eliminar deal  
**Módulo:** Deals  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Deal de prueba.  

**Datos requeridos:** Deal ID temporal.  

**Pasos detallados:**
1. `/Deals/Details/{id}` → Eliminar.

**Resultado esperado:** Deal no listado.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura lista.  

---

**ID:** DEAL-008  
**Nombre:** Bulk update stage deals  
**Módulo:** Deals  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Varios deals seleccionados.  

**Datos requeridos:** dealIds; stage Negotiation.  

**Pasos detallados:**
1. POST `/Deals/BulkActions`.

**Resultado esperado:** `?bulkUpdated=N`; etapa actualizada.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura bulk result.  

---

**ID:** DEAL-009  
**Nombre:** Import deals CSV  
**Módulo:** Deals  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** CSV deals válido (customerId, title, amount).  

**Datos requeridos:** Archivo import TechNova.  

**Pasos detallados:**
1. `/Deals/Import` subir CSV.

**Resultado esperado:** Deals importados hacia total 28.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura import.  

---

**ID:** DEAL-010  
**Nombre:** Monto inválido (≤ 0)  
**Módulo:** Deals  
**Prioridad:** P0  
**Tipo:** Negativo  

**Precondiciones:** Formulario crear deal.  

**Datos requeridos:** amount = 0 o negativo.  

**Pasos detallados:**
1. Intentar crear deal monto 0.

**Resultado esperado:** Error dominio/UI; deal no creado.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura mensaje error.  

---

**ID:** DEAL-011  
**Nombre:** Filtrar deals por etapa en pipeline  
**Módulo:** Deals  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** 28 deals distribuidos por etapa.  

**Datos requeridos:** `?stage=Proposal`.  

**Pasos detallados:**
1. `/Deals?stage=Proposal`.

**Resultado esperado:** Solo deals en etapa Proposal.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura filtro pipeline.  

---

# SECCIÓN 6 — USERS

---

**ID:** USER-001  
**Nombre:** Crear usuario vendedor TechNova  
**Módulo:** Users  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Login Admin o Manager.  

**Datos requeridos:** email `vendedor.cr@technova.local`; password; nombre/apellido.  

**Pasos detallados:**
1. `/Users/Create`.
2. Completar formulario POST.
3. Guardar.

**Resultado esperado:** Usuario en `/Users`; `UserCreatedEvent` en Audit.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura lista usuarios.  

---

**ID:** USER-002  
**Nombre:** Editar usuario existente  
**Módulo:** Users  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Usuario editable.  

**Datos requeridos:** User ID.  

**Pasos detallados:**
1. `/Users/Edit/{id}`.
2. Cambiar firstName/lastName.
3. Guardar POST.

**Resultado esperado:** Datos actualizados; `UserUpdatedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura edición.  

---

**ID:** USER-003  
**Nombre:** Desactivar usuario  
**Módulo:** Users  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Usuario activo de prueba.  

**Datos requeridos:** User ID.  

**Pasos detallados:**
1. `/Users/Edit/{id}` → Toggle status inactivo (`OnPostToggleStatusAsync`).

**Resultado esperado:** `IsActive=false`; login posterior falla; `UserDeactivatedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura status + intento login fallido.  

---

**ID:** USER-004  
**Nombre:** Activar usuario  
**Módulo:** Users  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Usuario inactivo.  

**Datos requeridos:** User ID.  

**Pasos detallados:**
1. Toggle activar en `/Users/Edit/{id}`.

**Resultado esperado:** `IsActive=true`; `UserActivatedEvent`; login OK.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura + login exitoso.  

---

**ID:** USER-005  
**Nombre:** Asignar rol Sales a usuario  
**Módulo:** Users  
**Prioridad:** P0  
**Tipo:** Funcional  

**Precondiciones:** Usuario sin rol Sales.  

**Datos requeridos:** User ID; rol Sales.  

**Pasos detallados:**
1. `/Users/Edit/{id}` → POST `OnPostAssignRoleAsync` rol Sales.

**Resultado esperado:** Rol Sales en lista roles usuario; `UserRoleAddedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura roles usuario.  

---

**ID:** USER-006  
**Nombre:** Eliminar rol de usuario  
**Módulo:** Users  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Usuario con rol Viewer asignable.  

**Datos requeridos:** User ID; rol Viewer.  

**Pasos detallados:**
1. POST `OnPostRemoveRoleAsync` quitar Viewer.

**Resultado esperado:** Rol removido; `UserRoleRemovedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura roles.  

---

**ID:** USER-007  
**Nombre:** MFA habilitar vía API  
**Módulo:** Users  
**Prioridad:** P2  
**Tipo:** Integración  

**Precondiciones:** JWT desde `/api/auth/login`.  

**Datos requeridos:** Endpoint MFA según `EnableMfaCommand`.  

**Pasos detallados:**
1. Autenticar API.
2. Invocar flujo enable MFA.
3. Login UI verificar mensaje MFA.

**Resultado esperado:** `MfaEnabled=true`; login UI requiere verify-mfa.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Respuesta API + captura login MFA.  

---

**ID:** USER-008  
**Nombre:** Import users CSV  
**Módulo:** Users  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** CSV usuarios válido.  

**Datos requeridos:** `/Users/Import`.  

**Pasos detallados:**
1. Subir CSV con 3 usuarios Support.

**Resultado esperado:** `?imported=N`; usuarios listados.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura import.  

---

**ID:** USER-009  
**Nombre:** Página Roles — distribución por rol  
**Módulo:** Users  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** 14 usuarios TechNova.  

**Datos requeridos:** Login Admin.  

**Pasos detallados:**
1. `/Users/Roles`.
2. Verificar conteos Admin/Manager/Sales/Viewer/Support.

**Resultado esperado:** Conteos `RoleCounts` coherentes con BD (no tabla decorativa de `/Users`).  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura `/Users/Roles`.  

---

**ID:** USER-010  
**Nombre:** Bulk desactivar usuarios  
**Módulo:** Users  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Selección múltiple usuarios prueba.  

**Datos requeridos:** userIds.  

**Pasos detallados:**
1. POST `/Users/BulkActions` acción desactivar.

**Resultado esperado:** Usuarios inactivos masivamente.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura bulk result.  

---

# SECCIÓN 7 — WORKFLOWS

---

**ID:** WF-001  
**Nombre:** Crear workflow LeadCreated TechNova  
**Módulo:** Workflows  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Login Manager.  

**Datos requeridos:** Nombre "TN-AutoScore Lead"; descripción.  

**Pasos detallados:**
1. `/Workflows/Create`.
2. POST nombre y descripción.

**Resultado esperado:** Redirect `/Workflows`; workflow en lista (hacia 6 total).  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura lista workflows.  

---

**ID:** WF-002  
**Nombre:** Editar workflow (nombre, activo)  
**Módulo:** Workflows  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Workflow existente.  

**Datos requeridos:** Workflow ID.  

**Pasos detallados:**
1. `/Workflows/Edit/{id}`.
2. Cambiar nombre; toggle IsActive.
3. Guardar.

**Resultado esperado:** Cambios persistidos.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura edición.  

---

**ID:** WF-003  
**Nombre:** Agregar trigger DomainEvent LeadCreatedEvent  
**Módulo:** Workflows  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Workflow activo.  

**Datos requeridos:** type=DomainEvent; eventType=`LeadCreatedEvent`.  

**Pasos detallados:**
1. En Edit → POST `OnPostAddTriggerAsync`.

**Resultado esperado:** Trigger guardado en workflow; match al crear lead.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio — motor ejecuta logging principalmente.  

**Evidencia requerida:** Captura triggers + log ejecución.  

---

**ID:** WF-004  
**Nombre:** Agregar condition BusinessRule  
**Módulo:** Workflows  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Workflow con trigger.  

**Datos requeridos:** type BusinessRule; expression texto.  

**Pasos detallados:**
1. POST `OnPostAddConditionAsync`.

**Resultado esperado:** Condición almacenada (evaluación TODO = siempre true en motor).  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura condiciones.  

---

**ID:** WF-005  
**Nombre:** Agregar action UpdateStatus  
**Módulo:** Workflows  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Workflow con trigger.  

**Datos requeridos:** type UpdateStatus; target definido.  

**Pasos detallados:**
1. POST `OnPostAddActionAsync`.

**Resultado esperado:** Acción registrada; log en ejecución workflow.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura acciones.  

---

**ID:** WF-006  
**Nombre:** Duplicar workflow  
**Módulo:** Workflows  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Workflow origen.  

**Datos requeridos:** Workflow ID.  

**Pasos detallados:**
1. `/Workflows/Edit/{id}` → Duplicar (`OnPostDuplicateAsync`).

**Resultado esperado:** Segundo workflow con copia configuración.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura dos workflows.  

---

**ID:** WF-007  
**Nombre:** Eliminar workflow  
**Módulo:** Workflows  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Workflow descartable.  

**Datos requeridos:** Workflow ID.  

**Pasos detallados:**
1. POST `OnPostDeleteAsync` en Edit.

**Resultado esperado:** Workflow no listado.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura lista.  

---

**ID:** WF-008  
**Nombre:** Import workflows JSON  
**Módulo:** Workflows  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** JSON export válido.  

**Datos requeridos:** `/Workflows/Import`.  

**Pasos detallados:**
1. Subir JSON workflows.

**Resultado esperado:** `?imported=N`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura import.  

---

**ID:** WF-009  
**Nombre:** Ejecución workflow al crear lead  
**Módulo:** Workflows  
**Prioridad:** P1  
**Tipo:** Integración  

**Precondiciones:** Workflow activo trigger LeadCreatedEvent; API+EventBus.  

**Datos requeridos:** Lead nuevo.  

**Pasos detallados:**
1. Crear lead.
2. Revisar logs / `ExecutionCount` en workflow Edit.

**Resultado esperado:** Workflow `RecordExecution` incrementado; log "Workflow executed".  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura execution count + log Serilog.  

---

# SECCIÓN 8 — POLICIES

---

**ID:** POL-001  
**Nombre:** Crear política TechNova  
**Módulo:** Policies  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Login Admin.  

**Datos requeridos:** Nombre "TN-Deal-100k"; expression válida; descripción.  

**Pasos detallados:**
1. `/Policies/Create`.
2. POST crear (hacia 4 policies).

**Resultado esperado:** Redirect `/Policies`; política listada.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura lista policies.  

---

**ID:** POL-002  
**Nombre:** Editar política  
**Módulo:** Policies  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Policy ID.  

**Datos requeridos:** Nueva expression.  

**Pasos detallados:**
1. `/Policies/Edit/{id}` → guardar cambios.

**Resultado esperado:** Política actualizada.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura edición.  

---

**ID:** POL-003  
**Nombre:** Duplicar política  
**Módulo:** Policies  
**Prioridad:** P3  
**Tipo:** Funcional  

**Precondiciones:** Política origen.  

**Datos requeridos:** Policy ID.  

**Pasos detallados:**
1. POST `OnPostDuplicateAsync`.

**Resultado esperado:** Copia en lista.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura lista.  

---

**ID:** POL-004  
**Nombre:** Eliminar política  
**Módulo:** Policies  
**Prioridad:** P3  
**Tipo:** Funcional  

**Precondiciones:** Política de prueba.  

**Datos requeridos:** Policy ID.  

**Pasos detallados:**
1. POST delete en Edit.

**Resultado esperado:** Política removida.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura lista.  

---

**ID:** POL-005  
**Nombre:** Import policies JSON  
**Módulo:** Policies  
**Prioridad:** P3  
**Tipo:** Funcional  

**Precondiciones:** Archivo JSON policies.  

**Datos requeridos:** `/Policies/Import`.  

**Pasos detallados:**
1. Subir JSON.

**Resultado esperado:** Import exitoso.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura import.  

---

**ID:** POL-006  
**Nombre:** Evaluar política tras evento deal  
**Módulo:** Policies  
**Prioridad:** P2  
**Tipo:** Integración  

**Precondiciones:** Política activa; PolicyEngine conectado a dispatcher.  

**Datos requeridos:** Deal > 100000 USD.  

**Pasos detallados:**
1. Crear/actualizar deal alto valor.
2. Revisar logs PolicyEngine / violaciones si expuestas.

**Resultado esperado:** Evaluación registrada en logs o resultado policy (según implementación).  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Log policy evaluation.  

---

# SECCIÓN 9 — AUDIT

---

**ID:** AUD-001  
**Nombre:** Filtrar eventos por tipo LeadCreated  
**Módulo:** Audit  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Eventos en Event Store tras cargar TechNova.  

**Datos requeridos:** `?eventType=LeadCreated`.  

**Pasos detallados:**
1. `/Audit?eventType=LeadCreatedEvent` (o nombre exacto UI).
2. Verificar listado.

**Resultado esperado:** Solo eventos lead creados.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura filtro audit.  

---

**ID:** AUD-002  
**Nombre:** Filtrar eventos por rango fechas  
**Módulo:** Audit  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Eventos últimos 7 días.  

**Datos requeridos:** from/to en query.  

**Pasos detallados:**
1. `/Audit?from=...&to=...`.

**Resultado esperado:** Eventos dentro del rango.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura filtros fecha.  

---

**ID:** AUD-003  
**Nombre:** Export auditoría JSON  
**Módulo:** Audit  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Login Manager.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. `/Audit` → POST Export (`OnPostExportAsync`).
2. Descargar archivo.

**Resultado esperado:** Archivo JSON descargado con eventos.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Archivo exportado.  

---

**ID:** AUD-004  
**Nombre:** Verificar eventos tras flujo lead→cliente→deal  
**Módulo:** Audit  
**Prioridad:** P0  
**Tipo:** Integración  

**Precondiciones:** Flujo LEAD-006 + DEAL-001 ejecutado.  

**Datos requeridos:** CorrelationId o timestamps.  

**Pasos detallados:**
1. Buscar en Audit: LeadCreated, LeadQualified, LeadConverted, CustomerCreated, DealCreated.

**Resultado esperado:** Cadena de eventos presente en orden lógico.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura Audit con 5+ eventos.  

---

# SECCIÓN 10 — SETTINGS

---

**ID:** SET-001  
**Nombre:** Editar datos tenant TechNova  
**Módulo:** Settings  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Login Admin.  

**Datos requeridos:** name "TechNova Solutions"; region; timezone America/Panama.  

**Pasos detallados:**
1. `/Settings`.
2. POST `OnPostUpdateTenantAsync`.

**Resultado esperado:** Nombre tenant actualizado; `TenantUpdatedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura Settings guardado.  

---

**ID:** SET-002  
**Nombre:** Actualizar settings JSON sistema  
**Módulo:** Settings  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** JSON settings válido.  

**Datos requeridos:** settingsJson.  

**Pasos detallados:**
1. POST `OnPostUpdateSettingsAsync`.

**Resultado esperado:** Settings persistidos en tenant.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura JSON guardado.  

---

**ID:** SET-003  
**Nombre:** Exportar configuración tenant  
**Módulo:** Settings  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Login Manager.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. POST `OnPostExportConfigAsync`.
2. Descargar archivo.

**Resultado esperado:** Archivo config exportado.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Archivo export.  

---

**ID:** SET-004  
**Nombre:** Importar configuración tenant  
**Módulo:** Settings  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Archivo export previo.  

**Datos requeridos:** IFormFile config.  

**Pasos detallados:**
1. POST `OnPostImportConfigAsync`.

**Resultado esperado:** Config restaurada sin error.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura import OK.  

---

**ID:** SET-005  
**Nombre:** Restore defaults configuración  
**Módulo:** Settings  
**Prioridad:** P3  
**Tipo:** Funcional  

**Precondiciones:** Login Admin.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. POST `OnPostRestoreDefaultsAsync`.

**Resultado esperado:** Valores por defecto aplicados.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura antes/después.  

---

# SECCIÓN 11 — SUPPORT

---

**ID:** SUP-001  
**Nombre:** Health Database Healthy  
**Módulo:** Support  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** PostgreSQL activo; login Support.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. `/Support`.
2. Leer `DatabaseStatus`.

**Resultado esperado:** Estado `Healthy`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura Support DB verde/Healthy.  

---

**ID:** SUP-002  
**Nombre:** Health EventBus Healthy  
**Módulo:** Support  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** EventBus InMemory o RabbitMQ up.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. Verificar `EventBusStatus` en `/Support`.

**Resultado esperado:** `Healthy`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura EventBus status.  

---

**ID:** SUP-003  
**Nombre:** Health Cache Healthy  
**Módulo:** Support  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Redis o MemoryCache configurado.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. Verificar `CacheStatus`.

**Resultado esperado:** `Healthy`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura Cache status.  

---

**ID:** SUP-004  
**Nombre:** Servicios caídos — PostgreSQL detenido  
**Módulo:** Support  
**Prioridad:** P2  
**Tipo:** Negativo  

**Precondiciones:** Simular BD caída (entorno controlado).  

**Datos requeridos:** Postgres stopped.  

**Pasos detallados:**
1. Detener PostgreSQL.
2. Refrescar `/Support`.

**Resultado esperado:** `DatabaseStatus` Unhealthy o error visible.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto (entorno)  

**Evidencia requerida:** Captura Unhealthy.  

---

# SECCIÓN 12 — AGENTES IA

---

**ID:** AGT-001  
**Nombre:** Ver listado agentes en UI  
**Módulo:** Agents  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Login Admin.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. `/Agents`.
2. Verificar 7 agentes listados (LeadIntelligence, CustomerRisk, DealStrategy, Communication, etc.).

**Resultado esperado:** Tarjetas agentes visibles con descripción y eventos suscritos.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Bajo  

**Evidencia requerida:** Captura `/Agents`.  

---

**ID:** AGT-002  
**Nombre:** Editar configuración agente  
**Módulo:** Agents  
**Prioridad:** P2  
**Tipo:** Funcional  

**Precondiciones:** Tenant TechNova.  

**Datos requeridos:** agentName LeadIntelligenceAgent; configJson válido.  

**Pasos detallados:**
1. POST `OnPostUpdateAgentConfigAsync` desde `/Agents`.

**Resultado esperado:** Config guardada por tenant; mensaje éxito.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura config guardada.  

---

**ID:** AGT-003  
**Nombre:** Evento LeadCreated dispara agente (Worker activo)  
**Módulo:** Agents  
**Prioridad:** P2  
**Tipo:** Integración  

**Precondiciones:** `AutonomusCRM.Workers` ejecutándose; mismo EventBus que API.  

**Datos requeridos:** Lead nuevo.  

**Pasos detallados:**
1. Crear lead.
2. Revisar logs Worker LeadIntelligenceAgent.

**Resultado esperado:** Log "processing LeadCreatedEvent"; score lead actualizado 0-100.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Log Worker + lead score en UI.  

---

**ID:** AGT-004  
**Nombre:** Worker inactivo — score no automático  
**Módulo:** Agents  
**Prioridad:** P2  
**Tipo:** Negativo  

**Precondiciones:** Solo API sin Worker.  

**Datos requeridos:** Lead nuevo.  

**Pasos detallados:**
1. Crear lead sin Worker.
2. Verificar score solo manual o null.

**Resultado esperado:** Sin actualización automática por agente; evento sí en Audit.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura lead sin score auto.  

---

**ID:** AGT-005  
**Nombre:** DealStrategy tras DealStageChanged  
**Módulo:** Agents  
**Prioridad:** P3  
**Tipo:** Integración  

**Precondiciones:** Worker activo.  

**Datos requeridos:** Deal con cambio etapa.  

**Pasos detallados:**
1. Cambiar etapa deal.
2. Revisar logs DealStrategyAgent.

**Resultado esperado:** Log procesamiento `DealStageChangedEvent`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Log Worker.  

---

# SECCIÓN 13 — RBAC

**Matriz esperada (según análisis — solo Users/Settings restringen Admin,Manager):**

| Ruta | Admin | Manager | Sales | Support | Viewer |
|------|-------|---------|-------|---------|--------|
| `/Users` | Permitido | Permitido | Denegado | Denegado | Denegado |
| `/Settings` | Permitido | Permitido | Denegado | Denegado | Denegado |
| `/Leads` | Permitido | Permitido | Permitido | Permitido | Permitido* |
| `/Customers` | Permitido | Permitido | Permitido | Permitido | Permitido* |
| `/Deals` | Permitido | Permitido | Permitido | Permitido | Permitido* |
| `/Workflows` | Permitido | Permitido | Permitido | Permitido | Permitido* |
| `/Policies` | Permitido | Permitido | Permitido | Permitido | Permitido* |
| `/Audit` | Permitido | Permitido | Permitido | Permitido | Permitido* |
| `/Support` | Permitido | Permitido | Permitido | Permitido | Permitido |
| `/Agents` | Permitido | Permitido | Permitido | Permitido | Permitido* |

\*Viewer: acceso lectura esperado por negocio; código actual permite POST (gap).

---

**ID:** RBAC-001  
**Nombre:** Admin accede Users y Settings  
**Módulo:** RBAC  
**Prioridad:** P0  
**Tipo:** RBAC  

**Precondiciones:** Login Admin.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. Navegar `/Users` → HTTP 200, tabla usuarios.
2. Navegar `/Settings` → HTTP 200.

**Resultado esperado:** Acceso permitido sin AccessDenied.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Capturas Users + Settings.  

---

**ID:** RBAC-002  
**Nombre:** Manager accede Users y Settings  
**Módulo:** RBAC  
**Prioridad:** P0  
**Tipo:** RBAC  

**Precondiciones:** Login Manager.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. `/Users` y `/Settings`.

**Resultado esperado:** Acceso permitido.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Capturas.  

---

**ID:** RBAC-003  
**Nombre:** Sales denegado en Users  
**Módulo:** RBAC  
**Prioridad:** P0  
**Tipo:** RBAC  

**Precondiciones:** Login Sales.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. Navegar `/Users`.

**Resultado esperado:** Redirect `/Account/AccessDenied`.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura AccessDenied.  

---

**ID:** RBAC-004  
**Nombre:** Sales denegado en Settings  
**Módulo:** RBAC  
**Prioridad:** P0  
**Tipo:** RBAC  

**Precondiciones:** Login Sales.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. Navegar `/Settings` (incl. manipulación URL directa).

**Resultado esperado:** AccessDenied.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura URL Settings + AccessDenied.  

---

**ID:** RBAC-005  
**Nombre:** Support denegado en Users  
**Módulo:** RBAC  
**Prioridad:** P0  
**Tipo:** RBAC  

**Precondiciones:** Login Support.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. `/Users`.

**Resultado esperado:** AccessDenied.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura.  

---

**ID:** RBAC-006  
**Nombre:** Viewer denegado en Users  
**Módulo:** RBAC  
**Prioridad:** P0  
**Tipo:** RBAC  

**Precondiciones:** Login Viewer.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. `/Users`.

**Resultado esperado:** AccessDenied.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura.  

---

**ID:** RBAC-007  
**Nombre:** Sales accede Leads y Deals  
**Módulo:** RBAC  
**Prioridad:** P0  
**Tipo:** RBAC  

**Precondiciones:** Login Sales.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. `/Leads`, `/Deals`.

**Resultado esperado:** HTTP 200; listados visibles.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Capturas.  

---

**ID:** RBAC-008  
**Nombre:** Viewer POST editar lead (gap permisos)  
**Módulo:** RBAC  
**Prioridad:** P0  
**Tipo:** RBAC  

**Precondiciones:** Login Viewer; lead ID conocido.  

**Datos requeridos:** Lead ID.  

**Pasos detallados:**
1. Abrir `/Leads/Edit/{id}`.
2. Cambiar nombre y guardar POST.

**Resultado esperado (negocio):** Denegado o solo lectura.  
**Resultado esperado (código actual):** Puede permitir guardado — documentar como defecto si ocurre.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Crítico  

**Evidencia requerida:** Captura edición exitosa o bloqueo.  

---

**ID:** RBAC-009  
**Nombre:** Viewer acceso Audit y Workflows  
**Módulo:** RBAC  
**Prioridad:** P1  
**Tipo:** RBAC  

**Precondiciones:** Login Viewer.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. `/Audit`, `/Workflows`.

**Resultado esperado:** Definir política negocio; hoy autenticado = acceso.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Capturas.  

---

**ID:** RBAC-010  
**Nombre:** Manipulación URL Users como Sales  
**Módulo:** RBAC  
**Prioridad:** P0  
**Tipo:** RBAC  

**Precondiciones:** Sesión Sales.  

**Datos requeridos:** URL `/Users/Create`.  

**Pasos detallados:**
1. Pegar URL directa en barra direcciones.

**Resultado esperado:** AccessDenied (no formulario crear).  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura.  

---

# SECCIÓN 14 — MULTITENANCY

---

**ID:** MT-001  
**Nombre:** Login usuario tenant B con TenantId incorrecto  
**Módulo:** Multitenancy  
**Prioridad:** P1  
**Tipo:** Negativo  

**Precondiciones:** 2 tenants en BD (TechNova + otro).  

**Datos requeridos:** Usuario solo en tenant B; TenantId de tenant A en login.  

**Pasos detallados:**
1. Login con email válido pero TenantId ajeno.

**Resultado esperado:** Login falla o sin acceso a datos.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Crítico  

**Evidencia requerida:** Captura error login.  

---

**ID:** MT-002  
**Nombre:** UI muestra datos primer tenant (GetDefaultTenantId)  
**Módulo:** Multitenancy  
**Prioridad:** P1  
**Tipo:** Negativo  

**Precondiciones:** Usuario autenticado tenant B; primer tenant en BD es A.  

**Datos requeridos:** Lead exclusivo tenant A.  

**Pasos detallados:**
1. Login tenant B.
2. Abrir `/Leads` y buscar lead exclusivo tenant A.

**Resultado esperado (negocio):** No visible.  
**Riesgo análisis:** Puede mostrarse si resolver usa primer tenant.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Crítico  

**Evidencia requerida:** Captura listado + IDs tenant.  

---

**ID:** MT-003  
**Nombre:** Creación registro no cruza tenant en dominio  
**Módulo:** Multitenancy  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Login con TenantId correcto TechNova.  

**Datos requeridos:** Lead creado con tenantId claim.  

**Pasos detallados:**
1. Crear lead autenticado TechNova.
2. Verificar en BD `TenantId` del registro = TechNova.

**Resultado esperado:** TenantId coherente con login.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Query SQL o Audit tenantId.  

---

**ID:** MT-004  
**Nombre:** Eventos Audit aislados por tenant  
**Módulo:** Multitenancy  
**Prioridad:** P1  
**Tipo:** Funcional  

**Precondiciones:** Eventos en 2 tenants.  

**Datos requeridos:** Filtro tenant en Audit si existe.  

**Pasos detallados:**
1. Generar evento en tenant TechNova.
2. `/Audit` filtrar — no eventos tenant B.

**Resultado esperado:** Event Store filtra por tenant del contexto.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Captura Audit filtrado.  

---

# SECCIÓN 15 — CONCURRENCIA

---

**ID:** CONC-001  
**Nombre:** Dos Sales editan mismo Lead simultáneamente  
**Módulo:** Concurrencia  
**Prioridad:** P2  
**Tipo:** Negativo  

**Precondiciones:** 2 browsers/sesiones Sales.  

**Datos requeridos:** Mismo Lead ID.  

**Pasos detallados:**
1. Sesión A abre Edit lead, cambia phone.
2. Sesión B abre Edit, cambia company, guarda primero.
3. Sesión A guarda después.

**Resultado esperado:** Último guardado gana o conflicto documentado (sin RowVersion).  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Capturas ambas sesiones + valor final.  

---

**ID:** CONC-002  
**Nombre:** Dos usuarios editan mismo Deal  
**Módulo:** Concurrencia  
**Prioridad:** P2  
**Tipo:** Negativo  

**Precondiciones:** 2 sesiones Manager/Sales.  

**Datos requeridos:** Deal ID.  

**Pasos detallados:**
1. A cambia probabilidad a 40%.
2. B cambia etapa a Negotiation.
3. Guardar en secuencia rápida.

**Resultado esperado:** Estado final predecible; sin corrupción.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Captura deal final.  

---

**ID:** CONC-003  
**Nombre:** Bulk simultáneo leads  
**Módulo:** Concurrencia  
**Prioridad:** P3  
**Tipo:** Negativo  

**Precondiciones:** 2 sesiones Manager.  

**Datos requeridos:** Overlap lead IDs en bulk.  

**Pasos detallados:**
1. Ejecutar bulk status Contacted y Qualified en paralelo sobre mismos IDs.

**Resultado esperado:** Sin error 500; estado final uno de los dos.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Logs + estado final muestra.  

---

# SECCIÓN 16 — FLUJOS E2E REALES

---

**ID:** E2E-001  
**Nombre:** Escenario vendedor TechNova — día completo  
**Módulo:** E2E compuesto  
**Prioridad:** P0  
**Tipo:** E2E compuesto  

**Precondiciones:** Datos base TechNova; EventBus operativo; login Sales.  

**Datos requeridos:** Credenciales Sales; lead nuevo CR.  

**Pasos detallados:**
1. AUTH-003 Login Sales.
2. DASH-001 Revisar dashboard.
3. LEAD-001 Crear lead Costa Rica.
4. LEAD-004 Calificar lead.
5. LEAD-006 Convertir a cliente.
6. CUST-008 Crear deal desde cliente.
7. DEAL-003 Actualizar etapa Proposal.
8. DEAL-004 Probabilidad 65%.
9. DEAL-005 Cerrar Won.
10. AUD-004 Verificar cadena eventos Audit.
11. AUTH-006 Logout.

**Resultado esperado:** Flujo comercial completo sin error; eventos en Audit; métricas dashboard actualizadas.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Crítico  

**Evidencia requerida:** Screenshots por paso + export Audit.  

---

**ID:** E2E-002  
**Nombre:** Escenario gerente TechNova — operación  
**Módulo:** E2E compuesto  
**Prioridad:** P0  
**Tipo:** E2E compuesto  

**Precondiciones:** Login Manager; CSV leads preparado.  

**Datos requeridos:** 20 leads CSV; usuario nuevo.  

**Pasos detallados:**
1. AUTH-002 Login Manager.
2. USER-001 Crear vendedor.
3. USER-005 Asignar rol Sales.
4. LEAD-007 Import CSV 20 leads.
5. CUST-007 Bulk update customers.
6. WF-001 Crear workflow LeadCreated.
7. POL-001 Crear política.
8. SET-001 Actualizar timezone tenant.
9. AUD-003 Export auditoría JSON.
10. AUTH-006 Logout.

**Resultado esperado:** Operaciones administrativas y comerciales OK.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Alto  

**Evidencia requerida:** Paquete evidencias gerente.  

---

**ID:** E2E-003  
**Nombre:** Escenario soporte TechNova — salud sistema  
**Módulo:** E2E compuesto  
**Prioridad:** P1  
**Tipo:** E2E compuesto  

**Precondiciones:** Login Support.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. AUTH-004 Login Support.
2. SUP-001 a SUP-003 Health checks.
3. RBAC-005 Users denegado.
4. LEAD-010 Buscar lead (lectura).
5. AUD-001 Filtrar LeadCreated últimas 24h.
6. AUTH-006 Logout.

**Resultado esperado:** Health OK; acceso acorde rol; auditoría legible.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Capturas Support + Audit.  

---

**ID:** E2E-004  
**Nombre:** Escenario Admin — gobierno y agentes  
**Módulo:** E2E compuesto  
**Prioridad:** P1  
**Tipo:** E2E compuesto  

**Precondiciones:** Worker opcional; login Admin.  

**Datos requeridos:** Ninguno.  

**Pasos detallados:**
1. AUTH-001 Login Admin.
2. AGT-001 Ver agentes.
3. AGT-002 Editar config LeadIntelligence.
4. USER-009 `/Users/Roles`.
5. SET-003 Export config → SET-004 Import.
6. USER-003 Desactivar usuario prueba.
7. SUP-001 Health OK.
8. AUTH-006 Logout.

**Resultado esperado:** Gobierno tenant y agentes sin error.  

**Resultado obtenido:** *(pendiente ejecución)*  

**Estado:** Pendiente  

**Riesgo:** Medio  

**Evidencia requerida:** Capturas Admin.  

---

# RESUMEN EJECUTIVO

## Total casos creados

| Sección | IDs | Cantidad |
|---------|-----|----------|
| 1 Auth | AUTH-001…011 | 11 |
| 2 Dashboard | DASH-001…005 | 5 |
| 3 Leads | LEAD-001…015 | 15 |
| 4 Customers | CUST-001…008 | 8 |
| 5 Deals | DEAL-001…011 | 11 |
| 6 Users | USER-001…010 | 10 |
| 7 Workflows | WF-001…009 | 9 |
| 8 Policies | POL-001…006 | 6 |
| 9 Audit | AUD-001…004 | 4 |
| 10 Settings | SET-001…005 | 5 |
| 11 Support | SUP-001…004 | 4 |
| 12 Agents | AGT-001…005 | 5 |
| 13 RBAC | RBAC-001…010 | 10 |
| 14 Multitenancy | MT-001…004 | 4 |
| 15 Concurrencia | CONC-001…003 | 3 |
| 16 E2E compuestos | E2E-001…004 | 4 |
| **TOTAL** | | **114** |

## Casos por prioridad

| Prioridad | Cantidad | % |
|-----------|----------|---|
| **P0** | 38 | 33% |
| **P1** | 42 | 37% |
| **P2** | 28 | 25% |
| **P3** | 6 | 5% |

## Cobertura por módulo (casos diseñados / flujos del análisis)

| Módulo | Casos | Cobertura diseño |
|--------|-------|------------------|
| Auth | 11 | ~100% flujos auth documentados |
| Dashboard | 5 | ~100% KPIs Index |
| Leads | 15 | ~95% acciones UI |
| Customers | 8 | ~90% |
| Deals | 11 | ~95% |
| Users | 10 | ~90% |
| Workflows | 9 | ~90% |
| Policies | 6 | ~85% |
| Audit | 4 | ~80% |
| Settings | 5 | ~100% |
| Support | 4 | ~100% health |
| Agents | 5 | ~80% |
| RBAC | 10 | Matriz crítica cubierta |
| Multitenancy | 4 | Riesgos análisis cubiertos |
| Concurrencia | 3 | Escenarios análisis |
| E2E compuestos | 4 | Escenarios A/B/C/D análisis |

**Cobertura global estimada del mapa funcional:** **~92%** de acciones UI documentadas en `ANALISIS_OPERACIONAL_REAL_CRM.md`.

**Cobertura ejecución:** **0%** (todos Pendiente).

---

## Veredicto (diseño — no ejecución)

| Ítem | Valor |
|------|-------|
| **Veredicto batería** | **GO** — lista para ejecución humana |
| **Veredicto producto** | **CONDICIONAL** — ejecutar P0 RBAC-008, MT-002, LEAD-012 antes de certificar |

## Riesgos principales

1. **RBAC incompleto** — Viewer/Support pueden escribir en Leads/Deals (RBAC-008).
2. **Multitenant UI** — `GetDefaultTenantIdAsync` puede mezclar datos (MT-002).
3. **EventBus/Worker** — convert lead y agentes dependen de infraestructura (LEAD-006, AGT-003).
4. **Workflows/Policies** — ejecución real limitada a logs (WF-009, POL-006).
5. **404 sin vista** — LEAD-012 puede fallar UX.

## Recomendaciones

1. **Preparar datos TechNova** antes de ejecutar: 100/55/28/6/4/14 entidades.
2. **Orden ejecución:** P0 Auth → RBAC → Leads/Customers/Deals → E2E-001 → P1 resto → P2/P3.
3. **Entorno:** API `localhost:5154`, PostgreSQL `autonomuscrm`, `EventBus: InMemory`, Worker opcional para sección Agents.
4. **Evidencia:** 1 captura + URL por caso P0; export Audit al cierre de cada E2E compuesto.
5. **No confiar** en tabla decorativa permisos en `/Users` — usar matriz RBAC de este documento.

## Prioridad de ejecución sugerida

```text
Fase 1 (P0): AUTH-001…008, RBAC-001…010, LEAD-001,004,006,012,013, DEAL-001,005,010,
             CUST-001,008, USER-001,003,005, AUD-004, E2E-001, E2E-002

Fase 2 (P1): Dashboard, imports, bulk, workflows triggers, Audit export, Settings,
             MT-*, E2E-003, E2E-004

Fase 3 (P2/P3): Agents Worker, concurrencia, policies avanzadas, rate limit
```

---

*Documento generado exclusivamente desde `ANALISIS_OPERACIONAL_REAL_CRM.md`. Sin ejecución de pruebas. Todos los casos en Estado: Pendiente.*


