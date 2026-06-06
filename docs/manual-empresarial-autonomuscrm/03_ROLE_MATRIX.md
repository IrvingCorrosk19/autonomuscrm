# 03 — Matriz de Roles y Permisos

**Roles definidos en código:** Admin, Manager, Sales, Support, Viewer  
**Evidencia:** `DemoRoleUsers.cs`, `Users/Roles.cshtml.cs`, `CommercialWriteAuthorizationMiddleware.cs`

---

## 1. Usuarios demo (seed)

| Rol | Email | Contraseña |
|-----|-------|------------|
| Admin | admin@autonomuscrm.local | Admin123! |
| Manager | manager@autonomuscrm.local | Manager123! |
| Sales | **sales@autonomuscrm.local** | **Sales123!** |
| Support | support@autonomuscrm.local | Support123! |
| Viewer | viewer@autonomuscrm.local | Viewer123! |

Patrón CEO_DEMO: `{Role}123!` para los mismos emails.

---

## 2. Pantalla de inicio por rol

`RoleHomeRedirect.cs`:

| Rol | Redirección post-login |
|-----|------------------------|
| Admin | `/executive` |
| Manager | `/executive` |
| **Sales** | **`/revenue`** |
| Support | `/Customer360` |
| Viewer | `/` (Command) |

---

## 3. Matriz de capacidades

| Capacidad | Admin | Manager | Sales | Support | Viewer |
|-----------|:-----:|:-------:|:-----:|:-------:|:------:|
| Acceso autenticado (lectura general) | ✅ | ✅ | ✅ | ✅ | ✅ |
| POST `/api/users` | ✅ | ❌ | ❌ | ❌ | ❌ |
| POST `/api/tenants` | ✅ | ❌ | ❌ | ❌ | ❌ |
| Gestión usuarios UI (`/Users/*`) | ✅ | ✅ | ❌ | ❌ | ❌ |
| Settings (`/Settings`) | ✅ | ✅ | ❌ | ❌ | ❌ |
| Crear/editar Leads (UI POST) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Crear/editar Customers (UI) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Crear/editar Deals (UI) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Workflows/Policies escritura UI | ✅ | ✅ | ✅ | ❌ | ❌ |
| Qualify/Convert/Delete Lead (handlers) | ✅ | ✅ | ✅ | ❌ | ❌ |
| API commercial POST (sin filtro rol) | ✅ | ✅ | ✅ | ✅* | ✅* |

\*Autenticado únicamente — **no hay** `[Authorize(Roles=...)]` en controllers comerciales.

---

## 4. Responsabilidades recomendadas por rol

### Sales (perfil del manual: sales@autonomuscrm.local)
- Gestionar leads, pipeline, cierre de deals
- Ejecutar seguimiento diario y tareas asignadas
- Usar Revenue OS y Command para priorizar
- **No** administrar usuarios ni settings del tenant

### Manager
- Supervisar pipeline, usuarios, políticas
- Aprobar operaciones en Trust Studio cuando aplique
- Configuración operativa en Settings

### Admin
- Todo lo de Manager + provisioning API tenants/users
- Políticas de control y auditoría completa

### Support
- Customer 360, Customer Success, tickets
- Lectura de datos comerciales; sin escritura en CRM comercial (UI)

### Viewer
- Solo lectura en módulos comerciales (UI)
- Command y reportes según acceso autenticado

---

## 5. Riesgos operativos documentados (del código)

1. **Brecha UI vs API:** Support/Viewer bloqueados en Razor writes pero API POST comercial no filtra por rol.
2. **Policies RequireManager/RequireSales:** registradas pero sin uso en endpoints.
3. **AssignRole:** acepta cualquier string de rol en dominio (sin whitelist estricta).

---

## 6. Buenas prácticas

- Asignar **Sales** solo a ejecutivos comerciales.
- Usar **Viewer** para stakeholders que solo consultan.
- Revisar **Audit** (`/Audit`) ante cambios sensibles.
- Rotar contraseñas demo en producción real (seed es demostración).
