# Admin â€” Casos de Prueba Funcionales

**Entorno:** http://164.68.99.83:8091  
**Usuario:** `admin@autonomuscrm.local`  
**Password:** `AutonomusTest123!`  
**Generado:** 2026-06-10

---
### TC-ADM-001

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Auth |
| **Ruta** | `/Account/Login` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | No autenticado |
| **Datos** | admin@autonomuscrm.local |

**Pasos:**
1. Login
2. Verificar redirect

**Resultado esperado:** /executive

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-002

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Revenue OS |
| **Ruta** | `/revenue` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Admin |
| **Datos** | â€” |

**Pasos:**
1. Abrir Revenue
2. Ver mÃ©tricas pipeline

**Resultado esperado:** Dashboard Revenue OS OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-003

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | 10 leads seed |
| **Datos** | â€” |

**Pasos:**
1. Listar leads
2. Buscar/filtrar
3. Abrir drawer preview

**Resultado esperado:** Tabla responsive, drawer funcional

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-004

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads/Import` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | CSV vÃ¡lido |
| **Datos** | import modal |

**Pasos:**
1. Modal import
2. Subir CSV
3. Confirmar

**Resultado esperado:** Leads importados

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-005

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Customers |
| **Ruta** | `/Customers/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Admin |
| **Datos** | Nuevo Cliente QA |

**Pasos:**
1. Crear customer
2. Verificar listado

**Resultado esperado:** Customer creado

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-006

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Customers |
| **Ruta** | `/Customers/Details/{id}` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Customer seed |
| **Datos** | â€” |

**Pasos:**
1. Details
2. Modal Create Deal
3. Crear deal

**Resultado esperado:** Deal vinculado

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-007

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Deals |
| **Ruta** | `/Deals/BulkActions` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | Deals seleccionados |
| **Datos** | bulk stage |

**Pasos:**
1. Seleccionar deals
2. Bulk modal
3. Aplicar

**Resultado esperado:** Stage actualizado

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-008

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Tasks |
| **Ruta** | `/Tasks` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | 8 tasks seed |
| **Datos** | â€” |

**Pasos:**
1. Listar tasks
2. Complete task
3. Assign task

**Resultado esperado:** Estado actualizado

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-009

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Workflows |
| **Ruta** | `/Workflows` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | 4 workflows seed |
| **Datos** | â€” |

**Pasos:**
1. Listar workflows
2. Ver activos/inactivos

**Resultado esperado:** Lista correcta

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-010

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Policies |
| **Ruta** | `/Policies/Edit/{id}` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | Policy seed |
| **Datos** | â€” |

**Pasos:**
1. Editar
2. Duplicate
3. Verificar copia

**Resultado esperado:** Duplicado en lista

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-011

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Users |
| **Ruta** | `/Users/Roles` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n Admin |
| **Datos** | â€” |

**Pasos:**
1. Abrir /Users/Roles
2. Ver conteos por rol

**Resultado esperado:** Matriz roles visible

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-012

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Settings |
| **Ruta** | `/Settings` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Admin |
| **Datos** | Export config |

**Pasos:**
1. ExportConfig
2. Descargar JSON

**Resultado esperado:** Archivo export generado

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-013

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Command |
| **Ruta** | `/` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Admin |
| **Datos** | â€” |

**Pasos:**
1. Command Center
2. Ctrl+K palette
3. Buscar Leads

**Resultado esperado:** Palette navega a ruta

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-014

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Agents |
| **Ruta** | `/Agents` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n Admin |
| **Datos** | â€” |

**Pasos:**
1. Abrir Workforce/Agents

**Resultado esperado:** Vista agentes AI

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-015

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Voice |
| **Ruta** | `/VoiceCalls` |
| **Prioridad** | P2 |
| **PrecondiciÃ³n** | SesiÃ³n Admin |
| **Datos** | GUID customer |

**Pasos:**
1. Log manual call

**Resultado esperado:** Call registrada (MVP)

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-016

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Localization |
| **Ruta** | `cualquier pÃ¡gina` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n Admin |
| **Datos** | es / es-PA |

**Pasos:**
1. Cambiar idioma selector
2. Verificar labels

**Resultado esperado:** UI traducida

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-017

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads/Details/{id}` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Lead seed f1000001-...009 |
| **Datos** | â€” |

**Pasos:**
1. Qualify lead
2. Convert to customer

**Resultado esperado:** Customer creado desde lead

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-018

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Deals |
| **Ruta** | `/Deals/Details/{id}` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Deal abierto |
| **Datos** | â€” |

**Pasos:**
1. Close Won modal
2. Confirmar

**Resultado esperado:** Deal status Won

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-019

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Integrations |
| **Ruta** | `/Integrations` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | Sin OAuth config |
| **Datos** | â€” |

**Pasos:**
1. Intentar Connect HubSpot

**Resultado esperado:** Error controlado o redirect OAuth

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-ADM-020

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Audit |
| **Ruta** | `/Audit` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Post CRUD |
| **Datos** | â€” |

**Pasos:**
1. Export audit CSV

**Resultado esperado:** Export descargable

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |

---

**Total casos:** 20  
**Ejecutados:** _/_ | **PASS:** _/_ | **FAIL:** _/_

