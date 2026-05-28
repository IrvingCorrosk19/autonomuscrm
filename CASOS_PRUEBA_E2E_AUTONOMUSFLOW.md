# Casos de prueba E2E operacionales — AutonomusFlow

| Campo | Valor |
|-------|-------|
| **Fuente** | `ANALISIS_PREMIUM_PROCESOS_AUTONOMUSFLOW.md` |
| **Plan** | `PLAN_MAESTRO_PRUEBAS_OPERACIONALES_AUTONOMUSFLOW.md` |
| **Empresa simulada** | TechNova Solutions |
| **URL base** | `http://localhost:5154` |
| **Estado global corrida** | **PENDIENTE** (Fase 1 — solo documentación) |
| **Total casos** | **118** |

---

## Convenciones

| Campo | Valores |
|-------|---------|
| **Estado** | `PENDIENTE` \| `PASS` \| `FAIL` \| `BLOCKED` \| `SKIP` |
| **Resultado observado** | *(vacío hasta Fase 2)* |
| **Prioridad** | P0 \| P1 \| P2 \| P3 |
| **Severidad** | S1 Crítica … S4 Baja |
| **GAP** | Funcionalidad inexistente según análisis — no es FAIL de regresión |
| **EXPECT-FAIL** | Se anticipa FAIL por brecha conocida (B06, etc.) |

**Passwords:** `{Rol}123!` — emails `{rol}@autonomuscrm.local`

---

## Índice por categoría

| Cat. | Código | Casos | P0 |
|------|--------|:-----:|:--:|
| Auth / Login | AUTH, SEC-SES | 8 | 5 |
| Navegación | NAV | 5 | 2 |
| Por rol | ROL | 15 | 6 |
| Por proceso | PROC | 22 | 8 |
| E2E compuestos | E2E | 6 | 3 |
| Negativos | NEG | 10 | 4 |
| Error humano | HUM | 8 | 2 |
| Multiusuario | MULTI | 6 | 2 |
| Concurrencia | CONC | 4 | 1 |
| Seguridad | SEC | 14 | 8 |
| Multi-tenant | TEN | 6 | 3 |
| Trazabilidad | TRZ | 8 | 3 |
| Recuperación | REC | 6 | 2 |
| Datos corruptos | DAT | 6 | 2 |
| Automatización | AUT | 10 | 2 |
| UX no técnico | UX | 8 | 2 |

---

# PARTE A — Casos detallados (formato completo)

---

## E2E-001 — Vendedor convierte lead en cliente y cierra deal (flujo dorado)

| Campo | Valor |
|-------|-------|
| **ID** | E2E-001 |
| **Nombre** | Flujo comercial completo Sales — TechNova |
| **Objetivo** | Validar el proceso operativo diario de un ejecutivo de ventas de punta a punta |
| **Rol** | Sales |
| **Precondiciones** | API + BD; seed activo; usuario `sales@autonomuscrm.local` activo |
| **Datos de prueba** | Lead: Diego Ramírez, Finanzas del Istmo SA, diego.r@finanzasistmo.com, +50760001122, Website; Deal: "Implementación CRM Q1", USD 25000 |
| **Flujo paso a paso** | 1) Login 2) Dashboard `/` 3) `/Leads/Create` crear lead 4) `/Leads/Details/{id}` → Calificar 5) Convertir a cliente 6) Crear deal desde modal 7) `/Deals/Details/{id}` cambiar etapa a Proposal, prob. 60% 8) Cerrar deal ganado |
| **Resultado esperado** | Lead Qualified→Converted; cliente visible; deal ClosedWon; KPIs dashboard actualizados; eventos en BD `DomainEvents` |
| **Resultado observado** | *(pendiente)* |
| **Evidencia requerida** | 8 capturas; export opcional DomainEvents por SQL; URLs finales |
| **Severidad** | S2 si falla |
| **Prioridad** | **P0** |
| **Riesgo** | R05 (sin tareas post-cierre) |
| **Dependencias** | AUTH-004 |
| **Estado** | PENDIENTE |

---

## E2E-002 — Manager supervisa pipeline sin editar datos

