# Plan de pruebas funcionales E2E — AutonomusCRM

> **Basado en:** `ANALISIS_FUNCIONAL_COMPLETO_APLICACION.md`  
> **Fecha:** 2026-05-25  
> **Tipo:** Pruebas manuales (Browser Tab / usuario real) + base para automatización futura  
> **Alcance:** UI Razor Pages (flujo principal). API complementaria en anexo.

---

## 1. Objetivo

Validar que un usuario real puede completar los flujos de negocio del CRM desde el navegador: autenticación, gestión comercial (leads → clientes → deals), administración, automatización y auditoría.

Este plan está diseñado para ejecutarse **caso a caso** en Browser Tab (Cursor IDE o navegador manual), marcando PASS / FAIL / BLOCKED / SKIP.

---

## 2. Entornos de ejecución

| Entorno | URL base | Cuándo usar |
|---------|----------|-------------|
| **Local** | `http://localhost:5154` | Desarrollo, datos destructivos OK |
| **VPS preview** | `http://164.68.99.83:8091` | Demo compartida, cuidado con datos |
| **Render producción** | — | **NO usar** para E2E destructivos |

### 2.1 Pre-requisitos local

```powershell
cd c:\Proyectos\autonomuscrm
docker compose up -d postgres redis rabbitmq
dotnet run --project AutonomusCRM.API
# Opcional workers:
dotnet run --project AutonomusCRM.Workers
```

Verificar: `http://localhost:5154/Account/Login` responde 200.

### 2.2 Pre-requisitos VPS

- Stack Docker activo en `/opt/autonomuscrm/deploy`
- Tras redeploy del contenedor API: **recargar login** (antiforgery / Data Protection)

### 2.3 Navegador

- Chrome / Edge reciente
- Ventana limpia o perfil de prueba (sin cookies previas al inicio de suite)
- Resolución mínima: 1280×720 (sidebar visible)

---

## 3. Datos de prueba (seed)

### 3.1 Usuarios por rol

| Rol | Email | Contraseña |
|-----|-------|------------|
| Admin | `admin@autonomuscrm.local` | `Admin123!` |
| Manager | `manager@autonomuscrm.local` | `Manager123!` |
| Sales | `sales@autonomuscrm.local` | `Sales123!` |
| Support | `support@autonomuscrm.local` | `Support123!` |
| Viewer | `viewer@autonomuscrm.local` | `Viewer123!` |

El **Tenant ID** se precarga en `/Account/Login`. No borrarlo ni dejarlo en ceros.

### 3.2 Datos adicionales a crear durante la suite

| ID sugerido | Entidad | Valor ejemplo | Usado en casos |
|-------------|---------|---------------|----------------|
| `DATA-LEAD-01` | Lead | `Lead E2E Test`, `leade2e@test.local`, Website | E2E-L-01 … E2E-L-05 |
| `DATA-CUST-01` | Customer | `Cliente E2E SA`, `clientee2e@test.local` | E2E-C-01 … |
| `DATA-DEAL-01` | Deal | `Deal E2E Q1`, 15000 | E2E-D-01 … |
| `DATA-USER-01` | User | `e2e.user@test.local` / `E2eUser123!` | E2E-U-01 |
| `DATA-WF-01` | Workflow | `Workflow E2E` | E2E-W-01 |
| `DATA-POL-01` | Policy | `Policy E2E`, expression simple | E2E-P-01 |

### 3.3 Archivos de importación (preparar en `tests/e2e/fixtures/`)

**customers-import.csv**
```csv
Name,Email,Phone,Company
Import CSV 1,import1@test.local,+50760001001,Corp A
Import CSV 2,import2@test.local,+50760001002,Corp B
```

**leads-import.json**
```json
[
  { "Name": "Import JSON Lead", "Email": "importjson@test.local", "Source": "Website" }
]
```

---

## 4. Convenciones del plan

### 4.1 Identificadores de caso

| Prefijo | Módulo |
|---------|--------|
| `E2E-AUTH` | Autenticación |
| `E2E-DASH` | Dashboard |
| `E2E-L` | Leads |
| `E2E-C` | Customers |
| `E2E-D` | Deals |
| `E2E-U` | Users |
| `E2E-W` | Workflows |
| `E2E-P` | Policies |
| `E2E-AG` | Agents |
| `E2E-AUD` | Audit |
| `E2E-SET` | Settings |
| `E2E-SUP` | Support |
| `E2E-NAV` | Navegación |
| `E2E-SEC` | Seguridad / permisos |
| `E2E-API` | API (complemento) |

