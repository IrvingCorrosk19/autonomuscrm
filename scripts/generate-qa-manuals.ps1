# Genera manuales funcionales por rol (plantilla enterprise)
$OutDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'Documentation\QA'
$base = 'http://164.68.99.83:8091'
$pwd = 'AutonomusTest123!'

function Manual($file, $role, $email, $home, $intro, $canSee, $canDo, $cannot, $daily, $useCases, $errors, $faq) {
@"
# $role — Manual Funcional QA

**AutonomusCRM / AutonomusFlow** | Entorno: $base  
**Usuario QA:** ``$email`` | **Password:** ``$pwd``  
**Home tras login:** ``$home``

---

## 1. Introducción

### Responsabilidades
$intro

### Objetivos operativos
- Ejecutar el ciclo comercial/operativo asignado al rol **$role** sin escalación a desarrollo.
- Validar que cada ruta autorizada carga, persiste datos y respeta RBAC.
- Documentar desviaciones en ``ROLE_CERTIFICATION_MATRIX.md``.

### KPIs sugeridos (UAT)
| KPI | Meta UAT |
|-----|----------|
| Login exitoso día 1 | 100% |
| Rutas smoke del rol | 100% PASS |
| CRUD permitidos sin 500 | 100% |
| Acciones prohibidas → AccessDenied | 100% |

---

## 2. Menús y navegación

### Puede ver (rutas autenticadas)
$canSee

### Puede hacer (escritura)
$canDo

### No puede hacer (debe recibir AccessDenied o 403)
$cannot

> Sidebar: ``_FlowSidebar.cshtml`` — Workflows y FailedEvents existen pero **no están en menú**; acceso directo por URL.

---

## 3. Operación diaria

### Inicio de jornada
1. Login en ``/Account/Login`` (TenantId vacío).
2. Verificar redirect a ``$home``.
3. Revisar Command Center o dashboard home del rol.
4. Revisar tareas en ``/Tasks`` (si aplica).

### Actividades núcleo
$daily

### Cierre de jornada
1. Completar tareas abiertas asignadas.
2. Verificar deals/leads actualizados en pipeline.
3. Revisar ``/Audit`` si hubo cambios críticos (Admin/Manager).

---

## 4. Casos de uso reales (datos seed VPS)

$useCases

---

## 5. Errores comunes

$errors

---

## 6. FAQ

$faq

---

## 7. Casos de prueba vinculados

Ver ``$($file -replace '_Functional_Manual','')_TEST_CASES.md`` en esta carpeta.

"@ | Set-Content (Join-Path $OutDir $file) -Encoding UTF8
}

Manual 'SuperAdmin_Functional_Manual.md' 'SuperAdmin (Admin máximo)' 'superadmin@autonomuscrm.local' '/executive' @'
Cuenta principal de tenant con rol **Admin** en RBAC (no existe SuperAdmin en código). Responsable de gobierno del tenant: usuarios, políticas, workflows, trust, billing, integraciones y operaciones comerciales completas.
'@ @'
- Command: `/`, `/TrustInbox`, `/Agents`, `/command/decisions`
- Revenue: `/revenue`, `/executive`, `/Deals`
- CRM: `/Leads`, `/Customers`, `/Customer360`, `/customer-success`
- Inteligencia: `/Memory`
- Operación: `/Tasks`, `/VoiceCalls`, `/FailedEvents`
- Plataforma: `/Users`, `/Policies`, `/Workflows`, `/Audit`, `/Settings`, `/billing`, `/Integrations`
'@ @'
- CRUD Leads, Customers, Deals, Workflows, Policies
- Gestión usuarios: Create/Edit/Roles/Import/Bulk
- Settings: tenant, export/import config
- Trust: Approve/Reject/Rollback, threshold
- API: POST `/api/users` (RequireAdmin), provisioning con platform key
'@ @'
- Ninguna restricción RBAC dentro del tenant (salvo límites de plan billing)
'@ @'
1. Revisar Executive OS y métricas pipeline.
2. Gestionar usuarios y roles del equipo.
3. Aprobar decisiones Trust pendientes.
4. Configurar workflows y policies.
5. Revisar billing usage y audit trail.
'@ @'
**UC-SA-01 — Onboarding equipo:** Crear `ops@` → asignar Admin → verificar login `/executive`.

