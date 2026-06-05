# Pruebas funcionales por rol — AutonomusFlow

Plan para ejecutar **como agente de QA**: cada caso es un flujo **inicio → fin** (login → acción → verificación → logout). Usar en local (`https://localhost:7xxx` o el puerto de launchSettings) o VPS preview (`http://164.68.99.83:8091`).

---

## 1. Configuración del agente

| Campo | Valor |
|--------|--------|
| Tenant demo | **AutonomusCRM Demo** (o el que muestre el login; en dev el Tenant ID se autocompleta) |
| Contraseña por rol | `{Rol}123!` → `Admin123!`, `Manager123!`, `Sales123!`, `Support123!`, `Viewer123!` |
| Health check | `GET /health` → texto `Healthy` |
| Evidencia | Captura pantalla + URL final + mensaje flash/toast por paso crítico |

### Usuarios seed

| Rol | Email | Home tras login (`RoleHomeRedirect`) |
|-----|--------|--------------------------------------|
| Admin | `admin@autonomuscrm.local` | `/executive` |
| Manager | `manager@autonomuscrm.local` | `/executive` |
| Sales | `sales@autonomuscrm.local` | `/revenue` |
| Support | `support@autonomuscrm.local` | `/Customer360` |
| Viewer | `viewer@autonomuscrm.local` | `/` (Command) |

### Datos demo de referencia (tenant inicial)

- Clientes: Corporación Alpha, Beta Industries, Gamma Services  
- Leads: Lead Web 1, Referido VIP, Campaña Email  
- Deal: **Implementación CRM Q1** (~25 000) ligado a Alpha  

### Matriz de permisos (resumen)

| Capacidad | Admin | Manager | Sales | Support | Viewer |
|-----------|:-----:|:-------:|:-----:|:-------:|:------:|
| Executive / Revenue OS (lectura) | ✓ | ✓ | ✓ | ✓ | ✓ |
| Command `/` | ✓ | ✓ | ✓ | ✓ | ✓ |
| Trust Studio (aprobar/rechazar) | ✓ | ✓ | ✓* | ✓* | ✓* |
| Leads — crear/editar/calificar | ✓ | ✓ | ✓ | ✗ | ✗ |
| Customers / Deals — escritura | ✓ | ✓ | ✓ | ✗ | ✗ |
| Users / Settings | ✓ | ✓ | ✗ | ✗ | ✗ |
| Policies / Workflows — Create/Edit (GET) | ✓ | ✓ | ✓ | ✗ | ✗ |

\*Sin restricción de rol en página; cualquier usuario autenticado puede operar Trust si hay ítems en cola.

**Middleware comercial:** `Support` y `Viewer` reciben redirect a `/Account/AccessDenied` en POST (y GET a rutas `/Create`, `/Edit`) bajo `/Leads`, `/Customers`, `/Deals`, `/Workflows`, `/Policies`.

---

## 2. Orden de ejecución recomendado

1. **FT-COMMON-01** (smoke sin login)  
2. **FT-ADMIN-*** (crea datos que usarán otros roles)  
3. **FT-SALES-*** (pipeline comercial)  
4. **FT-MANAGER-*** (gobierno + usuarios)  
5. **FT-SUPPORT-*** (360 + solo lectura comercial)  
6. **FT-VIEWER-*** (lectura + negativas)  
7. **FT-CROSS-*** (cambio de rol / sesión)

---

## 3. Casos comunes (todos los roles)

### FT-COMMON-01 — Smoke público y login

| # | Acción | Resultado esperado |
|---|--------|-------------------|
| 1 | Abrir `/health` | `Healthy`, sin error 500 |
| 2 | Abrir `/Account/Login` | Formulario visible; en dev, cuentas demo listadas |
| 3 | Enviar credenciales vacías | Mensaje de error; permanece en login |
| 4 | Login con email válido y password incorrecta | “Credenciales inválidas” (o equivalente) |
| 5 | Cerrar pestaña y reabrir `/executive` sin cookie | Redirect a login |

### FT-COMMON-02 — Sesión y navegación global (ejecutar por rol)