| Campo | Valor |
|-------|-------|
| **ID** | E2E-002 |
| **Objetivo** | Validar rol supervisor: visibilidad sin alteración indebida |
| **Rol** | Manager |
| **Precondiciones** | Deals existentes de corrida E2E-001 o seed |
| **Datos** | `manager@autonomuscrm.local` |
| **Flujo** | Login → `/` pipeline → `/Deals` filtros → `/Leads` solo lectura → intento POST editar deal ajeno (opcional) → `/Users` crear usuario temporal |
| **Resultado esperado** | Ve pipeline; puede gestionar usuarios; puede editar comercial si política negocio lo permite |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Capturas pipeline + Users |
| **Severidad** | S3 |
| **Prioridad** | P1 |
| **Riesgo** | — |
| **Dependencias** | AUTH-002 |
| **Estado** | PENDIENTE |

---

## SEC-V-01 — Viewer intenta crear lead (debe bloquearse)

| Campo | Valor |
|-------|-------|
| **ID** | SEC-V-01 |
| **Objetivo** | Verificar que rol lectura no altera comercial |
| **Rol** | Viewer |
| **Precondiciones** | Middleware comercial activo (`CommercialWriteAuthorizationMiddleware`) |
| **Datos** | `viewer@autonomuscrm.local` |
| **Flujo** | Login → `/Leads/Create` GET (formulario puede cargar) → POST crear lead con datos válidos (Burp o form) |
| **Resultado esperado** | POST rechazado: 403 / AccessDenied / sin persistencia en BD |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Respuesta HTTP; COUNT leads antes/después |
| **Severidad** | S1 si persiste |
| **Prioridad** | **P0** |
| **Riesgo** | R02 |
| **Dependencias** | AUTH-005 |
| **Estado** | PENDIENTE |

---

## SEC-S-01 — Support intenta POST en Deals

| Campo | Valor |
|-------|-------|
| **ID** | SEC-S-01 |
| **Objetivo** | Support solo consulta |
| **Rol** | Support |
| **Flujo** | Login → `/Deals/Details/{id}` → POST UpdateStage |
| **Resultado esperado** | Bloqueo middleware; etapa sin cambio |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Captura + BD |
| **Severidad** | S1 |
| **Prioridad** | **P0** |
| **Riesgo** | — |
| **Dependencias** | AUTH-003 |
| **Estado** | PENDIENTE |

---

## TRZ-001 — Auditoría refleja acciones reales (EXPECT-FAIL conocido)

| Campo | Valor |
|-------|-------|
| **ID** | TRZ-001 |
| **Objetivo** | Tras E2E-001, usuario Admin ve eventos en `/Audit` |
| **Rol** | Admin |
| **Precondiciones** | E2E-001 ejecutado; eventos en tabla `DomainEvents` |
| **Flujo** | Login Admin → `/Audit` → filtrar por fecha hoy → ver lista |
| **Resultado esperado (negocio)** | Lista con LeadCreated, LeadQualified, DealCreated, etc. |
| **Resultado esperado (análisis premium)** | **Lista vacía o incompleta** (B06 deserialización) |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Captura Audit + `SELECT COUNT(*) FROM "DomainEvents"` + export POST |
| **Severidad** | S2 (compliance) |
| **Prioridad** | **P0** |
| **Riesgo** | R01, R12, R17 |
| **Dependencias** | E2E-001 |
| **Estado** | PENDIENTE — marcar **EXPECT-FAIL** hasta fix B06 |

---

## AUT-AG-002 — Agente IA “activo” sin Worker (escenario real riesgo)

| Campo | Valor |
|-------|-------|
| **ID** | AUT-AG-002 |
| **Objetivo** | Detectar desconexión UI vs Worker |
| **Rol** | Admin |
| **Precondiciones** | Solo API corriendo; **Workers detenido**; EventBus InMemory |
| **Flujo** | Login → `/Agents` ver estado → crear lead → volver Agents / revisar score lead |
| **Resultado esperado (usuario cree)** | Score actualizado por IA |
| **Resultado esperado (real)** | UI puede mostrar activo; score **sin cambio** o sin agente |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Captura Agents + lead score antes/después + logs Worker ausentes |
| **Severidad** | S3 |
| **Prioridad** | P1 |
| **Riesgo** | R04, R10 |
| **Dependencias** | PROC-L-01 |
| **Estado** | PENDIENTE |

---

## AUT-WF-004 — Workflow se dispara sin efecto (EXPECT-FAIL parcial)