**UC-SA-02 — Pipeline review:** Abrir deal seed *CRM Enterprise Banco Regional* (`d1000001-...001`) → actualizar probabilidad → export executive.

**UC-SA-03 — Trust governance:** TrustInbox → aprobar audit seed → verificar en Audit.

**UC-SA-04 — Automatización:** Workflows/Edit → modal Add Trigger → LeadCreatedEvent → guardar.
'@ @'
| Error | Causa | Solución |
|-------|-------|----------|
| Login loop | TenantId incorrecto | Dejar vacío o `00000000-...` |
| 403 API users | Token sin rol Admin | Usar superadmin/admin |
| Workflows no en menú | No está en sidebar | URL directa `/Workflows` |
| Billing sin checkout | UI read-only | Usar API `POST /api/billing/checkout` |
'@ @'
**¿SuperAdmin vs Admin?** Mismo rol RBAC `Admin`; superadmin es cuenta de escalación QA.

**¿Puede crear tenants?** Solo vía `POST /api/provisioning/tenants` con platform key, no desde UI.

Manual 'Admin_Functional_Manual.md' 'Admin' 'admin@autonomuscrm.local' '/executive' @'
Administrador operativo del tenant. Paridad funcional con SuperAdmin en UI; diferencia práctica: cuenta de operaciones diarias vs cuenta de escalación QA.
'@ @'
Mismas rutas que SuperAdmin (ver manual SuperAdmin sección 2).
'@ @'
Mismo alcance escritura comercial + Users/Settings + Trust + Workflows/Policies.
'@ @'
POST `/api/provisioning/tenants` sin platform key (401).
'@ @'
1. Login → Executive.
2. Operaciones CRM y administración de usuarios.
3. Revisar Revenue y Customer Success.
'@ @'
**UC-ADM-01:** Crear lead → convertir customer → deal → Close Won (flujo completo).

**UC-ADM-02:** Import CSV leads vía modal en `/Leads`.

**UC-ADM-03:** Settings → ExportConfig → backup JSON tenant.
'@ @'
| Error | Causa | Solución |
|-------|-------|----------|
| Users 403 | Rol Sales/Support | Usar admin@ |
| Import falla | CSV mal formado | Ver plantilla Import |
'@ @'
**¿Admin puede todo?** Sí en UI tenant; APIs platform requieren keys adicionales.

Manual 'Manager_Functional_Manual.md' 'Manager' 'manager@autonomuscrm.local' '/executive' @'
Gestión comercial y de equipo. Supervisa pipeline, asigna usuarios Sales, configura workflows/policies, aprueba trust. No tiene `RequireAdmin` en API users.
'@ @'
Executive, Revenue, CRM completo lectura, Users/Settings (Admin+Manager), Trust, Audit, Tasks, C360.
'@ @'
CRUD comercial, crear usuarios Sales, workflows, policies, export executive.
'@ @'
POST `/api/users` → 403. Provisioning tenants → 401.
'@ @'
1. Executive OS — revisión pipeline semanal.
2. Crear/asignar leads y deals del equipo.
3. Revisar tasks completadas (seed task revisión pipeline).
'@ @'
**UC-MGR-01:** Crear `sales3@` (si plan permite) → rol Sales → verificar `/revenue`.

**UC-MGR-02:** Deal *Automatizacion Logistica* → mover stage → task seguimiento.

**UC-MGR-03:** Workflow *Tarea seguimiento deal* → verificar activo.
'@ @'
| Error | Causa | Solución |
|-------|-------|----------|
| Límite usuarios | Plan free/starter | SQL bump plan o billing |
| API 403 users | RequireAdmin | Pedir Admin |
'@ @'
**¿Manager = Admin?** Casi paridad UI; API admin-only bloqueada.