### 4.2 Prioridad

| Nivel | Significado | Orden ejecución |
|-------|-------------|-----------------|
| **P0** | Bloqueante — smoke release | 1º |
| **P1** | Core negocio | 2º |
| **P2** | Admin / automatización | 3º |
| **P3** | Complementario | 4º |

### 4.3 Estados

| Estado | Significado |
|--------|-------------|
| PASS | Resultado esperado |
| FAIL | Resultado incorrecto |
| BLOCKED | No ejecutable (entorno caído) |
| SKIP | Omitido a propósito |
| KNOWN | Comportamiento conocido distinto al ideal (documentar) |

### 4.4 Comportamiento conocido (no fallar como bug sin confirmar)

Documentado en análisis — marcar **KNOWN** si aplica:

1. **UI no restringe por rol** — Viewer puede entrar a `/Users` y `/Settings`.
2. **Tenant siempre = primer tenant de BD** — no el del JWT.
3. **Agentes IA** — UI estática; sin efecto LLM real.
4. **Errores silenciosos** — algunos catch solo redirigen sin mensaje.

---

## 5. Suite P0 — Smoke (obligatoria antes de release)

> **Duración estimada:** 45–60 min manual  
> **Rol principal:** Admin (salvo casos multi-rol indicados)

---

### E2E-AUTH-01 — Login Admin exitoso
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |
| **Rol** | Admin |
| **Precondición** | Sin sesión activa |

**Pasos:**
1. Ir a `/Account/Login`
2. Verificar Tenant ID precargado (no `00000000-...`)
3. Ver tabla demo con 5 roles (si `Seed:Enabled`)
4. Email: `admin@autonomuscrm.local`, Password: `Admin123!`
5. Clic **Entrar**

**Resultado esperado:**
- Redirect a `/` (Dashboard)
- Sidebar visible con enlaces Principal / Autonomía / Administración
- No mensaje "Credenciales inválidas"

---

### E2E-AUTH-02 — Login fallido (password incorrecta)
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |
| **Rol** | — |

**Pasos:**
1. `/Account/Login`
2. Email admin, password `WrongPass123!`
3. Entrar

**Resultado esperado:**
- Permanece en login
- Mensaje de error visible (credenciales inválidas)

---

### E2E-AUTH-03 — Login por cada rol demo
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |
| **Roles** | Admin, Manager, Sales, Support, Viewer |

**Pasos:** Repetir login con cada par email/password de sección 3.1.

**Resultado esperado:**
- Los 5 acceden al Dashboard
- Logout entre cada uno (POST sidebar o `/Account/Logout`)

---

### E2E-AUTH-04 — Logout
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |
| **Precondición** | Sesión Admin activa |

**Pasos:**
1. Clic **Cerrar sesión** en sidebar
2. Intentar abrir `/Leads`

**Resultado esperado:**
- Redirect a `/Account/Login`
- `/Leads` no accesible sin auth

---

### E2E-NAV-01 — Navegación sidebar completa
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |
| **Rol** | Admin |

**Pasos:** Desde Dashboard, clic en cada enlace del sidebar:
`/`, `/Leads`, `/Deals`, `/Customers`, `/Support`, `/Agents`, `/Workflows`, `/Policies`, `/Users`, `/Audit`, `/Settings`

**Resultado esperado:**
- Cada ruta carga HTTP 200 sin excepción visible
- Item activo resaltado en sidebar

---

### E2E-DASH-01 — Dashboard muestra métricas
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |
| **Precondición** | Seed con leads/deals |

**Pasos:**
1. Login Admin → `/`
2. Observar KPIs: total leads, deals, conversión, pipeline

**Resultado esperado:**
- `HasData` equivalente: números ≥ 0 (seed: leads ≥ 3, deals ≥ 1)
- Sin pantalla vacía de error

---

### E2E-L-01 — Crear lead desde lista
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |
| **Rol** | Sales |

**Pasos:**
1. `/Leads`
2. Crear lead inline o ir a Create: `Lead E2E Test`, email `leade2e@test.local`, source Website
3. Guardar

**Resultado esperado:**
- Lead aparece en listado
- Status inicial New (o equivalente UI)

---

### E2E-L-02 — Calificar lead
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |
| **Precondición** | E2E-L-01 completado |

**Pasos:**
1. Abrir `/Leads/Details/{id}` del lead creado
2. Acción **Calificar** / Qualify

**Resultado esperado:**
- Status → Qualified
- Permanece en details sin error

---

### E2E-L-03 — Convertir lead a cliente
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |
| **Precondición** | Lead existente |