| # | Acción | Resultado esperado |
|---|--------|-------------------|
| 1 | Login con usuario del rol bajo prueba | Redirect al **home del rol** (tabla §1) |
| 2 | Sidebar: abrir Command, Trust, Revenue, Executive, Customer 360 | Cada ruta carga sin HTTP 500 |
| 3 | `Ctrl+K` → buscar “Alpha” o “CRM” | Paleta muestra resultados o estado vacío coherente |
| 4 | Menú usuario → **Cerrar sesión** | Vuelta a `/Account/Login`; rutas protegidas redirigen a login |
| 5 | Volver a entrar con el mismo usuario | Home correcto; datos del tenant visibles |

---

## 4. Admin — flujos completos

### FT-ADMIN-01 — Gobierno ejecutivo (CEO path)

**Objetivo:** Validar vista C-level de punta a punta.

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Login `admin@autonomuscrm.local` / `Admin123!` | URL `/executive` |
| 2 | Revisar tarjetas KPI, alertas, narrativa | Contenido renderizado; sin excepción en UI |
| 3 | Cambiar periodo QBR si hay selector (`quarterly` / otros) | Datos se actualizan o mensaje coherente |
| 4 | Export **Executive summary** (`OnGetExport?type=executive`) | Descarga HTML `executive-summary.html` |
| 5 | Export **Board summary** (`type=board`) | Descarga `board-summary.html` |
| 6 | Ir a `/` (Command) | Dashboard Flow con periodo 7/30 días operativo |
| 7 | Logout | Sesión cerrada |

### FT-ADMIN-02 — Trust Studio: decisión IA de inicio a fin

**Precondición:** Cola Trust con al menos 1 ítem pendiente (si vacía, disparar acción autónoma desde Command/Agents o usar tenant CEO_DEMO).

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Login Admin → `/TrustInbox` | Lista de cola + métricas; badge si hay pendientes |
| 2 | Seleccionar primer ítem | Panel detalle + explicabilidad / outcome fabric |
| 3 | **Aprobar** decisión | Mensaje “aprobada y ejecutada”; ítem sale de pendientes o cambia estado |
| 4 | Si hay otro ítem: **Rechazar** con nota | Mensaje de rechazo; cola actualizada |
| 5 | Volver a Command `/` | Métricas Trust coherentes con acciones |

### FT-ADMIN-03 — Administración de usuarios

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Login Admin → `/Users` | Lista usuarios demo (≥5) |
| 2 | **Crear** usuario: `qa.agent@autonomuscrm.local`, rol Viewer, password temporal | Redirect/lista con usuario nuevo |
| 3 | **Editar** ese usuario: cambiar nombre | Cambios persistidos tras refresh |
| 4 | `/Users/Roles` — revisar roles disponibles | Admin, Manager, Sales, Support, Viewer listados |
| 5 | Logout → Login con `qa.agent@autonomuscrm.local` | Home `/` (Viewer) |
| 6 | (Opcional cleanup) Admin elimina o desactiva usuario QA | — |

### FT-ADMIN-04 — Plataforma: políticas, auditoría, integraciones

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | `/Policies` → listar | Políticas visibles |
| 2 | `/Policies/Create` → guardar política mínima de prueba | Redirect sin error |
| 3 | `/Audit` → filtrar por fecha reciente | Eventos/registros cargan |
| 4 | `/Integrations` → revisar conectores | UI de integraciones; sin 500 |
| 5 | `/Settings` → revisar comunicaciones / tenant | Página carga (Admin autorizado) |
| 6 | `/Agents` (Workforce) | Estado agentes / configuración visible |

### FT-ADMIN-05 — Pipeline comercial completo (Lead → Customer → Deal)

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | `/Leads` → **Crear** lead `FT Admin Lead {timestamp}` fuente Website | Redirect con `?created=true` o mensaje éxito |
| 2 | Abrir detalle del lead creado | Formulario detalle visible |
| 3 | **Calificar** lead | Estado actualizado en detalle |
| 4 | **Convertir a cliente** | Redirect `/Customers/Details/{id}` |
| 5 | Desde detalle lead (o deal): **Crear deal** título `FT Deal Admin`, monto `15000` | Redirect `/Deals/Details/{id}` |
| 6 | `/Deals` → abrir deal → cambiar etapa/estado si UI lo permite | Persistencia tras refresh |
| 7 | `/revenue` y `/Customer360` | Datos alineados con nuevo deal/cliente |

---

## 5. Manager — flujos completos

