# Support â€” Casos de Prueba Funcionales

**Entorno:** http://164.68.99.83:8091  
**Usuario:** `support@autonomuscrm.local`  
**Password:** `AutonomusTest123!`  
**Generado:** 2026-06-10

---
### TC-SUP-001

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Auth |
| **Ruta** | `/Account/Login` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | support@autonomuscrm.local |

**Pasos:**
1. Login

**Resultado esperado:** /Customer360

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SUP-002

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Customer360 |
| **Ruta** | `/Customer360` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Support |
| **Datos** | Banco Regional |

**Pasos:**
1. Buscar
2. Abrir 360

**Resultado esperado:** Vista cliente OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SUP-003

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Support |
| **Datos** | â€” |

**Pasos:**
1. Ver lista leads

**Resultado esperado:** Lectura OK, sin botÃ³n crear

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SUP-004

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Leads |
| **Ruta** | `/Leads/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Support |
| **Datos** | â€” |

**Pasos:**
1. GET /Leads/Create

**Resultado esperado:** Redirect /Account/AccessDenied

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SUP-005

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Deals |
| **Ruta** | `/Deals` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Ver deals

**Resultado esperado:** Lectura OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SUP-006

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Deals |
| **Ruta** | `/Deals/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Intentar crear deal

**Resultado esperado:** AccessDenied

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SUP-007

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Customer Success |
| **Ruta** | `/customer-success` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | SesiÃ³n Support |
| **Datos** | ticket |

**Pasos:**
1. CreateTicket
2. CloseTicket

**Resultado esperado:** Ticket lifecycle OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SUP-008

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Trust |
| **Ruta** | `/TrustInbox` |
| **Prioridad** | P1 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Ver cola trust

**Resultado esperado:** Lectura OK

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SUP-009

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Customers |
| **Ruta** | `/Customers/Details/{id}` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Ver detalle
2. Intentar delete

**Resultado esperado:** Delete bloqueado o denied

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SUP-010

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Users |
| **Ruta** | `/Users` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Acceder Users

**Resultado esperado:** No autorizado

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SUP-011

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Tasks |
| **Ruta** | `/Tasks` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | CS tickets seed |
| **Datos** | â€” |

**Pasos:**
1. Ver tickets CS_Ticket

**Resultado esperado:** Tasks visibles

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |
### TC-SUP-012

| Campo | Valor |
|-------|-------|
| **MÃ³dulo** | Workflows |
| **Ruta** | `/Workflows/Create` |
| **Prioridad** | P0 |
| **PrecondiciÃ³n** | â€” |
| **Datos** | â€” |

**Pasos:**
1. Intentar crear workflow

**Resultado esperado:** AccessDenied

| Resultado obtenido | Estado |
|--------------------|--------|
| _Pendiente ejecuciÃ³n humana_ | â˜ PASS â˜ FAIL |

---

**Total casos:** 12  
**Ejecutados:** _/_ | **PASS:** _/_ | **FAIL:** _/_