| Campo | Valor |
|-------|-------|
| **ID** | AUT-WF-004 |
| **Objetivo** | Validar expectativa: workflow cuenta ejecución pero no cambia datos |
| **Rol** | Admin |
| **Precondiciones** | Workflow activo trigger `LeadCreatedEvent`, acción `UpdateStatus` |
| **Flujo** | Crear workflow en `/Workflows/Create` → crear lead → verificar estado lead |
| **Resultado esperado (análisis)** | Execution count++ ; **estado lead sin cambio automático** (B03) |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Workflow edit execution count; estado lead |
| **Severidad** | S3 |
| **Prioridad** | P1 |
| **Riesgo** | R03 |
| **Dependencias** | AUT-WF-001 |
| **Estado** | PENDIENTE |

---

## CONC-001 — Dos Sales editan mismo deal simultáneamente

| Campo | Valor |
|-------|-------|
| **ID** | CONC-001 |
| **Objetivo** | Validar comportamiento concurrencia (último write gana) |
| **Rol** | Sales (2 sesiones: sales + manager o 2 browsers) |
| **Precondiciones** | Mismo dealId |
| **Flujo** | Browser A: probabilidad 40% → guardar; Browser B: probabilidad 80% → guardar inmediato después |
| **Resultado esperado** | Un valor final coherente (80% si B último); sin error 500; sin mensaje conflicto |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Capturas ambos + estado final |
| **Severidad** | S3 |
| **Prioridad** | P2 |
| **Riesgo** | R08 |
| **Dependencias** | PROC-D-01 |
| **Estado** | PENDIENTE |

---

## TEN-003 — Usuario intenta acceder datos otro tenant vía API

| Campo | Valor |
|-------|-------|
| **ID** | TEN-003 |
| **Objetivo** | Aislamiento tenant en API |
| **Rol** | Admin tenant A + token |
| **Precondiciones** | **2 tenants** en BD (crear QA-Tenant-B si no existe); lead en tenant B |
| **Flujo** | JWT/login tenant A → `GET api/Customers/{id}` con id de tenant B y tenantId=A en query |
| **Resultado esperado** | 404 o 403 — sin datos tenant B |
| **Resultado esperado (análisis)** | **Riesgo FAIL** por B15 SameTenant incompleto |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Response body |
| **Severidad** | S1 |
| **Prioridad** | **P0** |
| **Riesgo** | R02 |
| **Dependencias** | TEN-002 (setup 2 tenants) |
| **Estado** | PENDIENTE |

---

## DAT-001 — Importación CSV leads inválidos

| Campo | Valor |
|-------|-------|
| **ID** | DAT-001 |
| **Objetivo** | Robustez importación |
| **Rol** | Sales |
| **Datos** | `qa-leads-invalid.csv` (nombre vacío, email "no-email") |
| **Flujo** | `/Leads` → Import modal → subir archivo → submit |
| **Resultado esperado** | Rechazo parcial o mensaje error; no filas corruptas en listado; o skip con resumen |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Archivo + mensaje UI + COUNT leads |
| **Severidad** | S3 |
| **Prioridad** | P1 |
| **Riesgo** | R07 |
| **Dependencias** | IMP-001 |
| **Estado** | PENDIENTE |

---

## UX-001 — Usuario no técnico entiende “Lead” sin capacitación

| Campo | Valor |
|-------|-------|
| **ID** | UX-001 |
| **Objetivo** | Heurística comprensión |
| **Rol** | Sales (tester actúa como usuario novel) |
| **Flujo** | Login fresco → seguir solo menú → intentar “registrar prospecto” sin saber que es Leads |
| **Resultado esperado** | Menú “Leads” + subtítulo ayudan; completar tarea en <3 min |
| **Criterio fallo** | No encuentra dónde crear prospecto en 5 min |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Nota tiempo + fricciones |
| **Severidad** | S4 |
| **Prioridad** | P2 |
| **Riesgo** | vocabulario CRM |
| **Dependencias** | NAV-001 |
| **Estado** | PENDIENTE |

---

## NEG-AUTH-01 — Login contraseña incorrecta

