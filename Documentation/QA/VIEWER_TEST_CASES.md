# Viewer â€” Casos de Prueba Funcionales

**Entorno:** http://164.68.99.83:8091  
**Usuario:** `viewer@autonomuscrm.local`  
**Password:** `AutonomusTest123!`  
**Generado:** 2026-06-10

---
### TC-VWR-001

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Auth |
| **Ruta** | `/Account/Login` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | viewer@autonomuscrm.local |

**Pasos:**
1. Login

**Resultado esperado:** Redirect /

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-VWR-002

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Command |
| **Ruta** | `/` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Viewer |
| **Datos** | â€” |

**Pasos:**
1. Dashboard command

**Resultado esperado:** Lectura OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-VWR-003

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Listar leads

**Resultado esperado:** Sin acciones write

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-VWR-004

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. GET Create

**Resultado esperado:** AccessDenied

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-VWR-005

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Customers |
| **Ruta** | `/Customers` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Listar

**Resultado esperado:** Lectura OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-VWR-006

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Deals |
| **Ruta** | `/Deals` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Pipeline lectura

**Resultado esperado:** OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-VWR-007

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Executive |
| **Ruta** | `/executive` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Ver executive

**Resultado esperado:** Lectura OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-VWR-008

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Revenue |
| **Ruta** | `/revenue` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Ver revenue

**Resultado esperado:** Lectura OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-VWR-009

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Audit |
| **Ruta** | `/Audit` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Ver audit log

**Resultado esperado:** Lectura OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-VWR-010

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Trust |
| **Ruta** | `/TrustInbox` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Ver trust (sin approve si restringido)

**Resultado esperado:** PÃ¡gina carga

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-VWR-011

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Users |
| **Ruta** | `/Users` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Acceder

**Resultado esperado:** Denied

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-VWR-012

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Workflows |
| **Ruta** | `/Workflows/Edit/{id}` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. GET Edit

**Resultado esperado:** AccessDenied

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |

---

**Total casos:** 12  
**Ejecutados:** _/_ | **PASS:** _/_ | **FAIL:** _/_

