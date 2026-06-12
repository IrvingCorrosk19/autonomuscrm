# Manager â€” Casos de Prueba Funcionales

**Entorno:** http://164.68.99.83:8091  
**Usuario:** `manager@autonomuscrm.local`  
**Password:** `AutonomusTest123!`  
**Generado:** 2026-06-10

---
### TC-MGR-001

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Auth |
| **Ruta** | `/Account/Login` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | manager@autonomuscrm.local |

**Pasos:**
1. Login

**Resultado esperado:** /executive

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-002

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Executive |
| **Ruta** | `/executive` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Manager |
| **Datos** | â€” |

**Pasos:**
1. Ver Executive OS
2. Export executive HTML

**Resultado esperado:** Export descarga archivo

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-003

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Users |
| **Ruta** | `/Users/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Manager |
| **Datos** | nuevo sales |

**Pasos:**
1. Crear usuario sales
2. Asignar rol Sales

**Resultado esperado:** Usuario operativo

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-004

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Manager |
| **Datos** | Lead Manager |

**Pasos:**
1. Crear lead

**Resultado esperado:** Lead visible

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-005

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Deals |
| **Ruta** | `/Deals/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Customer seed |
| **Datos** | â€” |

**Pasos:**
1. Crear deal

**Resultado esperado:** Deal en pipeline

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-006

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Workflows |
| **Ruta** | `/Workflows/Create` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n Manager |
| **Datos** | WF QA |

**Pasos:**
1. Crear workflow

**Resultado esperado:** Workflow en lista

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-007

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Policies |
| **Ruta** | `/Policies/Create` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | SesiÃ³n Manager |
| **Datos** | â€” |

**Pasos:**
1. Crear policy

**Resultado esperado:** Policy activa

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-008

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Trust |
| **Ruta** | `/TrustInbox` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Pending audits |
| **Datos** | â€” |

**Pasos:**
1. Aprobar decisiÃ³n

**Resultado esperado:** Queue decrementa

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-009

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Revenue |
| **Ruta** | `/revenue` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Manager |
| **Datos** | â€” |

**Pasos:**
1. Ver Revenue OS

**Resultado esperado:** Dashboard OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-010

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Tasks |
| **Ruta** | `/Tasks` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Revisar pipeline semanal task seed

**Resultado esperado:** Task Completed visible

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-011

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Customer360 |
| **Ruta** | `/Customer360` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | Logistica |

**Pasos:**
1. Buscar cliente
2. Ver 360

**Resultado esperado:** Detalle cliente

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-012

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Settings |
| **Ruta** | `/Settings` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Manager |
| **Datos** | â€” |

**Pasos:**
1. Abrir Settings
2. Actualizar regiÃ³n

**Resultado esperado:** Acceso permitido Admin+Manager

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-013

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | API |
| **Ruta** | `POST /api/users` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | JWT Manager |
| **Datos** | â€” |

**Pasos:**
1. Intentar crear user API

**Resultado esperado:** 403 Forbidden (RequireAdmin)

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-014

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads/BulkActions` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Bulk assign

**Resultado esperado:** Leads actualizados

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-MGR-015

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Deals |
| **Ruta** | `/Deals/Edit/{id}` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | Deal seed |
| **Datos** | â€” |

**Pasos:**
1. Editar deal

**Resultado esperado:** Cambios guardados

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |

---

**Total casos:** 15  
**Ejecutados:** _/_ | **PASS:** _/_ | **FAIL:** _/_