| Campo | Valor |
|-------|-------|
| **ID** | NEG-AUTH-01 |
| **Objetivo** | Rechazo credenciales inválidas |
| **Rol** | N/A |
| **Flujo** | `/Account/Login` email válido, password wrong |
| **Resultado esperado** | Permanece en login; mensaje error; sin cookie |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Captura |
| **Severidad** | S2 |
| **Prioridad** | **P0** |
| **Riesgo** | — |
| **Dependencias** | — |
| **Estado** | PENDIENTE |

---

## API-001 — JWT inválido en endpoint protegido

| Campo | Valor |
|-------|-------|
| **ID** | API-001 |
| **Objetivo** | Seguridad API |
| **Rol** | N/A |
| **Flujo** | `GET /api/Leads?tenantId={guid}` header `Authorization: Bearer invalid` |
| **Resultado esperado** | 401 Unauthorized |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | curl output |
| **Severidad** | S1 |
| **Prioridad** | **P0** |
| **Riesgo** | — |
| **Dependencias** | — |
| **Estado** | PENDIENTE |

---

## HUM-001 — Error humano: olvidar calificar lead antes de convertir

| Campo | Valor |
|-------|-------|
| **ID** | HUM-001 |
| **Objetivo** | Sistema permite o advierte conversión desde New |
| **Rol** | Sales |
| **Flujo** | Crear lead New → Details → Convertir sin calificar |
| **Resultado esperado** | Conversión exitosa (regla actual) O advertencia UX |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Estado final lead/customer |
| **Severidad** | S4 |
| **Prioridad** | P2 |
| **Riesgo** | calidad datos |
| **Dependencias** | — |
| **Estado** | PENDIENTE |

---

## REC-001 — Lead eliminado: navegación a URL antigua

| Campo | Valor |
|-------|-------|
| **ID** | REC-001 |
| **Objetivo** | Recuperación ante 404 lógico |
| **Rol** | Sales |
| **Flujo** | Crear lead → eliminar en Details → pegar URL `/Leads/Details/{id}` |
| **Resultado esperado** | Redirect `/Leads` + TempData error (REM-003 pattern) |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Captura |
| **Severidad** | S3 |
| **Prioridad** | P1 |
| **Riesgo** | — |
| **Dependencias** | PROC-L-DEL |
| **Estado** | PENDIENTE |

---

## PROC-GAP-01 — Gestión de contactos (NO EXISTE)

| Campo | Valor |
|-------|-------|
| **ID** | PROC-GAP-01 |
| **Objetivo** | Confirmar ausencia módulo |
| **Rol** | Sales |
| **Flujo** | Buscar en menú “Contactos”; buscar ruta `/Contacts` |
| **Resultado esperado** | **GAP** — no existe (B01) |
| **Resultado observado** | *(pendiente)* |
| **Evidencia** | Captura menú |
| **Severidad** | N/A |
| **Prioridad** | P3 |
| **Riesgo** | R15 |
| **Dependencias** | — |
| **Estado** | **SKIP** — GAP documentado |

---

## PROC-GAP-02 — Crear tarea de seguimiento (NO EXISTE)

| Campo | Valor |
|-------|-------|
| **ID** | PROC-GAP-02 |
| **Objetivo** | Confirmar ausencia tareas |
| **Rol** | Sales |
| **Flujo** | Tras crear lead, buscar “nueva tarea” / “recordatorio” |
| **Resultado esperado** | **GAP** B02 |
| **Estado** | **SKIP** — GAP |

---

# PARTE B — Catálogo compacto (todos los campos)

> **Estado Fase 2 (2026-05-27):** P0 ejecutados — ver `RESULTADOS_EJECUCION_AUTONOMUSFLOW.md` | Resto: PENDIENTE o PASS en regresión local

## B.1 Autenticación y sesión (AUTH, SEC-SES)

| ID | Nombre | Objetivo | Rol | Prioridad | Sev. | Riesgo | Dep. |
|----|--------|---------|-----|:---------:|:----:|--------|------|
| AUTH-001 | Login Admin | Acceso admin | Admin | P0 | S2 | — | — |
| AUTH-002 | Login Manager | Acceso manager | Manager | P0 | S2 | — | — |
| AUTH-003 | Login Support | Acceso support | Support | P0 | S2 | — | — |
| AUTH-004 | Login Sales | Acceso sales | Sales | P0 | S2 | — | — |
| AUTH-005 | Login Viewer | Acceso viewer | Viewer | P0 | S2 | — | — |
| AUTH-006 | Logout | Cerrar sesión | Sales | P0 | S3 | R19 | AUTH-004 |
| NEG-AUTH-02 | TenantId vacío | Validación | Admin | P1 | S3 | — | — |
| SEC-SES-01 | Sesión expirada | Redirect login claro | Sales | P2 | S4 | R19 | AUTH-004 |

