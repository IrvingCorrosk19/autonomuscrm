# ROLE TEST MATRIX — TechSolutions Panamá

**Empresa:** TechSolutions Panamá  
**Fecha:** 2026-05-28  
**Entorno:** Instalación limpia post-provisioning, sin datos demo  
**Roles en código:** Admin, Manager, Sales, Support, Viewer (5 roles — **no SuperAdmin**)

> El escenario pide 1 SuperAdmin — se mapea al **Admin principal** (`admin@techsolutions.pa`) provisionado en bootstrap. No existe rol SuperAdmin en RBAC.

---

## Usuarios del escenario

| Usuario | Email | Rol sistema | Home esperado |
|---------|-------|-------------|---------------|
| Admin principal | `admin@techsolutions.pa` | Admin | `/executive` |
| Admin operaciones | `ops@techsolutions.pa` | Admin | `/executive` |
| Manager comercial | `manager@techsolutions.pa` | Manager | `/executive` |
| Sales rep 1 | `sales1@techsolutions.pa` | Sales | `/revenue` |
| Sales rep 2 | `sales2@techsolutions.pa` | Sales | `/revenue` |
| Support agent | `support@techsolutions.pa` | Support | `/Customer360` |
| Viewer ejecutivo | `viewer@techsolutions.pa` | Viewer | `/` |

---

## Matriz de pruebas por rol

### Admin principal (`admin@techsolutions.pa`)

| # | Objetivo | Prueba | Resultado esperado |
|---|----------|--------|-------------------|
| A1 | Bootstrap | Login post-provisioning | ✅ Sesión, redirect `/executive` |
| A2 | Administración | Crear usuario `ops@` | ✅ Usuario creado (sin rol hasta Edit) |
| A3 | RBAC | Asignar rol Admin a `ops@` | ✅ Rol persistido |
| A4 | Tenant | Actualizar nombre tenant | ✅ Si vía UpdateTenant |
| A5 | Provisioning API | POST `/api/provisioning/tenants` con platform key | ✅ Puede crear 2do tenant (si multi-tenant) |
| A6 | Trust | Aprobar decisión HITL en TrustInbox | ✅ Si hay audits pendientes |
| A7 | Billing | Ver `/billing` | ✅ Plan free, usage counts |
| A8 | Policies | Crear policy ABAC | ✅ Persistida |
| A9 | Workflows | Crear y activar workflow | ✅ En lista activos |
| A10 | Audit | Ver `/Audit` tras actividad | ✅ Eventos registrados |
| A11 | Settings | Cambiar región/timezone | ⚠️ UI OK, persistencia log-only |
| A12 | Integrations | Configurar OAuth HubSpot | ⚠️ Requiere ClientId/Secret |
| A13 | Users API | POST `/api/users` | ✅ 201 con JWT Admin |
| A14 | Commercial write | POST `/Leads/Create` | ✅ Permitido |
| A15 | Failed Events | Ver cola si RabbitMQ falla | ✅ Acceso Admin |

---

### Admin operaciones (`ops@techsolutions.pa`)

| # | Objetivo | Prueba | Resultado esperado |
|---|----------|--------|-------------------|
| O1 | Paridad Admin | Mismas rutas que Admin principal | ✅ Equivalente Admin |
| O2 | Usuarios | Crear `manager@` | ✅ |
| O3 | Roles | Asignar Manager a `manager@` | ✅ |
| O4 | Seguridad | Intentar sin sesión → `/Users` | ❌ Redirect login |
| O5 | API tenants | POST `/api/tenants` | ✅ Crea tenant (sin user) |

---

### Manager (`manager@techsolutions.pa`)

| # | Objetivo | Prueba | Resultado esperado |
|---|----------|--------|-------------------|
| M1 | Home | Login | ✅ Redirect `/executive` |
| M2 | Pipeline | Ver Executive OS con deals | ✅ Empty o con data creada |
| M3 | Equipo | Crear `sales1@` vía `/Users/Create` | ✅ (Admin,Manager autorizados) |
| M4 | Roles | Asignar Sales a `sales1@` | ✅ |
| M5 | Commercial write | Crear lead | ✅ |
| M6 | Commercial write | Crear deal | ✅ |
| M7 | Workflows | Crear workflow | ✅ |
| M8 | Trust | Ver TrustInbox | ✅ Lectura/aprobación según implementación |
| M9 | Restricted | POST `/api/provisioning/tenants` sin platform key | ❌ 401 (no es ops de plataforma) |
| M10 | Support data | Editar ticket CS | ✅ Según permisos página |

---

### Sales (`sales1@`, `sales2@`)

