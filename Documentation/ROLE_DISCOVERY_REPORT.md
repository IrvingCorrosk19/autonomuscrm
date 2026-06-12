# ROLE_DISCOVERY_REPORT — Inventario de Roles AutonomusCRM

**Fecha de análisis:** 2026-06-05  
**Método:** Análisis estático de código, seed, migraciones, middleware, policies, menús y controllers  
**Principio:** Solo roles verificados en implementación real

---

## 1. Resumen ejecutivo

AutonomusCRM implementa **exactamente 5 roles RBAC** como cadenas de texto en `User.Roles` (columna `jsonb` en PostgreSQL). **No existe enum de roles** ni rol SuperAdmin.

| Rol | Usuarios demo | Home post-login |
|-----|---------------|-----------------|
| Admin | admin@autonomuscrm.local | `/executive` |
| Manager | manager@autonomuscrm.local | `/executive` |
| Sales | sales@autonomuscrm.local | `/revenue` |
| Support | support@autonomuscrm.local | `/Customer360` |
| Viewer | viewer@autonomuscrm.local | `/` |

**Evidencia seed:** `AutonomusCRM.Infrastructure/Persistence/Seed/DemoRoleUsers.cs`  
**Evidencia redirect:** `AutonomusCRM.API/Infrastructure/RoleHomeRedirect.cs`  
**Whitelist UI:** `Users/Edit.cshtml.cs`, `Users/Roles.cshtml.cs` → `{ Admin, Manager, Sales, Support, Viewer }`

---

## 2. Roles descubiertos (detalle)

### 2.1 Admin

| Atributo | Valor |
|----------|-------|
| **Descripción** | Administrador del tenant con máximos privilegios operativos |
| **Permisos distintivos** | Único rol con `POST /api/tenants` y `POST /api/users` (`RequireAdmin`) |
| **Menús visibles** | Los 19 ítems del sidebar (sin filtro por rol) |
| **Módulos accesibles** | Todos los módulos autenticados |
| **Escritura comercial UI** | Sí (Leads, Customers, Deals, Workflows, Policies) |
| **Gestión usuarios** | Sí (`/Users/*` — Admin + Manager) |
| **Settings** | Sí (`/Settings` — Admin + Manager) |
| **Trust Studio** | Acceso completo (aprobar decisiones HITL) |
| **Cantidad permisos distintivos** | ~45 acciones (lectura global + escritura comercial + admin UI + API provisioning) |

### 2.2 Manager

| Atributo | Valor |
|----------|-------|
| **Descripción** | Gerente comercial/operativo del tenant |
| **Permisos distintivos** | Igual que Admin en UI excepto API `RequireAdmin` |
| **Home** | `/executive` |
| **Escritura comercial** | Sí |
| **Gestión usuarios / Settings** | Sí |
| **API tenants/users POST** | No |
| **Cantidad permisos distintivos** | ~40 acciones |

### 2.3 Sales

| Atributo | Valor |
|----------|-------|
| **Descripción** | Ejecutivo de ventas — ciclo Lead → Deal → cierre |
| **Home** | `/revenue` |
| **Escritura comercial** | Sí (middleware + handlers Leads) |
| **Gestión usuarios / Settings** | No (Access Denied) |
| **Pantallas primarias** | `/revenue`, `/Leads`, `/Deals`, `/Tasks`, `/Customers` |
| **Cantidad permisos distintivos** | ~25 acciones comerciales |

### 2.4 Support

| Atributo | Valor |
|----------|-------|
| **Descripción** | Soporte y Customer Success — post-venta, retención |
| **Home** | `/Customer360` |
| **Escritura comercial UI** | **No** (middleware bloquea POST y Create/Edit) |
| **Lectura comercial** | Sí (listas y detalles GET) |
| **Pantallas primarias** | `/customer-success`, `/Customer360`, `/Customers` (lectura) |
| **Brecha API** | API comercial POST permitida si autenticado (sin filtro rol) |
| **Cantidad permisos distintivos** | ~15 acciones (lectura + CS) |

### 2.5 Viewer

| Atributo | Valor |
|----------|-------|
| **Descripción** | Consulta y reportes — stakeholders de solo lectura |
| **Home** | `/` (Command Center) |
| **Escritura comercial UI** | **No** (igual que Support) |
| **Gestión usuarios** | No |
| **SCIM default** | Rol por defecto si no se especifica en provisioning |
| **Cantidad permisos distintivos** | ~12 acciones (lectura) |

---

## 3. Matriz resumida de acciones