**Pasos:**
1. Details del lead → **Convertir a cliente**

**Resultado esperado:**
- Redirect a `/Customers/Details/{customerId}`
- Cliente con datos del lead

---

### E2E-D-01 — Crear deal desde lead
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |
| **Precondición** | Lead existente |

**Pasos:**
1. `/Leads/Details/{id}`
2. Crear deal: título `Deal E2E Q1`, monto `15000`

**Resultado esperado:**
- Redirect a `/Deals/Details/{dealId}`
- Deal Open, stage inicial visible

---

### E2E-D-02 — Avanzar stage del deal
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |
| **Precondición** | Deal E2E-D-01 |

**Pasos:**
1. `/Deals/Details/{id}`
2. Cambiar stage → Qualification o Proposal
3. Guardar / actualizar

**Resultado esperado:**
- Stage actualizado en UI
- Probabilidad puede cambiar según dominio

---

### E2E-D-03 — Cerrar deal (won)
| Campo | Valor |
|-------|-------|
| **Prioridad** | P0 |

**Pasos:**
1. Details deal → **Cerrar deal** (won)
2. Volver a `/Deals` y filtrar

**Resultado esperado:**
- Deal status Closed / ClosedWon
- Dashboard revenue puede reflejar cambio (refresh `/`)

---

## 6. Suite P1 — Core CRM extendido

---

### E2E-C-01 — CRUD cliente manual
| Rol | Sales |
| **Pasos:** Create → Edit (cambiar company) → Details → verificar campos |
| **Esperado:** Datos persisten tras refresh |

### E2E-C-02 — Import CSV clientes
| **Pasos:** `/Customers` → Import → subir `customers-import.csv` |
| **Esperado:** Redirect con `imported=2`; clientes visibles en lista |

### E2E-C-03 — Bulk update status clientes
| **Pasos:** Seleccionar ≥2 clientes → bulk action updateStatus → Customer |
| **Esperado:** Redirect `bulkUpdated` ≥ 1; status cambiado en lista |

### E2E-C-04 — Crear deal desde customer details
| **Pasos:** `/Customers/Details/{id}` → crear deal |
| **Esperado:** Redirect a deal details vinculado al customer |

### E2E-L-04 — Editar lead
| **Pasos:** Edit → cambiar phone/company → guardar |
| **Esperado:** Cambios visibles en details |

### E2E-L-05 — Eliminar lead
| **Pasos:** Details → Delete → confirmar si hay diálogo |
| **Esperado:** Redirect `/Leads`; lead no en lista |

### E2E-D-04 — Editar deal (amount, probability)
| **Pasos:** `/Deals/Edit/{id}` → amount 20000, probability 60 |
| **Esperado:** Valores en details |

### E2E-D-05 — Bulk deals stage
| **Pasos:** Seleccionar deals → bulk stage Negotiation |
| **Esperado:** bulkUpdated en query string |

### E2E-DASH-02 — Dashboard refleja acciones
| **Pasos:** Tras E2E-L/D, refresh `/` |
| **Esperado:** Totals coherentes con BD (manual o SQL spot check) |

---

## 7. Suite P1 — Usuarios

### E2E-U-01 — Listar usuarios
| Rol | Admin |
| **Pasos:** `/Users` |
| **Esperado:** ≥5 usuarios demo visibles |

### E2E-U-02 — Crear usuario
| **Pasos:** `/Users/Create` → `e2e.user@test.local` / `E2eUser123!` |
| **Esperado:** Usuario en lista |

### E2E-U-03 — Asignar rol Support
| **Pasos:** `/Users/Edit/{id}` → assign role Support |
| **Esperado:** Rol visible en perfil usuario |

### E2E-U-04 — Desactivar y reactivar usuario
| **Pasos:** Toggle status inactive → login falla con ese user → reactivate |
| **Esperado:** Login bloqueado cuando inactive |

### E2E-U-05 — Vista Roles
| **Pasos:** `/Users/Roles` |
| **Esperado:** Conteos por rol (Admin, Manager, Sales, Support, Viewer) |

### E2E-U-06 — Bulk activate/deactivate
| **Pasos:** BulkActions con 2 user ids |
| **Esperado:** Redirect con resultado |

---

## 8. Suite P2 — Automatización y admin

### E2E-W-01 — Crear workflow
| **Pasos:** `/Workflows/Create` → `Workflow E2E` |
| **Esperado:** Aparece en `/Workflows` |