Manual 'Sales_Functional_Manual.md' 'Sales' 'sales1@autonomuscrm.local' '/revenue' @'
Ejecutivo comercial. Gestiona leads, clientes y oportunidades. Home Revenue OS. Sin acceso Users/Settings.
'@ @'
`/revenue`, `/Leads`, `/Customers`, `/Deals`, `/Tasks`, `/Customer360` (lectura), Trust (lectura/acción), Command `/`.
'@ @'
Create/Edit Leads, Customers, Deals, Workflows/Policies (commercial write middleware).
'@ @'
`/Users`, `/Settings`, POST `/api/users`, GET `/Leads/Create` como Support (N/A — Sales sí puede).
'@ @'
1. Revenue OS — prioridades del día.
2. Trabajar leads asignados (seed Lead Web Fintech).
3. Avanzar deals en pipeline.
4. Completar tasks de seguimiento.
'@ @'
**UC-SALES-01:** Crear lead → calificar → convertir → crear deal → Close Won.

**UC-SALES-02:** Deal Details → modal Update Stage → Negociación.

**UC-SALES-03:** FlowActions desde insight Revenue → crear task.
'@ @'
| Error | Causa | Solución |
|-------|-------|----------|
| AccessDenied Create | Rol Support/Viewer | Verificar cuenta |
| No ve Users | RBAC correcto | Escalar a Manager |
'@ @'
**¿Sales ve deals de otro sales?** Sí, visibilidad tenant-wide.

Manual 'Support_Functional_Manual.md' 'Support' 'support@autonomuscrm.local' '/Customer360' @'
Atención al cliente y éxito del cliente. Home Customer360. Lectura comercial; escritura en Customer Success y tasks CS.
'@ @'
`/Customer360`, `/customer-success`, `/Leads` `/Customers` `/Deals` lectura, `/Tasks`, Trust lectura, Command `/`.
'@ @'
CreateTicket, CloseTicket, RunPlaybook en Customer Success. Complete tasks CS.
'@ @'
Commercial write: `/Leads/Create`, `/Deals/Create`, `/Workflows/Edit`, `/Policies/Create` → AccessDenied.
'@ @'
1. Customer360 — cola clientes prioritarios.
2. Tickets CS abiertos (seed incidencia API).
3. Cerrar tickets resueltos.
'@ @'
**UC-SUP-01:** Buscar *Banco Regional PA* → abrir 360 → revisar health.

**UC-SUP-02:** Crear ticket onboarding Logística → asignar → cerrar.

**UC-SUP-03:** Verificar bloqueo al intentar crear lead.
'@ @'
| Error | Causa | Solución |
|-------|-------|----------|
| AccessDenied en Create | Middleware commercial | Comportamiento esperado |
| No puede Users | RBAC | Escalar Admin |
'@ @'
**¿Support aprueba Trust?** UI permite acciones; validar política interna.

Manual 'Viewer_Functional_Manual.md' 'Viewer' 'viewer@autonomuscrm.local' '/' @'
Consulta ejecutiva. Solo lectura en módulos comerciales y dashboards. Sin CRUD ni administración.
'@ @'
Todos los dashboards y listados CRM en modo lectura: `/`, `/executive`, `/revenue`, `/Leads`, `/Customers`, `/Deals`, `/Audit`, `/TrustInbox`, `/Memory`, `/billing`.
'@ @'
Navegación, búsqueda, filtros, export audit (si permitido), ver modales de detalle sin POST.
'@ @'
Cualquier Create/Edit/Delete comercial, Users, Settings, Workflows write.
'@ @'
1. Dashboard Command Center.
2. Revisar pipeline y métricas ejecutivas.
3. Consultar audit para reporting.
'@ @'
**UC-VWR-01:** Ver deal *Salud Integral — Cerrada Ganada* sin editar.

**UC-VWR-02:** Intentar `/Leads/Create` → confirmar AccessDenied.

**UC-VWR-03:** Executive export HTML (si botón visible).
'@ @'
| Error | Causa | Solución |
|-------|-------|----------|
| Puede editar | Bug RBAC | Reportar FAIL |
| Menú muestra Create | UI no oculta botón | Validar que POST falla |
'@'
**¿Viewer puede Trust approve?** UI no restringe por rol; validar si negocio lo permite.
'@

Write-Host 'Manuals generated.'