| # | Objetivo | Prueba | Resultado esperado |
|---|----------|--------|-------------------|
| S1 | Home | Login sales1 | ✅ Redirect `/revenue` |
| S2 | Leads | Crear lead propio | ✅ |
| S3 | Leads | Editar lead asignado | ✅ |
| S4 | Customer | Convertir lead → customer | ✅ |
| S5 | Deal | Crear oportunidad | ✅ |
| S6 | Deal | Mover a Closed Won | ✅ |
| S7 | Restricted | Acceder `/Users` | ❌ 403 o redirect (solo Admin/Manager) |
| S8 | Restricted | POST `/api/users` | ❌ 403 |
| S9 | Restricted | `/Settings` cambios críticos | ❌ o lectura según página |
| S10 | Viewer parity | Sales2 ve deals de Sales1 | ✅ Mismo tenant — visibilidad tenant-wide |

---

### Support (`support@techsolutions.pa`)

| # | Objetivo | Prueba | Resultado esperado |
|---|----------|--------|-------------------|
| U1 | Home | Login | ✅ Redirect `/Customer360` |
| U2 | Customers | Ver lista clientes | ✅ Lectura |
| U3 | Leads | Ver `/Leads` | ✅ Lectura |
| U4 | Write block | POST `/Leads/Create` | ❌ AccessDenied (middleware) |
| U5 | Write block | GET `/Leads/Create` | ❌ Redirect AccessDenied |
| U6 | Deals | Ver deals | ✅ Lectura |
| U7 | Write block | POST `/Deals/Create` | ❌ AccessDenied |
| U8 | CS tickets | Crear/ver tickets | ✅ Según Customer Success OS |
| U9 | Trust | Ver TrustInbox | ✅ Lectura (aprobar puede estar restringido) |
| U10 | Users | Acceder `/Users` | ❌ No autorizado |

---

### Viewer (`viewer@techsolutions.pa`)

| # | Objetivo | Prueba | Resultado esperado |
|---|----------|--------|-------------------|
| V1 | Home | Login | ✅ Redirect `/` |
| V2 | Dashboard | Ver métricas | ✅ Lectura |
| V3 | Leads | Lista leads | ✅ Sin botones crear |
| V4 | Write block | Cualquier POST comercial | ❌ AccessDenied |
| V5 | Executive | Acceder `/executive` | ✅ Lectura si ruta permitida |
| V6 | Revenue | Acceder `/revenue` | ✅ Lectura |
| V7 | Settings | Acceder `/Settings` | ❌ o lectura limitada |
| V8 | API | POST `/api/leads` autenticado | ⚠️ API sin filtro rol comercial — **brecha** |
| V9 | Export | Exportar datos si disponible | Según implementación export |
| V10 | Audit | Ver audit log | ❌ típicamente Admin only |

---

## Matriz cruzada — permisos por módulo

| Módulo | Admin | Manager | Sales | Support | Viewer |
|--------|-------|---------|-------|---------|--------|
| `/executive` | ✅ RW* | ✅ RW* | ✅ R | ✅ R | ✅ R |
| `/revenue` | ✅ RW | ✅ RW | ✅ RW | ✅ R | ✅ R |
| `/Customer360` | ✅ RW | ✅ RW | ✅ R | ✅ RW | ✅ R |
| `/Leads` POST | ✅ | ✅ | ✅ | ❌ | ❌ |
| `/Customers` POST | ✅ | ✅ | ✅ | ❌ | ❌ |
| `/Deals` POST | ✅ | ✅ | ✅ | ❌ | ❌ |
| `/Users` | ✅ | ✅ | ❌ | ❌ | ❌ |
| `/Settings` | ✅ | ⚠️ | ❌ | ❌ | ❌ |
| `/Policies` POST | ✅ | ✅ | ❌ | ❌ | ❌ |
| `/Workflows` POST | ✅ | ✅ | ❌ | ❌ | ❌ |
| `/billing` | ✅ | ⚠️ R | ❌ | ❌ | ❌ |
| `/Audit` | ✅ | ⚠️ | ❌ | ❌ | ❌ |
| Trust HITL approve | ✅ | ✅ | ❌ | ⚠️ | ❌ |
| Provisioning API | ✅ (con key) | ❌ | ❌ | ❌ | ❌ |

\*RW en UI comercial vía middleware; configuración sistema puede variar.

---

## Secuencia de prueba recomendada (orden)

```
1. Provisionar tenant + admin@ (bootstrap)
2. admin@ crea ops@, manager@ → asigna roles
3. manager@ crea sales1@, sales2@, support@, viewer@ → asigna roles
4. sales1@ crea lead → customer → deal → closed won
5. Verificar métricas Executive/Revenue con cada rol
6. support@ verifica lectura sin escritura
7. viewer@ verifica solo lectura
8. admin@ configura workflow + policy
9. (Opcional) Activar IA y validar Trust HITL
```

---

## Notas de implementación

1. **SuperAdmin no existe** — usar Admin para pruebas de máximo privilegio.
2. **CreateUser no asigna rol** — paso obligatorio en `/Users/Edit` antes de probar permisos.
3. **API comercial sin filtro rol** — Viewer/Support podrían escribir vía API REST (brecha documentada).
4. **Multi-tenant login** — en Production, validar que cada usuario del tenant TechSolutions autentica correctamente.
