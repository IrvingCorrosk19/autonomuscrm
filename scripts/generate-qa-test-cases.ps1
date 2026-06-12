# Genera casos de prueba QA por rol desde inventario funcional real
$ErrorActionPreference = 'Stop'
$OutDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'Documentation\QA'
$env = @{
    BaseUrl = 'http://164.68.99.83:8091'
    Password = 'AutonomusTest123!'
    TenantId = 'b1000000-0000-4000-8000-000000000001'
}

function Format-Tc($tc) {
    @"
### $($tc.Id)

| Campo | Valor |
|-------|-------|
| **Módulo** | $($tc.Module) |
| **Ruta** | ``$($tc.Route)`` |
| **Prioridad** | $($tc.Priority) |
| **Precondición** | $($tc.Pre) |
| **Datos** | $($tc.Data) |

**Pasos:**
$($tc.Steps)

**Resultado esperado:** $($tc.Expected)

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecución humana_ | ☐ PASS ☐ FAIL |

"@
}

$superadmin = @(
    @{ Id='TC-SA-001'; Module='Auth'; Route='/Account/Login'; Priority='P0'; Pre='Usuario no autenticado'; Data='superadmin@autonomuscrm.local / AutonomusTest123!'; Steps="1. Abrir login`n2. Dejar TenantId vacío`n3. Ingresar credenciales`n4. Submit"; Expected='Redirect a /executive, sesión activa, menú completo visible' }
    @{ Id='TC-SA-002'; Module='Executive OS'; Route='/executive'; Priority='P0'; Pre='Sesión SuperAdmin'; Data='—'; Steps="1. Navegar a /executive`n2. Verificar KPIs y empty state o datos seed"; Expected='HTTP 200, dashboard Executive renderiza sin 500' }
    @{ Id='TC-SA-003'; Module='Users'; Route='/Users'; Priority='P0'; Pre='Sesión SuperAdmin'; Data='—'; Steps="1. Abrir /Users`n2. Verificar lista 7 usuarios seed"; Expected='Tabla usuarios visible, roles mostrados' }
    @{ Id='TC-SA-004'; Module='Users'; Route='/Users/Create'; Priority='P0'; Pre='Sesión SuperAdmin'; Data='qa.new@techsolutions.pa / Test123!'; Steps="1. POST crear usuario vía formulario`n2. Verificar redirect /Users"; Expected='Usuario creado en listado' }
    @{ Id='TC-SA-005'; Module='Users'; Route='/Users/Edit/{id}'; Priority='P1'; Pre='Usuario existente'; Data='Asignar rol Manager'; Steps="1. Editar usuario`n2. AssignRole Manager`n3. Guardar"; Expected='Rol persistido en BD' }
    @{ Id='TC-SA-006'; Module='Settings'; Route='/Settings'; Priority='P0'; Pre='Sesión SuperAdmin'; Data='timezone America/Panama'; Steps="1. Abrir Settings`n2. Actualizar tenant settings`n3. Guardar"; Expected='POST exitoso, sin error' }
    @{ Id='TC-SA-007'; Module='Policies'; Route='/Policies/Create'; Priority='P1'; Pre='Sesión SuperAdmin'; Data='expression: role in (Admin, Manager)'; Steps="1. Crear policy`n2. Verificar en lista"; Expected='Policy activa en /Policies' }
    @{ Id='TC-SA-008'; Module='Workflows'; Route='/Workflows/Edit/{id}'; Priority='P1'; Pre='Workflow seed b1000004-...001'; Data='Trigger DomainEvent LeadCreatedEvent'; Steps="1. Abrir Edit`n2. Modal Add Trigger`n3. Guardar"; Expected='Trigger visible en workflow' }
    @{ Id='TC-SA-009'; Module='Trust Studio'; Route='/TrustInbox'; Priority='P0'; Pre='Audits seed si existen'; Data='—'; Steps="1. Abrir TrustInbox`n2. Seleccionar item`n3. Approve"; Expected='Acción registrada, cola actualizada' }
    @{ Id='TC-SA-010'; Module='Audit'; Route='/Audit'; Priority='P0'; Pre='Actividad previa'; Data='—'; Steps="1. Abrir Audit`n2. Filtrar por fecha`n3. Abrir modal detalle evento"; Expected='Eventos listados, modal JSON' }
    @{ Id='TC-SA-011'; Module='Billing'; Route='/billing'; Priority='P1'; Pre='Sesión SuperAdmin'; Data='—'; Steps="1. Abrir billing`n2. Ver plan starter y usage"; Expected='Dashboard usage visible (checkout UI no implementado en Razor)' }
    @{ Id='TC-SA-012'; Module='Integrations'; Route='/Integrations'; Priority='P1'; Pre='Sesión SuperAdmin'; Data='—'; Steps="1. Abrir Integrations`n2. Ver marketplace cards"; Expected='Página carga, OAuth requiere credenciales externas' }
    @{ Id='TC-SA-013'; Module='Memory'; Route='/Memory'; Priority='P1'; Pre='Sesión SuperAdmin'; Data='—'; Steps="1. Abrir Memory`n2. Ver timeline/dashboard"; Expected='HTTP 200, read-only dashboard' }
    @{ Id='TC-SA-014'; Module='Failed Events'; Route='/FailedEvents'; Priority='P2'; Pre='Sesión SuperAdmin'; Data='—'; Steps="1. Navegar /FailedEvents (no en sidebar)`n2. Ver cola"; Expected='Página ops carga' }
    @{ Id='TC-SA-015'; Module='API Users'; Route='POST /api/users'; Priority='P0'; Pre='JWT Admin'; Data='Bearer token'; Steps="1. Login API`n2. POST crear usuario"; Expected='201 Created (RequireAdmin policy)' }
    @{ Id='TC-SA-016'; Module='Provisioning'; Route='POST /api/provisioning/tenants'; Priority='P2'; Pre='X-Platform-Key'; Data='Provisioning API key'; Steps="1. POST nuevo tenant con platform key"; Expected='Tenant creado (ops plataforma)' }
    @{ Id='TC-SA-017'; Module='Leads'; Route='/Leads/Create'; Priority='P0'; Pre='Sesión SuperAdmin'; Data='Lead QA SA'; Steps="1. Crear lead`n2. Verificar en /Leads"; Expected='Lead en listado' }
    @{ Id='TC-SA-018'; Module='Deals'; Route='/Deals/Details/{id}'; Priority='P0'; Pre='Deal seed d1000001-...001'; Data='probability 80'; Steps="1. Abrir Details`n2. Modal Update Probability`n3. Guardar"; Expected='Probabilidad actualizada' }
    @{ Id='TC-SA-019'; Module='Customer360'; Route='/Customer360'; Priority='P0'; Pre='Sesión SuperAdmin'; Data='Banco Regional'; Steps="1. Buscar cliente`n2. Abrir detalle /customers/{id}/360"; Expected='Vista 360 enterprise' }
    @{ Id='TC-SA-020'; Module='Customer Success'; Route='/customer-success'; Priority='P1'; Pre='Sesión SuperAdmin'; Data='ticket CS'; Steps="1. Crear ticket`n2. Cerrar ticket"; Expected='Ticket en lista' }
)