### E2E-W-02 — Editar workflow (trigger + action)
| **Pasos:** Edit → add trigger LeadCreated → add action SendNotification (o tipo disponible) |
| **Esperado:** Elementos listados en edit page |

### E2E-W-03 — Duplicar workflow
| **Esperado:** Segundo workflow con nombre copiado |

### E2E-W-04 — Eliminar workflow
| **Esperado:** Ya no en lista |

### E2E-P-01 — CRUD política
| **Pasos:** Create → Edit expression → Duplicate → Delete |
| **Esperado:** Ciclo completo sin error |

### E2E-AG-01 — Ver agentes
| **Pasos:** `/Agents` |
| **Esperado:** 7 agentes listados Active |

### E2E-AG-02 — Actualizar config agente
| **Pasos:** POST config JSON válido para LeadIntelligenceAgent |
| **Esperado:** TempData success o permanece en página sin crash |

### E2E-AUD-01 — Filtrar auditoría
| **Pasos:** `/Audit` → filtrar por eventType LeadCreated (si existe) |
| **Esperado:** Lista filtrada |

### E2E-AUD-02 — Export auditoría JSON
| **Pasos:** Export → descarga archivo |
| **Esperado:** JSON válido con eventos |

### E2E-SET-01 — Actualizar tenant
| **Pasos:** `/Settings` → cambiar timezone → guardar |
| **Esperado:** TempData success |

### E2E-SET-02 — Export / import config
| **Pasos:** Export JSON → modificar → import |
| **Esperado:** Settings restaurados |

### E2E-SET-03 — Restore defaults
| **Esperado:** Valores default (Region, MfaRequired, etc.) |

### E2E-SUP-01 — Health en soporte
| **Pasos:** `/Support` |
| **Esperado:** Database, EventBus, Cache = Healthy (con stack completo) |

---

## 9. Suite P2/P3 — Seguridad y permisos

> **Importante:** Marcar KNOWN si Viewer accede donde "idealmente" no debería.

### E2E-SEC-01 — Acceso anónimo bloqueado
| **Pasos:** Sin login → `/Customers`, `/Users`, `/Settings` |
| **Esperado:** Redirect login |

### E2E-SEC-02 — Viewer accede a módulos admin
| Rol | Viewer |
| **Pasos:** Login viewer → `/Users`, `/Settings` |
| **Esperado actual (KNOWN):** Acceso permitido |
| **Esperado ideal (futuro):** 403 / AccessDenied |

### E2E-SEC-03 — Sales accede a Users
| Rol | Sales |
| **Esperado actual (KNOWN):** Probablemente permitido |

### E2E-SEC-04 — API sin token
| **Pasos:** `GET /api/leads` sin Authorization |
| **Esperado:** 401 Unauthorized |

### E2E-SEC-05 — API CreateUser sin Admin
| **Pasos:** Login Sales → obtener cookie/token → `POST /api/Users` |
| **Esperado:** 403 Forbidden (policy RequireAdmin) |

### E2E-SEC-06 — Rate limit (opcional P3)
| **Pasos:** >200 requests/min al mismo endpoint |
| **Esperado:** 429 |

---

## 10. Flujos E2E integrados (escenarios usuario real)

### FLUJO-01 — Ciclo comercial completo (Sales)
```
Login Sales
  → Crear Lead
  → Calificar
  → Convertir a Customer
  → Crear Deal $X
  → Mover stage hasta Proposal
  → Cerrar Won
  → Verificar Dashboard
  → Logout
```
**Criterio éxito:** Sin pasos bloqueados; entidades visibles en cada etapa.

### FLUJO-02 — Onboarding admin
```
Login Admin
  → Crear usuario Sales nuevo
  → Asignar rol Sales
  → Login con nuevo user (ventana incógnito)
  → Crear lead
  → Logout ambos
```

### FLUJO-03 — Import masivo + bulk
```
Login Manager
  → Import CSV customers (2 filas)
  → Bulk status → Customer
  → Import leads JSON
  → Verificar counts en Dashboard
```

### FLUJO-04 — Configuración y auditoría
```
Login Admin
  → Settings: export config
  → Crear lead (genera evento)
  → Audit: filtrar LeadCreated
  → Export audit JSON
  → Verificar evento del lead en JSON
```

---

## 11. Matriz de ejecución por rol