| Rol | Crear | Editar | Eliminar | Aprobar | Configurar | Administrar |
| --- | ----- | ------ | -------- | ------- | ---------- | ----------- |
| **Admin** | ✅ Comercial + usuarios | ✅ | ✅ Leads (handlers) | ✅ Trust Studio | ✅ Settings, Policies, Workflows | ✅ Tenants/Users API |
| **Manager** | ✅ Comercial + usuarios | ✅ | ✅ Leads | ✅ Trust Studio | ✅ Settings, Policies | ✅ Users UI (no API tenants) |
| **Sales** | ✅ Leads/Customers/Deals | ✅ | ✅ Leads | ❌ (consulta) | ❌ | ❌ |
| **Support** | ❌ UI comercial | ❌ UI comercial | ❌ | ❌ | ❌ | ❌ |
| **Viewer** | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

**Notas:**
- *Crear/Editar/Eliminar* comercial = UI en `/Leads`, `/Customers`, `/Deals`, `/Workflows`, `/Policies`
- *Aprobar* = Trust Studio `/TrustInbox` (Admin/Manager operativo)
- *Configurar* = `/Settings`, `/Integrations`, `/Policies`
- *Administrar* = `/Users`, `POST /api/tenants`, `POST /api/users`

---

## 4. Nombres evaluados que NO son roles

| Nombre buscado | Resultado | Qué es en el sistema |
|----------------|-----------|----------------------|
| SuperAdmin | **No existe** | Admin es el rol máximo |
| Marketing | **No es rol** | Páginas públicas `/landing`, `/roi`, `/demo`; claves i18n `Marketing_*` |
| Customer Success | **No es rol** | Módulo `/customer-success`; usuario típico: Support |
| Operations | **No es rol** | Sección sidebar "Operation"; agente IA "Operations Agent" |
| Executive | **No es rol** | Dashboard `/executive` para Admin/Manager |
| Customer | **No es rol** | Entidad de dominio `Customer` |
| Agent | **No es rol** | Workforce IA en `/Agents` |
| Analyst | **No existe** | Sin página ni rol dedicado |

---

## 5. Fuentes de evidencia

| Componente | Archivo |
|------------|---------|
| Seed usuarios demo | `Infrastructure/Persistence/Seed/DemoRoleUsers.cs` |
| Entidad User.Roles | `Domain/Users/User.cs` |
| Migración jsonb Roles | `Migrations/20251224185349_InitialCreate.cs` |
| Middleware escritura | `API/Middleware/CommercialWriteAuthorizationMiddleware.cs` |
| Home redirect | `API/Infrastructure/RoleHomeRedirect.cs` |
| Policies ASP.NET | `Application/Authorization/Policies/AuthorizationPolicies.cs` |
| RequireAdmin uso | `API/Controllers/TenantsController.cs`, `UsersController.cs` |
| Leads authorize | `API/Pages/Leads/*.cshtml.cs` |
| Users/Settings authorize | `API/Pages/Users/*.cshtml.cs`, `Settings.cshtml.cs` |
| Sidebar (sin filtro rol) | `API/Pages/Shared/Flow/_FlowSidebar.cshtml` |
| Claims JWT/cookie | `Application/Auth/TokenService.cs` |
| ABAC (no RBAC) | `Infrastructure/Policies/PolicyEngine.cs` |
| SCIM default Viewer | `API/Controllers/EnterpriseAuthController.cs` |

---

## 6. Manuales generados (solo roles reales)

| Rol | Manual |
|-----|--------|
| Admin | `Roles/Admin_User_Manual.md` + `ADMIN_OPERATIONS_GUIDE.md` |
| Manager | `Roles/Manager_User_Manual.md` |
| Sales | `Roles/Sales_User_Manual.md` + `SALES_PLAYBOOK.md` |
| Support | `Roles/Support_User_Manual.md` + `SUPPORT_OPERATIONS_GUIDE.md` + `CUSTOMER_SUCCESS_PLAYBOOK.md` |
| Viewer | `Roles/Viewer_User_Manual.md` |

**No generado:** `SuperAdmin_User_Manual.md` — rol inexistente. Admin cubre administración máxima.

**Guía funcional (no rol):** `MARKETING_OPERATIONS_GUIDE.md` — generación de leads sin rol Marketing.

---

## 7. Riesgos de seguridad documentados

1. **Brecha UI vs API:** Support/Viewer bloqueados en Razor; API comercial solo exige autenticación.
2. **RequireManager / RequireSales:** Registradas, no aplicadas en controllers comerciales.
3. **AssignRole:** Acepta string arbitrario en dominio; whitelist solo en UI de edición.
4. **Sidebar:** Muestra enlaces Admin a todos los roles (Access Denied al navegar).

---

*Fin del reporte de descubrimiento de roles.*