## B.2 Navegación (NAV)

| ID | Nombre | Rol | Prioridad | Resultado esperado |
|----|--------|-----|:---------:|-------------------|
| NAV-001 | Menú lateral todas secciones | Sales | P0 | 10 ítems cargan sin 404 |
| NAV-002 | URL directa `/Leads/Create` sin login | — | P0 | Redirect Login |
| NAV-003 | `/Dashboard` huérfano | Admin | P3 | Carga pero no en menú; datos estáticos |
| NAV-004 | Breadcrumb mental Index | Manager | P2 | `/` = dashboard real |
| NAV-005 | AccessDenied `/Settings` Sales | Sales | P0 | `/Account/AccessDenied` |

## B.3 Por rol (ROL)

| ID | Rol | Escenario operacional | Prioridad |
|----|-----|----------------------|:---------:|
| ROL-A-01 | Admin | Día completo: users + settings + agents | P1 |
| ROL-M-01 | Manager | Supervisa pipeline + crea Sales | P0 |
| ROL-M-02 | Manager | Export audit | P1 |
| ROL-S-01 | Sales | Solo módulos comerciales | P0 |
| ROL-S-02 | Sales | No accede `/Users` | P0 |
| ROL-SP-01 | Support | Ve leads/deals, POST bloqueado | P0 |
| ROL-V-01 | Viewer | Dashboard solo lectura | P1 |
| ROL-V-02 | Viewer | BulkActions POST bloqueado | P0 |

## B.4 Procesos (PROC)

| ID | Proceso | Rol | P | Resultado esperado resumido |
|----|---------|-----|:-:|----------------------------|
| PROC-L-01 | Crear lead | Sales | P0 | Lead en lista New |
| PROC-L-02 | Editar lead | Sales | P0 | Cambios persistidos |
| PROC-L-03 | Calificar | Sales | P0 | Status Qualified |
| PROC-L-04 | Bulk status | Manager | P1 | N leads actualizados |
| PROC-L-05 | Export JSON leads | Sales | P2 | Archivo descarga |
| PROC-C-01 | Crear cliente | Sales | P0 | Customer en lista |
| PROC-C-02 | Record contact | Sales | P1 | LastContactAt actualizado |
| PROC-C-03 | Segmentar btn | Sales | P2 | **EXPECT-FAIL alert** B13 |
| PROC-D-01 | Crear deal | Sales | P0 | Deal Open |
| PROC-D-02 | Cambiar etapa | Sales | P0 | Stage persisted |
| PROC-D-03 | Cambiar probabilidad | Sales | P0 | % persisted |
| PROC-D-04 | Perder deal | Sales | P1 | ClosedLost |
| PROC-D-05 | Cerrar ganado | Sales | P0 | ClosedWon |
| PROC-D-06 | Simular escenarios btn | Manager | P3 | **EXPECT-FAIL alert** |
| PROC-U-01 | Crear usuario | Manager | P0 | User activo |
| PROC-U-02 | Editar roles user | Admin | P1 | Roles en BD |
| PROC-WF-01 | CRUD workflow | Admin | P1 | Guardado JSON |
| PROC-POL-01 | CRUD policy | Admin | P1 | Guardado |
| PROC-SET-01 | Export settings JSON | Admin | P1 | Archivo válido |
| PROC-SUP-01 | Health support | Support | P1 | DB/EventBus Healthy |
| PROC-GAP-01 | Contactos | — | — | **SKIP GAP** |
| PROC-GAP-02 | Tareas | — | — | **SKIP GAP** |

## B.5 Importación (IMP, DAT)

| ID | Tipo | Prioridad |
|----|------|:---------:|
| IMP-001 | CSV leads válido 5 filas | P0 |
| IMP-002 | JSON customers | P1 |
| IMP-003 | Deals import | P1 |
| DAT-001 | CSV inválido | P1 |
| DAT-002 | CSV vacío | P2 |
| DAT-003 | JSON malformado | P1 |

## B.6 Seguridad API (API, SEC)