| Caso | Admin | Manager | Sales | Support | Viewer |
|------|:-----:|:-------:|:-----:|:-------:|:------:|
| E2E-AUTH-03 | ✓ | ✓ | ✓ | ✓ | ✓ |
| E2E-L-01..05 | ✓ | ✓ | ✓ | ○ | ○ |
| E2E-C-* | ✓ | ✓ | ✓ | ○ | ○ |
| E2E-D-* | ✓ | ✓ | ✓ | ○ | ○ |
| E2E-U-* | ✓ | ○ | KNOWN | KNOWN | KNOWN |
| E2E-W/P/AG | ✓ | ○ | KNOWN | ○ | KNOWN |
| E2E-AUD | ✓ | ✓ | KNOWN | ✓ | KNOWN |
| E2E-SET | ✓ | KNOWN | KNOWN | KNOWN | KNOWN |
| E2E-SEC-02 | — | — | — | — | ✓ |

Leyenda: ✓ ejecutar | ○ opcional | KNOWN = documentar gap permisos

---

## 12. Plantilla de registro de ejecución

Copiar por cada corrida:

```markdown
## Run: YYYY-MM-DD — [Local|VPS] — Ejecutor: ___

| ID | Prioridad | Rol | Resultado | Notas | Bug # |
|----|-----------|-----|-----------|-------|-------|
| E2E-AUTH-01 | P0 | Admin | PASS | | |
| E2E-AUTH-02 | P0 | — | PASS | | |
| ... | | | | | |

**Resumen:** __ / __ PASS | __ FAIL | __ BLOCKED
**Build/commit:** ___
**URL:** ___
```

---

## 13. Criterios de salida (release gate)

| Gate | Condición |
|------|-----------|
| **Smoke P0** | 100% PASS (0 FAIL bloqueantes) |
| **P1 Core** | ≥ 95% PASS; FAIL solo con ticket |
| **P2 Admin** | ≥ 80% PASS |
| **Seguridad** | E2E-SEC-01, 04, 05 obligatorios PASS |
| **Known gaps** | Documentados; no bloquean preview si aceptados por producto |

---

## 14. Anexo — Casos API complementarios (no Browser Tab)

| ID | Método | Endpoint | Auth | Esperado |
|----|--------|----------|------|----------|
| E2E-API-01 | GET | `/health` | — | 200 |
| E2E-API-02 | POST | `/api/auth/login` | body tenant+user | 200 + accessToken |
| E2E-API-03 | GET | `/api/leads?tenantId=` | Bearer Admin | 200 list |
| E2E-API-04 | POST | `/api/customers` | Bearer Sales | 201/200 |
| E2E-API-05 | POST | `/api/users` | Bearer Sales | 403 |
| E2E-API-06 | POST | `/api/users` | Bearer Admin | 200 |

---

## 15. Roadmap automatización (post-manual)

Cuando se automatice (Playwright / Cursor Browser Tab script):

1. **Fixtures:** login helper por rol, tenantId from page
2. **Orden:** P0 serial; P1 paralelo por módulo con datos únicos (timestamp en emails)
3. **Assertions:** URL + texto visible + query params (`imported`, `bulkUpdated`)
4. **Screenshots:** solo en FAIL
5. **CI:** local Postgres + `dotnet run` + headless (fase 2)

Ejemplo pseudocódigo login:
```typescript
await page.goto(`${BASE}/Account/Login`);
await page.fill('#Email', 'sales@autonomuscrm.local');
await page.fill('#Password', 'Sales123!');
await page.click('button[type=submit]');
await expect(page).toHaveURL(`${BASE}/`);
```

---

## 16. Orden recomendado de ejecución (1ª corrida completa)

```
Día 1 — P0 (2h):
  AUTH-01..04 → NAV-01 → DASH-01 → L-01..03 → D-01..03

Día 2 — P1 CRM (2h):
  C-01..04 → L-04..05 → D-04..05 → DASH-02

Día 3 — P1 Users + P2 (2h):
  U-01..06 → W-01..04 → P-01

Día 4 — P2 Admin + SEC (1.5h):
  AG-01..02 → AUD-01..02 → SET-01..03 → SUP-01 → SEC-01..05

Día 5 — Flujos integrados (1h):
  FLUJO-01..04
```

---

## 17. Referencias

- Análisis arquitectónico: `ANALISIS_FUNCIONAL_COMPLETO_APLICACION.md`
- Despliegue VPS: `DESPLIEGUE_VPS_AUTONOMUSCRM.md`
- Credenciales seed: `DemoRoleUsers.cs`, login `/Account/Login`
- Tests unitarios actuales: `AutonomusCRM.Tests` (13 tests — no sustituyen E2E)

---

*Plan listo para ejecución manual en Browser Tab. Actualizar este documento cuando se implementen restricciones por rol en UI o se corrija resolución de tenant.*