### FT-MANAGER-01 — Supervisión ejecutiva + configuración

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Login `manager@autonomuscrm.local` / `Manager123!` | `/executive` |
| 2 | Revisar Executive OS (mismo checklist que FT-ADMIN-01 pasos 2–5) | OK |
| 3 | `/Settings` | Acceso permitido (Manager ∈ Admin,Manager) |
| 4 | Ajustar umbral Trust en `/TrustInbox` (slider POST threshold) | Mensaje “Umbral actualizado…” |
| 5 | `/Users` → listar (no obligatorio crear) | Acceso OK |

### FT-MANAGER-02 — Operación comercial sin borrado masivo

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | `/Leads` → crear lead `FT Manager Lead` | Éxito |
| 2 | Calificar + convertir a customer | Customer creado |
| 3 | `/Deals/Create` o desde lead: deal `FT Manager Deal` | Deal en pipeline |
| 4 | `/Workflows` → listar; abrir workflow existente en **Edit** | Formulario carga |
| 5 | `/customer-success` | Vista CS sin error |

### FT-MANAGER-03 — Negativa: no debe romper tenant

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Intentar `/Users/Import` con CSV inválido (si UI existe) | Error controlado, no 500 |
| 2 | Logout → FT-COMMON-02 con Manager | OK |

---

## 6. Sales — flujos completos

### FT-SALES-01 — Día tipo ventas (Revenue OS → cierre)

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Login `sales@autonomuscrm.local` / `Sales123!` | `/revenue` |
| 2 | Revisar forecast, pipeline, NBA/recomendaciones | UI Revenue OS completa |
| 3 | `/Deals` → abrir **Implementación CRM Q1** | Detalle deal demo |
| 4 | `/Leads` → crear `FT Sales Lead` | Éxito |
| 5 | Detalle lead → **Crear deal** desde lead | Deal nuevo en `/Deals/Details` |
| 6 | `/Customers` → abrir **Corporación Alpha** | Detalle cliente |
| 7 | `/Customer360` → buscar Alpha → **Detail** | Vista 360 con tabs/secciones |
| 8 | Logout | OK |

### FT-SALES-02 — Lead enrichment y eliminación

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Crear lead temporal `FT Sales Delete Me` | Creado |
| 2 | Detalle → **Eliminar** lead | Vuelta a lista sin el registro |
| 3 | Lista `/Leads` filtrar por fuente Website | Filtros aplican |

### FT-SALES-03 — Negativas de rol

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Navegar directo a `/Users` | Access Denied o redirect autorización |
| 2 | Navegar directo a `/Settings` | Access Denied |
| 3 | `/Leads/Edit` de lead ajeno (ID inválido) | Mensaje “no encontrado” o redirect seguro |

---

## 7. Support — flujos completos

### FT-SUPPORT-01 — Customer Success de inicio a fin

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Login `support@autonomuscrm.local` / `Support123!` | `/Customer360` |
| 2 | Listar clientes → entrar **Corporación Alpha** | Detail 360 carga (explain, memory, health) |
| 3 | `/customer-success` | Métricas CS / comunicaciones |
| 4 | `/Customers` → solo lectura: abrir detalle, **sin** botones crear guardando | Vista OK |
| 5 | `/VoiceCalls` | Listado/logs de voz |
| 6 | `/Tasks` | Tareas operativas visibles |

### FT-SUPPORT-02 — Trust y Command (solo lectura operativa)

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | `/TrustInbox` → revisar cola | Puede aprobar/rechazar si hay ítems (mismo que Admin) |
| 2 | `/` Command | KPIs visibles |
| 3 | `/Memory` | Business memory accesible |

### FT-SUPPORT-03 — Negativas escritura comercial

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | GET `/Leads/Create` | Redirect `/Account/AccessDenied` |
| 2 | GET `/Customers/Create` | Access Denied |
| 3 | GET `/Deals/Create` | Access Denied |
| 4 | POST crear lead (forzar formulario si agente usa DevTools) | Access Denied |
| 5 | `/Leads` lista | **Sí** puede ver lista y detalle GET |

---

## 8. Viewer — flujos completos

