# Resultados ejecución E2E — AutonomusCRM (iteración 2)

| Campo | Valor |
|-------|-------|
| **Fecha** | 2026-05-25 |
| **Entornos** | **Local** `http://localhost:5154` (principal) · VPS `http://164.68.99.83:8091` |
| **Modo** | Corregir bug → re-probar → siguiente rol |

---

## Localhost — `http://localhost:5154` (2026-05-25) — **COMPLETO**

| Campo | Valor |
|-------|-------|
| **Tenant demo** | `d7a30c86-7bb7-4303-9c1b-a0518fd78c67` |
| **API** | `dotnet run --project AutonomusCRM.API` |
| **EventBus local** | `InMemory` en `appsettings.Development.json` (sin Docker) |
| **Health** | `/health` → **Healthy** con InMemory |
| **PostgreSQL** | Local `localhost:5432` |
| **Script automatizado** | `tests/e2e/run-local-e2e.ps1` → **39/39 PASS** |

### Resumen localhost por rol

| Rol | Login | `/Users` | `/Settings` | `/Leads` |
|-----|-------|----------|-------------|----------|
| Admin | PASS | PASS (5+ usuarios) | PASS | PASS |
| Manager | PASS | PASS | PASS | PASS |
| Sales | PASS | **AccessDenied** | **AccessDenied** | PASS |
| Support | PASS | **AccessDenied** | **AccessDenied** | PASS |
| Viewer | PASS | **AccessDenied** | **AccessDenied** | PASS |

### Suite P0 + P1 localhost (script)

| Área | Casos | Resultado |
|------|-------|-----------|
| Auth (5 roles + logout + bad pwd) | E2E-AUTH-* | **PASS** |
| Seguridad UI + API | E2E-SEC-* | **PASS** |
| FLUJO-01 (lead → qualify → customer → deal → close) | FLUJO-01, E2E-L/D | **PASS** |
| Usuarios (listar, crear, asignar rol) | E2E-U-* | **PASS** |
| Clientes (crear + import CSV) | E2E-C-01, C-02 | **PASS** |
| Workflows (crear) | E2E-W-01 | **PASS** |
| Auditoría (listar + export JSON) | E2E-AUD-* | **PASS** |
| Navegación (10 módulos) | E2E-NAV/* | **PASS** |
| API health + login + leads | E2E-API-* | **PASS** |

### Cambio de config local

`appsettings.Development.json`: `EventBus.Provider` = `InMemory` para desarrollo sin Docker Desktop.

### Ejecutar de nuevo

```powershell
cd c:\Proyectos\autonomuscrm
dotnet run --project AutonomusCRM.API --urls http://localhost:5154
powershell -File tests/e2e/run-local-e2e.ps1
```

### Credenciales demo (local)

| Rol | Email | Password |
|-----|-------|----------|
| Admin | admin@autonomuscrm.local | Admin123! |
| Manager | manager@autonomuscrm.local | Manager123! |
| Sales | sales@autonomuscrm.local | Sales123! |
| Support | support@autonomuscrm.local | Support123! |
| Viewer | viewer@autonomuscrm.local | Viewer123! |

---

## VPS — `http://164.68.99.83:8091` (iteración anterior)

---

## Correcciones aplicadas durante la corrida

| ID | Descripción | Archivos |
|----|-------------|----------|
| **FIX-001** | Listas vacías: `FilteredUsers = FilteredUsers` → `filteredUsers` | `Users.cshtml.cs`, `Leads.cshtml.cs`, `Deals.cshtml.cs` |
| **FIX-002** | Login error con `role="alert"` | `Login.cshtml` |
| **FIX-003** | RBAC UI: Users/Settings solo Admin,Manager | `Users*.cshtml.cs`, `Settings.cshtml.cs` |
| **FIX-004** | Redirect login/denied usa rutas relativas (`/Account/...`) | `Program.cs` |

---

## Resumen por rol

### Rol: Admin — **COMPLETO**

| Caso | Resultado |
|------|-----------|
| Login / Logout | PASS |
| Dashboard | PASS |
| Leads (lista 4+) | PASS (FIX-001) |
| Users (lista 5) | PASS (FIX-001) |
| Settings | PASS |
| FLUJO-01 comercial | PASS (corrida anterior) |
| API CreateUser | PASS |

### Rol: Manager — **COMPLETO** (smoke)

| Caso | Resultado |
|------|-----------|
| Login | PASS |
| `/Users` | PASS — 5 usuarios listados |
| Settings | PASS (misma política Admin,Manager) |

### Rol: Sales — **COMPLETO** (smoke)

| Caso | Resultado |
|------|-----------|
| Login | PASS |
| `/Users` | PASS → AccessDenied en `:8091` (FIX-003) |
| `/Leads` | PASS — 4 leads visibles |
| `/Settings` | PASS → AccessDenied (esperado) |

### Rol: Viewer — **COMPLETO** (smoke)

| Caso | Resultado |
|------|-----------|
| Login | PASS |
| `/Settings` | PASS → AccessDenied en `:8091` |
| Leads lectura | PASS |

### Rol: Support — **COMPLETO** (smoke)

| Caso | Resultado |
|------|-----------|
| Login | PASS |
| `/Support` | PASS |
| `/Users` | PASS → denegado (redirect a AccessDenied; usar `:8091/Account/AccessDenied`) |

---

## Suite P0 actualizada

| ID | Antes | Después |
|----|-------|---------|
| E2E-AUTH-02 | PARTIAL | **PASS** (alert visible vía DOM) |
| E2E-U-01 | FAIL | **PASS** (5 usuarios + Editar) |
| E2E-SEC-02 | KNOWN | **PASS** (Sales → AccessDenied CRM) |
| E2E-L lista | posible vacío | **PASS** (4 leads) |

---

## Defectos abiertos

### BUG-003 — Redirect sin puerto en nginx (Prioridad: Media)

| Campo | Detalle |
|-------|---------|
| **Síntoma** | `AccessDenied` redirigía a `http://164.68.99.83/` (puerto 80, otro sitio) |
| **Fix** | FIX-004 en código; **redeploy pendiente** en esta sesión |
| **Workaround pruebas** | Usar siempre URL con `:8091` |

---

## Próximos pasos (P1)

1. Redeploy VPS con FIX-004 (redirect relativo)  
2. E2E-U-02 crear usuario, E2E-U-03 asignar rol  
3. E2E-C import CSV, E2E-W-01 workflow  
4. E2E-AUD-01 auditoría  

---

## IDs de datos de prueba (sesión)

| Entidad | ID |
|---------|-----|
| Lead E2E | `f781c232-a37d-43e7-b08b-54674b0e63e9` |
| Customer | `3f1feb24-316a-4400-a993-205211e6ae0d` |
| Deal E2E | `f3c40ed8-19cb-4ece-9405-8268485d78b3` |

---

*Informe vivo — se actualiza en cada ciclo fix → test.*