| ID | Escenario | P |
|----|-----------|:-:|
| API-001 | JWT inválido | P0 |
| API-002 | Login API + usar token | P1 |
| API-003 | GET lead stub | P2 |
| API-004 | POST qualify API | P1 |
| API-U-01 | Sales POST api/Users | P0 |
| SEC-URL-01 | POST directo sin antiforgery | P1 |

## B.7 Multi-tenant (TEN)

| ID | Escenario | P |
|----|-----------|:-:|
| TEN-001 | Claim tenant en cookie post-login | P0 |
| TEN-002 | Crear 2º tenant QA | P1 |
| TEN-003 | API cross-tenant | P0 |
| TEN-004 | UI no muestra tenant ajeno | P1 |

## B.8 Automatización (AUT)

| ID | Escenario | P | Nota |
|----|-----------|:-:|------|
| AUT-AG-001 | Guardar config agents | P1 | |
| AUT-AG-002 | Activo sin Worker | P1 | EXPECT-FAIL parcial |
| AUT-AG-005 | Con Worker+RabbitMQ score | P1 | BLOCKED sin infra |
| AUT-WF-004 | Workflow sin acción | P1 | EXPECT-FAIL B03 |
| AUT-POL-002 | Policy sin efecto | P2 | B04 |
| AUT-COM-001 | Email no enviado | P2 | **SKIP GAP** B09 |

## B.9 UX placeholder (UX)

| ID | Pantalla | Botón | P |
|----|----------|-------|:-:|
| UX-U-01 | Users | Gestionar roles | P2 |
| UX-D-01 | Deals | Aprobar acciones | P3 |
| UX-A-01 | Audit | Generar reporte | P2 |
| UX-T-01 | Login | Tenant ID manual | P2 |

## B.10 Multiusuario (MULTI)

| ID | Escenario | P |
|----|-----------|:-:|
| MULTI-01 | Sales crea; Manager ve | P1 |
| MULTI-02 | Admin desactiva user; login falla | P1 |
| MULTI-03 | Dos imports paralelos | P2 |

---

# PARTE C — Matriz proceso × casos

| Proceso obligatorio | Casos principales |
|--------------------|-------------------|
| Login | AUTH-001–006, NEG-AUTH-* |
| Navegación | NAV-* |
| Leads | PROC-L-*, E2E-001, DAT-*, IMP-001 |
| Conversión | E2E-001, HUM-001 |
| Deals | PROC-D-*, CONC-001 |
| Importación | IMP-*, DAT-* |
| Usuarios | PROC-U-*, ROL-A-01 |
| Roles | PROC-U-02, ROL-M-01, UX-U-01 |
| Workflows | PROC-WF-01, AUT-WF-* |
| Policies | PROC-POL-01, AUT-POL-* |
| Agents | AUT-AG-* |
| Settings | PROC-SET-01 |
| Audit | TRZ-*, ROL-M-02 |
| API | API-* |
| Seguridad | SEC-*, API-001 |
| Multi-tenant | TEN-* |

---

# PARTE D — Resumen ejecución (actualizar Fase 2)

| Prioridad | Total | PENDIENTE | PASS | FAIL | BLOCKED | SKIP |
|:---------:|:-----:|:---------:|:----:|:----:|:-------:|:----:|
| P0 | 41 | 41 | 0 | 0 | 0 | 0 |
| P1 | 38 | 38 | 0 | 0 | 0 | 0 |
| P2 | 28 | 28 | 0 | 0 | 0 | 0 |
| P3 | 11 | 11 | 0 | 0 | 0 | 0 |
| **GAP/SKIP** | 4 | — | — | — | — | 4 |

---

# PARTE E — Funcionalidades bloqueantes (referencia)

| Si FAIL en caso | Bloquea GO piloto | Bloquea GO SaaS |
|-----------------|:-----------------:|:---------------:|
| E2E-001 | **Sí** | **Sí** |
| SEC-V-01, SEC-S-01 | **Sí** | **Sí** |
| TRZ-001 | No | **Sí** |
| TEN-003 | No (1 tenant) | **Sí** |
| AUT-AG-002 | No | No (documentar) |
| PROC-GAP-01/02 | No (SKIP) | Plan producto |

---

*Catálogo listo para Fase 2. Actualizar **Resultado observado** y **Estado** en cada corrida. No inventar PASS sin evidencia.*
