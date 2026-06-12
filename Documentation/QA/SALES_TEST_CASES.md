# Sales â€” Casos de Prueba Funcionales

**Entorno:** http://164.68.99.83:8091  
**Usuario:** `sales1@autonomuscrm.local`  
**Password:** `AutonomusTest123!`  
**Generado:** 2026-06-10

---
### TC-SALES-001

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | sales1 autenticado |
| **Datos** | qa.lead.sales@techsolutions.pa |

**Pasos:**
1. Abrir Leads
2. Crear Lead
3. Guardar

**Resultado esperado:** Lead creado, visible en listado, sin errores

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-002

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Auth |
| **Ruta** | `/Account/Login` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | sales1@autonomuscrm.local |

**Pasos:**
1. Login

**Resultado esperado:** Redirect /revenue

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-003

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Revenue |
| **Ruta** | `/revenue` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Sales |
| **Datos** | â€” |

**Pasos:**
1. Ver Revenue OS home

**Resultado esperado:** Dashboard personal ventas

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-004

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads/Edit/{id}` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Lead propio |
| **Datos** | â€” |

**Pasos:**
1. Editar lead asignado

**Resultado esperado:** Cambios guardados

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-005

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads/Details/{id}` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Lead calificado |
| **Datos** | â€” |

**Pasos:**
1. Qualify
2. Convert to Customer

**Resultado esperado:** Customer creado

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-006

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Customers |
| **Ruta** | `/Customers/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Sales |
| **Datos** | â€” |

**Pasos:**
1. Crear customer

**Resultado esperado:** OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-007

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Deals |
| **Ruta** | `/Deals/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Customer existente |
| **Datos** | â€” |

**Pasos:**
1. Crear deal
2. Asignar amount

**Resultado esperado:** Deal en pipeline

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-008

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Deals |
| **Ruta** | `/Deals/Details/{id}` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Deal abierto |
| **Datos** | â€” |

**Pasos:**
1. Update stage modal
2. Mover a negociaciÃ³n

**Resultado esperado:** Stage actualizado

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-009

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Deals |
| **Ruta** | `/Deals/Details/{id}` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Deal listo |
| **Datos** | â€” |

**Pasos:**
1. Close Won

**Resultado esperado:** Deal cerrado ganado

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-010

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Users |
| **Ruta** | `/Users` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Sales |
| **Datos** | â€” |

**Pasos:**
1. Intentar /Users

**Resultado esperado:** 403 o AccessDenied

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-011

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Settings |
| **Ruta** | `/Settings` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n Sales |
| **Datos** | â€” |

**Pasos:**
1. Intentar /Settings

**Resultado esperado:** 403 Admin/Manager only

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-012

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Workflows |
| **Ruta** | `/Workflows/Edit/{id}` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n Sales |
| **Datos** | â€” |

**Pasos:**
1. Editar workflow

**Resultado esperado:** Permitido (commercial write)

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-013

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Tasks |
| **Ruta** | `/Tasks` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Task asignada sales1 |
| **Datos** | â€” |

**Pasos:**
1. Completar task

**Resultado esperado:** Status Completed

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-014

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | FlowActions |
| **Ruta** | `/FlowActions` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | Insight CTA en Revenue |
| **Datos** | â€” |

**Pasos:**
1. Click Create Task desde insight

**Resultado esperado:** Task creada

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SALES-015

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | sales2 autenticado |
| **Datos** | â€” |

**Pasos:**
1. sales2 crea lead
2. sales1 ve en lista

**Resultado esperado:** Visibilidad tenant-wide

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |

---

**Total casos:** 15  
**Ejecutados:** _/_ | **PASS:** _/_ | **FAIL:** _/_