$admin = @(
    @{ Id='TC-ADM-001'; Module='Auth'; Route='/Account/Login'; Priority='P0'; Pre='No autenticado'; Data='admin@autonomuscrm.local'; Steps="1. Login`n2. Verificar redirect"; Expected='/executive' }
    @{ Id='TC-ADM-002'; Module='Revenue OS'; Route='/revenue'; Priority='P0'; Pre='Sesión Admin'; Data='—'; Steps="1. Abrir Revenue`n2. Ver métricas pipeline"; Expected='Dashboard Revenue OS OK' }
    @{ Id='TC-ADM-003'; Module='Leads'; Route='/Leads'; Priority='P0'; Pre='10 leads seed'; Data='—'; Steps="1. Listar leads`n2. Buscar/filtrar`n3. Abrir drawer preview"; Expected='Tabla responsive, drawer funcional' }
    @{ Id='TC-ADM-004'; Module='Leads'; Route='/Leads/Import'; Priority='P1'; Pre='CSV válido'; Data='import modal'; Steps="1. Modal import`n2. Subir CSV`n3. Confirmar"; Expected='Leads importados' }
    @{ Id='TC-ADM-005'; Module='Customers'; Route='/Customers/Create'; Priority='P0'; Pre='Sesión Admin'; Data='Nuevo Cliente QA'; Steps="1. Crear customer`n2. Verificar listado"; Expected='Customer creado' }
    @{ Id='TC-ADM-006'; Module='Customers'; Route='/Customers/Details/{id}'; Priority='P0'; Pre='Customer seed'; Data='—'; Steps="1. Details`n2. Modal Create Deal`n3. Crear deal"; Expected='Deal vinculado' }
    @{ Id='TC-ADM-007'; Module='Deals'; Route='/Deals/BulkActions'; Priority='P1'; Pre='Deals seleccionados'; Data='bulk stage'; Steps="1. Seleccionar deals`n2. Bulk modal`n3. Aplicar"; Expected='Stage actualizado' }
    @{ Id='TC-ADM-008'; Module='Tasks'; Route='/Tasks'; Priority='P0'; Pre='8 tasks seed'; Data='—'; Steps="1. Listar tasks`n2. Complete task`n3. Assign task"; Expected='Estado actualizado' }
    @{ Id='TC-ADM-009'; Module='Workflows'; Route='/Workflows'; Priority='P0'; Pre='4 workflows seed'; Data='—'; Steps="1. Listar workflows`n2. Ver activos/inactivos"; Expected='Lista correcta' }
    @{ Id='TC-ADM-010'; Module='Policies'; Route='/Policies/Edit/{id}'; Priority='P1'; Pre='Policy seed'; Data='—'; Steps="1. Editar`n2. Duplicate`n3. Verificar copia"; Expected='Duplicado en lista' }
    @{ Id='TC-ADM-011'; Module='Users'; Route='/Users/Roles'; Priority='P1'; Pre='Sesión Admin'; Data='—'; Steps="1. Abrir /Users/Roles`n2. Ver conteos por rol"; Expected='Matriz roles visible' }
    @{ Id='TC-ADM-012'; Module='Settings'; Route='/Settings'; Priority='P0'; Pre='Sesión Admin'; Data='Export config'; Steps="1. ExportConfig`n2. Descargar JSON"; Expected='Archivo export generado' }
    @{ Id='TC-ADM-013'; Module='Command'; Route='/'; Priority='P0'; Pre='Sesión Admin'; Data='—'; Steps="1. Command Center`n2. Ctrl+K palette`n3. Buscar Leads"; Expected='Palette navega a ruta' }
    @{ Id='TC-ADM-014'; Module='Agents'; Route='/Agents'; Priority='P1'; Pre='Sesión Admin'; Data='—'; Steps="1. Abrir Workforce/Agents"; Expected='Vista agentes AI' }
    @{ Id='TC-ADM-015'; Module='Voice'; Route='/VoiceCalls'; Priority='P2'; Pre='Sesión Admin'; Data='GUID customer'; Steps="1. Log manual call"; Expected='Call registrada (MVP)' }
    @{ Id='TC-ADM-016'; Module='Localization'; Route='cualquier página'; Priority='P1'; Pre='Sesión Admin'; Data='es / es-PA'; Steps="1. Cambiar idioma selector`n2. Verificar labels"; Expected='UI traducida' }
    @{ Id='TC-ADM-017'; Module='Leads'; Route='/Leads/Details/{id}'; Priority='P0'; Pre='Lead seed f1000001-...009'; Data='—'; Steps="1. Qualify lead`n2. Convert to customer"; Expected='Customer creado desde lead' }
    @{ Id='TC-ADM-018'; Module='Deals'; Route='/Deals/Details/{id}'; Priority='P0'; Pre='Deal abierto'; Data='—'; Steps="1. Close Won modal`n2. Confirmar"; Expected='Deal status Won' }
    @{ Id='TC-ADM-019'; Module='Integrations'; Route='/Integrations'; Priority='P1'; Pre='Sin OAuth config'; Data='—'; Steps="1. Intentar Connect HubSpot"; Expected='Error controlado o redirect OAuth' }
    @{ Id='TC-ADM-020'; Module='Audit'; Route='/Audit'; Priority='P0'; Pre='Post CRUD'; Data='—'; Steps="1. Export audit CSV"; Expected='Export descargable' }
)

