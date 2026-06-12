# SuperAdmin â€” Casos de Prueba Funcionales

**Entorno:** http://164.68.99.83:8091  
**Usuario:** `superadmin@autonomuscrm.local`  
**Password:** `AutonomusTest123!`  
**Generado:** 2026-06-10

---
### TC-SA-001

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Auth |
| **Ruta** | `/Account/Login` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Usuario no autenticado |
| **Datos** | superadmin@autonomuscrm.local / AutonomusTest123! |

**Pasos:**
1. Abrir login
2. Dejar TenantId vacÃ­o
3. Ingresar credenciales
4. Submit

**Resultado esperado:** Redirect a /executive, sesiÃ³n activa, menÃº completo visible

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-002

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Executive OS |
| **Ruta** | `/executive` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | â€” |

**Pasos:**
1. Navegar a /executive
2. Verificar KPIs y empty state o datos seed

**Resultado esperado:** HTTP 200, dashboard Executive renderiza sin 500

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-003

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Users |
| **Ruta** | `/Users` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | â€” |

**Pasos:**
1. Abrir /Users
2. Verificar lista 7 usuarios seed

**Resultado esperado:** Tabla usuarios visible, roles mostrados

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-004

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Users |
| **Ruta** | `/Users/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | qa.new@techsolutions.pa / Test123! |

**Pasos:**
1. POST crear usuario vÃ­a formulario
2. Verificar redirect /Users

**Resultado esperado:** Usuario creado en listado

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-005

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Users |
| **Ruta** | `/Users/Edit/{id}` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | Usuario existente |
| **Datos** | Asignar rol Manager |

**Pasos:**
1. Editar usuario
2. AssignRole Manager
3. Guardar

**Resultado esperado:** Rol persistido en BD

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-006

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Settings |
| **Ruta** | `/Settings` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | timezone America/Panama |

**Pasos:**
1. Abrir Settings
2. Actualizar tenant settings
3. Guardar

**Resultado esperado:** POST exitoso, sin error

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-007

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Policies |
| **Ruta** | `/Policies/Create` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | expression: role in (Admin, Manager) |

**Pasos:**
1. Crear policy
2. Verificar en lista

**Resultado esperado:** Policy activa en /Policies

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-008

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Workflows |
| **Ruta** | `/Workflows/Edit/{id}` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | Workflow seed b1000004-...001 |
| **Datos** | Trigger DomainEvent LeadCreatedEvent |

**Pasos:**
1. Abrir Edit
2. Modal Add Trigger
3. Guardar

**Resultado esperado:** Trigger visible en workflow

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-009

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Trust Studio |
| **Ruta** | `/TrustInbox` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Audits seed si existen |
| **Datos** | â€” |

**Pasos:**
1. Abrir TrustInbox
2. Seleccionar item
3. Approve

**Resultado esperado:** AcciÃ³n registrada, cola actualizada

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-010

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Audit |
| **Ruta** | `/Audit` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Actividad previa |
| **Datos** | â€” |

**Pasos:**
1. Abrir Audit
2. Filtrar por fecha
3. Abrir modal detalle evento

**Resultado esperado:** Eventos listados, modal JSON

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-011

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Billing |
| **Ruta** | `/billing` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | â€” |

**Pasos:**
1. Abrir billing
2. Ver plan starter y usage

**Resultado esperado:** Dashboard usage visible (checkout UI no implementado en Razor)

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-012

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Integrations |
| **Ruta** | `/Integrations` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | â€” |

**Pasos:**
1. Abrir Integrations
2. Ver marketplace cards

**Resultado esperado:** PÃ¡gina carga, OAuth requiere credenciales externas

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-013

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Memory |
| **Ruta** | `/Memory` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | â€” |

**Pasos:**
1. Abrir Memory
2. Ver timeline/dashboard

**Resultado esperado:** HTTP 200, read-only dashboard

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-014

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Failed Events |
| **Ruta** | `/FailedEvents` |
| **Prioridad** | P2 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | â€” |

**Pasos:**
1. Navegar /FailedEvents (no en sidebar)
2. Ver cola

**Resultado esperado:** PÃ¡gina ops carga

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-015

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | API Users |
| **Ruta** | `POST /api/users` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | JWT Admin |
| **Datos** | Bearer token |

**Pasos:**
1. Login API
2. POST crear usuario

**Resultado esperado:** 201 Created (RequireAdmin policy)

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-016

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Provisioning |
| **Ruta** | `POST /api/provisioning/tenants` |
| **Prioridad** | P2 |
| **PrecondiciÃ³n** | X-Platform-Key |
| **Datos** | Provisioning API key |

**Pasos:**
1. POST nuevo tenant con platform key

**Resultado esperado:** Tenant creado (ops plataforma)

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-017

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | Lead QA SA |

**Pasos:**
1. Crear lead
2. Verificar en /Leads

**Resultado esperado:** Lead en listado

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-018

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Deals |
| **Ruta** | `/Deals/Details/{id}` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Deal seed d1000001-...001 |
| **Datos** | probability 80 |

**Pasos:**
1. Abrir Details
2. Modal Update Probability
3. Guardar

**Resultado esperado:** Probabilidad actualizada

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-019

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Customer360 |
| **Ruta** | `/Customer360` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | Banco Regional |

**Pasos:**
1. Buscar cliente
2. Abrir detalle /customers/{id}/360

**Resultado esperado:** Vista 360 enterprise

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SA-020

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Customer Success |
| **Ruta** | `/customer-success` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n SuperAdmin |
| **Datos** | ticket CS |

**Pasos:**
1. Crear ticket
2. Cerrar ticket

**Resultado esperado:** Ticket en lista

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |

---

**Total casos:** 20  
**Ejecutados:** _/_ | **PASS:** _/_ | **FAIL:** _/_

