# Matriz de roles y permisos — AutonomusFlow

| Fuente | `ANALISIS_PREMIUM_PROCESOS_AUTONOMUSFLOW.md` §5 |
| Fecha | 2026-05-27 |
| Modo | Diseño QA — validar en Fase 2 |

---

## 1. Roles del sistema

| Rol | Perfil | Objetivo de negocio |
|-----|--------|---------------------|
| **Admin** | TI / dueño tenant | Control total, usuarios, configuración |
| **Manager** | Jefe comercial | Supervisión pipeline + usuarios + settings |
| **Sales** | Ejecutivo ventas | Operación diaria Lead–Deal |
| **Support** | Soporte interno | Consulta; sin escritura comercial |
| **Viewer** | Dirección / auditoría | Solo lectura |

---

## 2. Matriz rol × funcionalidad (esperada vs implementada)

**Leyenda:** ✅ Permitido | ❌ Denegado | ⚐ Parcial / riesgo | — N/A módulo

| Funcionalidad | Ruta / acción | Admin | Manager | Sales | Support | Viewer | Mecanismo | Casos QA |
|---------------|---------------|:-----:|:-------:|:-----:|:-------:|:------:|-----------|----------|
| **Login** | `/Account/Login` | ✅ | ✅ | ✅ | ✅ | ✅ | Anónimo | AUTH-* |
| **Dashboard** | `/` GET | ✅ | ✅ | ✅ | ✅ | ✅ | Auth | NAV-001 |
| **Leads listar** | `/Leads` GET | ✅ | ✅ | ✅ | ✅ | ✅ | Auth | ROL-L-01 |
| **Lead crear** | `/Leads/Create` POST | ✅ | ✅ | ✅ | ❌ | ❌ | Middleware + handler | SEC-L-01 |
| **Lead calificar** | Details POST Qualify | ✅ | ✅ | ✅ | ❌ | ❌ | `[Authorize]` + middleware | SEC-L-02 |
| **Lead convertir** | Details POST Convert | ✅ | ✅ | ✅ | ❌ | ❌ | Idem | E2E-001 |
| **Customers CRUD** | POST `/Customers/*` | ✅ | ✅ | ✅ | ❌ | ❌ | Middleware | SEC-C-01 |
| **Deals CRUD** | POST `/Deals/*` | ✅ | ✅ | ✅ | ❌ | ❌ | Middleware | SEC-D-01 |
| **Deals cerrar** | Details POST Close | ✅ | ✅ | ✅ | ❌ | ❌ | Handler | PROC-D-05 |
| **Import leads** | `/Leads/Import` POST | ✅ | ✅ | ✅ | ❌ | ❌ | Middleware | IMP-001 |
| **Users listar** | `/Users` GET | ✅ | ✅ | ⚐ | ❌ | ❌ | `[Authorize Admin,Manager]` | SEC-U-01 |
| **User crear** | `/Users/Create` POST | ✅ | ✅ | ❌ | ❌ | ❌ | Page authorize | USR-001 |
| **Settings** | `/Settings` | ✅ | ✅ | ❌* | ❌ | ❌ | `[Authorize Admin,Manager]` | SEC-S-01 |
| **Workflows POST** | `/Workflows/*` | ✅ | ✅ | ✅ | ❌ | ❌ | Middleware | AUT-WF-01 |
| **Policies POST** | `/Policies/*` | ✅ | ✅ | ✅ | ❌ | ❌ | Middleware | AUT-POL-01 |
| **Agents config** | `/Agents` POST | ✅ | ✅ | ⚐ | ⚐ | ⚐ | Auth only | AUT-AG-01 |
| **Audit ver** | `/Audit` GET | ✅ | ✅ | ✅ | ✅ | ✅ | Auth | TRZ-001 |
| **Support health** | `/Support` | ✅ | ✅ | ✅ | ✅ | ✅ | Auth | OPS-001 |
| **API crear user** | `POST api/Users` | ✅ | ❌ | ❌ | ❌ | ❌ | RequireAdmin | API-U-01 |
| **API JWT lead** | `POST api/Leads` | ✅ | ✅ | ✅ | ⚐ | ⚐ | JWT + tenant query | API-L-01 |
| **Tareas** | — | — | — | — | — | — | **No existe** | N/A |
| **Contactos** | — | — | — | — | — | — | **No existe** | N/A |

\*Sales no tiene rol en Settings; acceso denegado por página.

---

## 3. Matriz rol × proceso de negocio

| Proceso | Admin | Manager | Sales | Support | Viewer |
|---------|:-----:|:-------:|:-----:|:-------:|:------:|
| P01 Login / navegación | Ejecuta | Ejecuta | Ejecuta | Ejecuta | Ejecuta |
| P02 Usuarios y roles | **Dueño** | **Gestiona** | No | No | No |
| P05 Gestión leads | Opcional | Supervisa | **Opera** | Solo ve | Solo ve |
| P06 Conversión | Opcional | Supervisa | **Opera** | No | No |
| P07 Pipeline deals | Opcional | **Supervisa** | **Opera** | Solo ve | Solo ve |
| P09 Automatización | Configura | Configura | Configura defs | Ve | Ve |
| P11 Reportes | Dashboard | Dashboard | Dashboard | Dashboard | Dashboard |
| P13 Auditoría | Export | Export | Ve (vacío) | Ve | Ve |
| P14 Día operativo Sales | — | Supervisa | **Principal** | — | — |
| P15 Día Admin | **Principal** | Parcial | — | — | — |

---

## 4. Escenarios RBAC obligatorios (resumen casos)

| ID | Escenario | Rol | Resultado esperado |
|----|-----------|-----|-------------------|
| SEC-V-01 | Viewer POST crear lead | Viewer | Bloqueo (middleware 403 o sin efecto) |
| SEC-S-01 | Support POST editar deal | Support | Bloqueo |
| SEC-S-02 | Sales GET `/Users` | Sales | 403 o redirect AccessDenied |
| SEC-M-01 | Manager crea usuario Sales | Manager | Usuario creado |
| SEC-A-01 | Admin POST Settings | Admin | Guardado OK |
| SEC-D-01 | Viewer URL directa `/Leads/Edit/{id}` POST | Viewer | Bloqueo |

---

## 5. Brechas de permisos documentadas (análisis premium)

| ID | Brecha | Riesgo | Caso que lo evidencia |
|----|--------|--------|------------------------|
| B15 | `SameTenantHandler` no compara recurso | Cross-tenant API | TEN-003, TEN-004 |
| — | Viewer antes podía POST (mitigado) | Medio | SEC-V-01 debe PASS con middleware |
| B13 | Botón “Gestionar roles” alert | UX confusión | UX-U-01 |
| — | Matriz permisos UI decorativa en Users | Falsa confianza | UX-U-02 |

---

## 6. Criterios de aceptación matriz (Fase 2)

| Criterio | Umbral |
|----------|--------|
| Todos los SEC-* P0 | PASS |
| Desviaciones documentadas | Con ticket y severidad |
| Sin FAIL en Sales flujo dorado | 0 |

---

*Validar celdas ⚐ en ejecución y actualizar columna “Observado Fase 2”.*
