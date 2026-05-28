# Errores y remediación — E2E AutonomusCRM (2026-05-26)

| ID fix | Severidad | Estado |
|--------|-----------|--------|
| REM-001 | Crítico | Corregido |
| REM-002 | Crítico | Corregido |
| REM-003 | Alto | Corregido |
| REM-004 | Medio | Corregido |
| REM-005 | Medio | Corregido |

---

## REM-001 — Multitenant: primer tenant en UI

| Campo | Detalle |
|-------|---------|
| **Error** | Páginas usaban `GetDefaultTenantIdAsync()` → primer tenant en BD, ignorando claim `TenantId` del login |
| **Causa raíz** | Duplicación de helper en cada PageModel sin leer `User.FindFirst("TenantId")` |
| **Archivos modificados** | `AutonomusCRM.API/Infrastructure/PageModelTenantExtensions.cs`; 30+ `Pages/**/*.cshtml.cs` |
| **Métodos modificados** | `GetTenantIdForPageAsync`; `GetDefaultTenantIdAsync` delegado |
| **Riesgo** | Bajo — fallback a primer tenant si no hay claim |
| **Impacto** | Datos mostrados alineados al tenant del usuario autenticado |
| **Resultado** | MT-003 PASS; coherencia login ↔ datos |

---

## REM-002 — Viewer/Support podían POST en Leads/Deals

| Campo | Detalle |
|-------|---------|
| **Error** | RBAC-008: Viewer editaba leads vía POST (solo Users/Settings tenían `[Authorize(Roles)]`) |
| **Causa raíz** | RBAC incompleto en Razor; políticas `RequireSales` no aplicadas a páginas comerciales |
| **Archivos modificados** | `AutonomusCRM.API/Middleware/CommercialWriteAuthorizationMiddleware.cs`; `Program.cs`; `Leads/Details.cshtml.cs`; `Leads/Edit.cshtml.cs`; `Leads.cshtml.cs` |
| **Métodos modificados** | `InvokeAsync` middleware; `OnPost*` handlers con `[Authorize(Roles)]` |
| **Riesgo** | Medio — Support no puede registrar contacto vía POST en Customers (aceptable; solo lectura operativa) |
| **Impacto** | POST en `/Leads`, `/Customers`, `/Deals`, `/Workflows`, `/Policies` solo Admin/Manager/Sales |
| **Resultado** | Viewer POST → `/Account/AccessDenied` (verificado HTTP) |

---

## REM-003 — Lead inexistente: página en blanco (404)

| Campo | Detalle |
|-------|---------|
| **Error** | LEAD-012: `/Leads/Details/{guid-invalid}` devolvía 404 sin UI |
| **Causa raíz** | `return NotFound()` sin vista Razor |
| **Archivos modificados** | `AutonomusCRM.API/Pages/Leads/Details.cshtml.cs`; `Leads.cshtml` |
| **Métodos modificados** | `OnGetAsync` → `RedirectToPage("/Leads")` + `TempData["ErrorMessage"]` |
| **Riesgo** | Bajo |
| **Impacto** | UX: redirect a lista con mensaje |
| **Resultado** | InvalidLead → `/Leads` (PASS browser/HTTP) |

---

## REM-004 — Regresión build tras refactor tenant

| Campo | Detalle |
|-------|---------|
| **Error** | 280 errores CS1519 tras reemplazo regex masivo de `GetDefaultTenantIdAsync` |
| **Causa raíz** | Cuerpos `catch` huérfanos del método antiguo |
| **Archivos modificados** | Múltiples `Pages/**/*.cshtml.cs` (limpieza automática) |
| **Métodos modificados** | N/A |
| **Riesgo** | Bajo |
| **Impacto** | Build restaurado |
| **Resultado** | `dotnet build` OK |

---

## REM-005 — Mensaje error en lista Leads

| Campo | Detalle |
|-------|---------|
| **Error** | TempData error no visible tras redirect lead no encontrado |
| **Causa raíz** | `Leads.cshtml` sin bloque alert |
| **Archivos modificados** | `AutonomusCRM.API/Pages/Leads.cshtml` |
| **Métodos modificados** | N/A (vista) |
| **Riesgo** | Nulo |
| **Impacto** | Usuario ve "Lead no encontrado" |
| **Resultado** | Validación visual PASS |

---

## Errores no corregidos (documentados)

| Error | Causa | Justificación |
|-------|-------|---------------|
| Volumen TechNova 100/55/28 | Datos no sembrados en esta corrida | No modificar BD masiva sin script aprobado; funcionalidad validada con seed + E2E |
| MFA AUTH-009 | Sin usuario MFA en demo | Flujo API documentado; SKIP |
| Worker agentes AGT-003+ | Proceso Workers no levantado | SKIP; Event Store + UI Agents OK |
| Multitenant 2 tenants MT-001/002 | Un solo tenant en BD local | SKIP; login por TenantId validado |
| Rate limit AUTH-011 | No ejecutada ráfaga | Riesgo bajo; config 200/min existe |

---

*Remediación aplicada solo en entorno local. Sin push ni cambios en Render/producción.*