### FT-VIEWER-01 — Consulta transversal solo lectura

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Login `viewer@autonomuscrm.local` / `Viewer123!` | `/` Command |
| 2 | Recorrer: Executive, Revenue, Deals, Customers, Customer360, Memory, Audit | Todas cargan en lectura |
| 3 | `/Deals/Details` deal demo | Sin controles de escritura o deshabilitados |
| 4 | `/billing` | Vista billing lectura |
| 5 | Logout | OK |

### FT-VIEWER-02 — Barrera de escritura (todas las rutas comerciales)

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | `/Leads/Create`, `/Customers/Edit/{id}`, `/Deals/Create` | Access Denied |
| 2 | `/Users`, `/Settings` | Access Denied |
| 3 | `/Policies/Create` | Access Denied |

### FT-VIEWER-03 — Paleta y navegación rápida

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | `Ctrl+K` → ir a Trust Studio | Navegación OK |
| 2 | Cambiar tema claro/oscuro | UI persiste preferencia en sesión |

---

## 9. Pruebas cruzadas (multi-rol)

### FT-CROSS-01 — Mismo dato, distintos ojos

| Paso | Rol | Acción | Resultado esperado |
|------|-----|--------|-------------------|
| 1 | Sales | Crear lead `FT Cross Role` | Existe en lista |
| 2 | Viewer | `/Leads` buscar ese lead | Visible, sin editar |
| 3 | Support | Detalle lead | Visible, sin POST calificar |
| 4 | Admin | Calificar y convertir | Customer creado |
| 5 | Manager | `/Customer360` ver customer | Datos consistentes |

### FT-CROSS-02 — Aislamiento de sesión

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Login Sales en navegador A | Home `/revenue` |
| 2 | Login Admin en navegador B (incógnito) | Home `/executive` |
| 3 | Logout solo en A | B sigue autenticado como Admin |

### FT-CROSS-03 — Tenant CEO_DEMO (si existe en selector login)

| Paso | Acción | Resultado esperado |
|------|--------|-------------------|
| 1 | Login Admin eligiendo tenant **CEO_DEMO** | Datos CEO demo, no mezclados con Demo |
| 2 | `/executive` | KPIs del tenant CEO |
| 3 | Volver a tenant **AutonomusCRM Demo** | Datos demo estándar |

---

## 10. APIs y rendimiento percibido (agente opcional)

| ID | Ruta | Método | Rol | Criterio |
|----|------|--------|-----|----------|
| FT-API-01 | `/health` | GET | — | 200 + `Healthy` |
| FT-API-02 | `/health/ready` | GET | — | 200 cuando DB up |
| FT-PERF-01 | `/Customer360/Detail?id={alpha}` | GET | Admin | TTFB &lt; 5s en VPS; sin timeout |
| FT-PERF-02 | `/executive` | GET | Manager | Sin error 500 tras cold start |

---

## 11. Plantilla de reporte del agente

```markdown
## Ejecución: {fecha} — {entorno URL}

| ID caso | Rol | Estado | Notas |
|---------|-----|--------|-------|
| FT-ADMIN-01 | Admin | PASS/FAIL | |
| ... | | | |

### Incidencias
- **ID:** FT-SALES-03 paso 1
- **Esperado:** Access Denied
- **Obtenido:** ...
- **Captura:** ...
```

---

## 12. Criterios globales de salida (Go / No-Go)

- [ ] Los 5 roles completan **FT-COMMON-02** sin error 500  
- [ ] **FT-ADMIN-05** y **FT-SALES-01** demuestran ciclo Lead→Deal  
- [ ] **FT-SUPPORT-03** y **FT-VIEWER-02** confirman bloqueo escritura comercial  
- [ ] Trust: al menos una aprobación o rechazo exitoso (**FT-ADMIN-02**)  
- [ ] Usuarios: Admin/Manager crean usuario (**FT-ADMIN-03**)  
- [ ] Sin regresión login/logout en los 5 perfiles  

---

## Referencias en código

- Roles y passwords: `AutonomusCRM.Infrastructure/Persistence/Seed/DemoRoleUsers.cs`  
- Home por rol: `AutonomusCRM.API/Infrastructure/RoleHomeRedirect.cs`  
- Escritura comercial: `AutonomusCRM.API/Middleware/CommercialWriteAuthorizationMiddleware.cs`  
- Conversión lead: `AutonomusCRM.API/Pages/Leads/Details.cshtml.cs`  