$manager = @(
    @{ Id='TC-MGR-001'; Module='Auth'; Route='/Account/Login'; Priority='P0'; Pre='—'; Data='manager@autonomuscrm.local'; Steps="1. Login"; Expected='/executive' }
    @{ Id='TC-MGR-002'; Module='Executive'; Route='/executive'; Priority='P0'; Pre='Sesión Manager'; Data='—'; Steps="1. Ver Executive OS`n2. Export executive HTML"; Expected='Export descarga archivo' }
    @{ Id='TC-MGR-003'; Module='Users'; Route='/Users/Create'; Priority='P0'; Pre='Sesión Manager'; Data='nuevo sales'; Steps="1. Crear usuario sales`n2. Asignar rol Sales"; Expected='Usuario operativo' }
    @{ Id='TC-MGR-004'; Module='Leads'; Route='/Leads/Create'; Priority='P0'; Pre='Sesión Manager'; Data='Lead Manager'; Steps="1. Crear lead"; Expected='Lead visible' }
    @{ Id='TC-MGR-005'; Module='Deals'; Route='/Deals/Create'; Priority='P0'; Pre='Customer seed'; Data='—'; Steps="1. Crear deal"; Expected='Deal en pipeline' }
    @{ Id='TC-MGR-006'; Module='Workflows'; Route='/Workflows/Create'; Priority='P1'; Pre='Sesión Manager'; Data='WF QA'; Steps="1. Crear workflow"; Expected='Workflow en lista' }
    @{ Id='TC-MGR-007'; Module='Policies'; Route='/Policies/Create'; Priority='P1'; Pre='Sesión Manager'; Data='—'; Steps="1. Crear policy"; Expected='Policy activa' }
    @{ Id='TC-MGR-008'; Module='Trust'; Route='/TrustInbox'; Priority='P0'; Pre='Pending audits'; Data='—'; Steps="1. Aprobar decisión"; Expected='Queue decrementa' }
    @{ Id='TC-MGR-009'; Module='Revenue'; Route='/revenue'; Priority='P0'; Pre='Sesión Manager'; Data='—'; Steps="1. Ver Revenue OS"; Expected='Dashboard OK' }
    @{ Id='TC-MGR-010'; Module='Tasks'; Route='/Tasks'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Revisar pipeline semanal task seed"; Expected='Task Completed visible' }
    @{ Id='TC-MGR-011'; Module='Customer360'; Route='/Customer360'; Priority='P0'; Pre='—'; Data='Logistica'; Steps="1. Buscar cliente`n2. Ver 360"; Expected='Detalle cliente' }
    @{ Id='TC-MGR-012'; Module='Settings'; Route='/Settings'; Priority='P0'; Pre='Sesión Manager'; Data='—'; Steps="1. Abrir Settings`n2. Actualizar región"; Expected='Acceso permitido Admin+Manager' }
    @{ Id='TC-MGR-013'; Module='API'; Route='POST /api/users'; Priority='P1'; Pre='JWT Manager'; Data='—'; Steps="1. Intentar crear user API"; Expected='403 Forbidden (RequireAdmin)' }
    @{ Id='TC-MGR-014'; Module='Leads'; Route='/Leads/BulkActions'; Priority='P1'; Pre='—'; Data='—'; Steps="1. Bulk assign"; Expected='Leads actualizados' }
    @{ Id='TC-MGR-015'; Module='Deals'; Route='/Deals/Edit/{id}'; Priority='P0'; Pre='Deal seed'; Data='—'; Steps="1. Editar deal"; Expected='Cambios guardados' }
)

$sales = @(
    @{ Id='TC-SALES-001'; Module='Leads'; Route='/Leads/Create'; Priority='P0'; Pre='sales1 autenticado'; Data='qa.lead.sales@techsolutions.pa'; Steps="1. Abrir Leads`n2. Crear Lead`n3. Guardar"; Expected='Lead creado, visible en listado, sin errores' }
    @{ Id='TC-SALES-002'; Module='Auth'; Route='/Account/Login'; Priority='P0'; Pre='—'; Data='sales1@autonomuscrm.local'; Steps="1. Login"; Expected='Redirect /revenue' }
    @{ Id='TC-SALES-003'; Module='Revenue'; Route='/revenue'; Priority='P0'; Pre='Sesión Sales'; Data='—'; Steps="1. Ver Revenue OS home"; Expected='Dashboard personal ventas' }
    @{ Id='TC-SALES-004'; Module='Leads'; Route='/Leads/Edit/{id}'; Priority='P0'; Pre='Lead propio'; Data='—'; Steps="1. Editar lead asignado"; Expected='Cambios guardados' }
    @{ Id='TC-SALES-005'; Module='Leads'; Route='/Leads/Details/{id}'; Priority='P0'; Pre='Lead calificado'; Data='—'; Steps="1. Qualify`n2. Convert to Customer"; Expected='Customer creado' }
    @{ Id='TC-SALES-006'; Module='Customers'; Route='/Customers/Create'; Priority='P0'; Pre='Sesión Sales'; Data='—'; Steps="1. Crear customer"; Expected='OK' }
    @{ Id='TC-SALES-007'; Module='Deals'; Route='/Deals/Create'; Priority='P0'; Pre='Customer existente'; Data='—'; Steps="1. Crear deal`n2. Asignar amount"; Expected='Deal en pipeline' }
    @{ Id='TC-SALES-008'; Module='Deals'; Route='/Deals/Details/{id}'; Priority='P0'; Pre='Deal abierto'; Data='—'; Steps="1. Update stage modal`n2. Mover a negociación"; Expected='Stage actualizado' }
    @{ Id='TC-SALES-009'; Module='Deals'; Route='/Deals/Details/{id}'; Priority='P0'; Pre='Deal listo'; Data='—'; Steps="1. Close Won"; Expected='Deal cerrado ganado' }
    @{ Id='TC-SALES-010'; Module='Users'; Route='/Users'; Priority='P0'; Pre='Sesión Sales'; Data='—'; Steps="1. Intentar /Users"; Expected='403 o AccessDenied' }
    @{ Id='TC-SALES-011'; Module='Settings'; Route='/Settings'; Priority='P1'; Pre='Sesión Sales'; Data='—'; Steps="1. Intentar /Settings"; Expected='403 Admin/Manager only' }
    @{ Id='TC-SALES-012'; Module='Workflows'; Route='/Workflows/Edit/{id}'; Priority='P1'; Pre='Sesión Sales'; Data='—'; Steps="1. Editar workflow"; Expected='Permitido (commercial write)' }
    @{ Id='TC-SALES-013'; Module='Tasks'; Route='/Tasks'; Priority='P0'; Pre='Task asignada sales1'; Data='—'; Steps="1. Completar task"; Expected='Status Completed' }
    @{ Id='TC-SALES-014'; Module='FlowActions'; Route='/FlowActions'; Priority='P1'; Pre='Insight CTA en Revenue'; Data='—'; Steps="1. Click Create Task desde insight"; Expected='Task creada' }
    @{ Id='TC-SALES-015'; Module='Leads'; Route='/Leads/Create'; Priority='P0'; Pre='sales2 autenticado'; Data='—'; Steps="1. sales2 crea lead`n2. sales1 ve en lista"; Expected='Visibilidad tenant-wide' }
)

$support = @(
    @{ Id='TC-SUP-001'; Module='Auth'; Route='/Account/Login'; Priority='P0'; Pre='—'; Data='support@autonomuscrm.local'; Steps="1. Login"; Expected='/Customer360' }
    @{ Id='TC-SUP-002'; Module='Customer360'; Route='/Customer360'; Priority='P0'; Pre='Sesión Support'; Data='Banco Regional'; Steps="1. Buscar`n2. Abrir 360"; Expected='Vista cliente OK' }
    @{ Id='TC-SUP-003'; Module='Leads'; Route='/Leads'; Priority='P0'; Pre='Sesión Support'; Data='—'; Steps="1. Ver lista leads"; Expected='Lectura OK, sin botón crear' }
    @{ Id='TC-SUP-004'; Module='Leads'; Route='/Leads/Create'; Priority='P0'; Pre='Sesión Support'; Data='—'; Steps="1. GET /Leads/Create"; Expected='Redirect /Account/AccessDenied' }
    @{ Id='TC-SUP-005'; Module='Deals'; Route='/Deals'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Ver deals"; Expected='Lectura OK' }
    @{ Id='TC-SUP-006'; Module='Deals'; Route='/Deals/Create'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Intentar crear deal"; Expected='AccessDenied' }
    @{ Id='TC-SUP-007'; Module='Customer Success'; Route='/customer-success'; Priority='P0'; Pre='Sesión Support'; Data='ticket'; Steps="1. CreateTicket`n2. CloseTicket"; Expected='Ticket lifecycle OK' }
    @{ Id='TC-SUP-008'; Module='Trust'; Route='/TrustInbox'; Priority='P1'; Pre='—'; Data='—'; Steps="1. Ver cola trust"; Expected='Lectura OK' }
    @{ Id='TC-SUP-009'; Module='Customers'; Route='/Customers/Details/{id}'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Ver detalle`n2. Intentar delete"; Expected='Delete bloqueado o denied' }
    @{ Id='TC-SUP-010'; Module='Users'; Route='/Users'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Acceder Users"; Expected='No autorizado' }
    @{ Id='TC-SUP-011'; Module='Tasks'; Route='/Tasks'; Priority='P0'; Pre='CS tickets seed'; Data='—'; Steps="1. Ver tickets CS_Ticket"; Expected='Tasks visibles' }
    @{ Id='TC-SUP-012'; Module='Workflows'; Route='/Workflows/Create'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Intentar crear workflow"; Expected='AccessDenied' }
)

$viewer = @(
    @{ Id='TC-VWR-001'; Module='Auth'; Route='/Account/Login'; Priority='P0'; Pre='—'; Data='viewer@autonomuscrm.local'; Steps="1. Login"; Expected='Redirect /' }
    @{ Id='TC-VWR-002'; Module='Command'; Route='/'; Priority='P0'; Pre='Sesión Viewer'; Data='—'; Steps="1. Dashboard command"; Expected='Lectura OK' }
    @{ Id='TC-VWR-003'; Module='Leads'; Route='/Leads'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Listar leads"; Expected='Sin acciones write' }
    @{ Id='TC-VWR-004'; Module='Leads'; Route='/Leads/Create'; Priority='P0'; Pre='—'; Data='—'; Steps="1. GET Create"; Expected='AccessDenied' }
    @{ Id='TC-VWR-005'; Module='Customers'; Route='/Customers'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Listar"; Expected='Lectura OK' }
    @{ Id='TC-VWR-006'; Module='Deals'; Route='/Deals'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Pipeline lectura"; Expected='OK' }
    @{ Id='TC-VWR-007'; Module='Executive'; Route='/executive'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Ver executive"; Expected='Lectura OK' }
    @{ Id='TC-VWR-008'; Module='Revenue'; Route='/revenue'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Ver revenue"; Expected='Lectura OK' }
    @{ Id='TC-VWR-009'; Module='Audit'; Route='/Audit'; Priority='P1'; Pre='—'; Data='—'; Steps="1. Ver audit log"; Expected='Lectura OK' }
    @{ Id='TC-VWR-010'; Module='Trust'; Route='/TrustInbox'; Priority='P1'; Pre='—'; Data='—'; Steps="1. Ver trust (sin approve si restringido)"; Expected='Página carga' }
    @{ Id='TC-VWR-011'; Module='Users'; Route='/Users'; Priority='P0'; Pre='—'; Data='—'; Steps="1. Acceder"; Expected='Denied' }
    @{ Id='TC-VWR-012'; Module='Workflows'; Route='/Workflows/Edit/{id}'; Priority='P0'; Pre='—'; Data='—'; Steps="1. GET Edit"; Expected='AccessDenied' }
)

function Write-Suite($name, $title, $user, $cases) {
    $path = Join-Path $OutDir "${name}_TEST_CASES.md"
    $body = @"
# $title

**Entorno:** $($env.BaseUrl)  
**Usuario:** ``$user``  
**Password:** ``$($env.Password)``  
**Generado:** $(Get-Date -Format 'yyyy-MM-dd')

---

"@
    foreach ($tc in $cases) { $body += Format-Tc $tc }
    $body += @"

---

**Total casos:** $($cases.Count)  
**Ejecutados:** _/_ | **PASS:** _/_ | **FAIL:** _/_

"@
    Set-Content $path $body -Encoding UTF8
    Write-Host "Wrote $path ($($cases.Count) cases)"
}

Write-Suite 'SUPERADMIN' 'SuperAdmin — Casos de Prueba Funcionales' 'superadmin@autonomuscrm.local' $superadmin
Write-Suite 'ADMIN' 'Admin — Casos de Prueba Funcionales' 'admin@autonomuscrm.local' $admin
Write-Suite 'MANAGER' 'Manager — Casos de Prueba Funcionales' 'manager@autonomuscrm.local' $manager
Write-Suite 'SALES' 'Sales — Casos de Prueba Funcionales' 'sales1@autonomuscrm.local' $sales
Write-Suite 'SUPPORT' 'Support — Casos de Prueba Funcionales' 'support@autonomuscrm.local' $support
Write-Suite 'VIEWER' 'Viewer — Casos de Prueba Funcionales' 'viewer@autonomuscrm.local' $viewer
Write-Host 'Done.'
