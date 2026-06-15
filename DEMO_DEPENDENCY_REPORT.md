# DEMO DEPENDENCY REPORT

**Proyecto:** AutonomusCRM  
**Fecha:** 2026-05-28  
**Objetivo:** Identificar todo lo que depende de datos demo y qué fallaría en un cliente nuevo sin seeds

---

## Resumen ejecutivo

El sistema **puede operar sin datos demo** para flujos CRM manuales (leads, clientes, deals, usuarios), pero **múltiples puntos del código y del despliegue asumen o inyectan datos demo** cuando `Seed:Enabled=true`. En un cliente real con `Seed:Enabled=false` y provisioning correcto, el CRM arranca vacío — lo esperado — pero varias experiencias quedan **degradadas o vacías** hasta que el cliente cree su propia data.

**Riesgo crítico de despliegue:** `deploy/docker-compose.vps.yml` fuerza `Seed__Enabled: "true"`.

---

## 1. Puntos de inyección demo

### 1.1 DatabaseSeeder (`DatabaseSeeder.cs`)

**Trigger:** `Seed:Enabled=true` (default interno del seeder: `true` si key ausente; startup gate usa `IsDevelopment()` si key ausente).

| Escenario BD | Comportamiento |
|--------------|----------------|
| **BD vacía** | Crea tenant `"AutonomusCRM Demo"`, 5 usuarios `DemoRoleUsers`, 3 customers, 3 leads, 1 deal; luego QA-B y CEO_DEMO |
| **BD con tenants existentes** | Aún ejecuta `EnsureDemoRoleUsersAsync`, `QaTenantSeeder`, `CeoDemoSeeder` |

### 1.2 DemoRoleUsers (`DemoRoleUsers.cs`)

Usuarios hardcodeados siempre con el mismo patrón:

| Rol | Email | Password |
|-----|-------|----------|
| Admin | `admin@autonomuscrm.local` | `Admin123!` |
| Manager | `manager@autonomuscrm.local` | `Manager123!` |
| Sales | `sales@autonomuscrm.local` | `Sales123!` |
| Support | `support@autonomuscrm.local` | `Support123!` |
| Viewer | `viewer@autonomuscrm.local` | `Viewer123!` |

**Usado por:** `DatabaseSeeder`, `CeoDemoSeeder`, página Login (panel demo).

### 1.3 CeoDemoSeeder (`CeoDemoSeeder.cs`)

| Elemento | Valor hardcodeado |
|----------|-------------------|
| Tenant ID | `TenantIds.CeoDemo` = `c0e00000-0000-4000-8000-000000000001` |
| Nombre | `CEO_DEMO` |
| Dataset | 50+ customers, deals, revenue, trust audits, business memory, workflow tasks |

**Excepción parcial:** respeta `CeoDemo:SkipDataset=true` en tenant settings (requiere deploy del parche).

### 1.4 QaTenantSeeder (`QaTenantSeeder.cs`)

| Elemento | Valor |
|----------|-------|
| Tenant ID | `TenantIds.QaTenantB` = `a8f41d97-8cc8-5414-0a2c-b1629fe89d78` |
| Admin | `admin-b@qa.autonomusflow.local` / `Admin123!` |

### 1.5 TenantIds (`TenantIds.cs`)

GUIDs fijos para QA y CEO demo — no se generan dinámicamente.

---

## 2. UI y UX acopladas a demo

### 2.1 Login (`Login.cshtml.cs`)

| Comportamiento | Condición |
|----------------|-----------|
| Muestra panel de cuentas demo con passwords | `Seed:Enabled=true` AND NOT Production |
| Muestra selector de tenant | NOT Production |
| Prefiere tenant `CEO_DEMO` como default | Si existe en BD |
| `DemoEmail` / `DemoPassword` hints | `Seed:AdminEmail` / `Seed:AdminPassword` (solo UI, no seeder) |

**Cliente real en Production:** sin panel demo, sin selector tenant — login solo email+password.

**Riesgo multi-tenant:** en Production, `TenantId` oculto = primer tenant por `CreatedAt` — usuarios de otros tenants pueden fallar login.

### 2.2 Deploy script (`deploy/deploy-vps.ps1`)

Imprime credenciales demo post-deploy — inadecuado para cliente real.

### 2.3 CI workflows (`.github/workflows/ci.yml`, `platform-ci.yml`)

`Seed__Enabled: "true"` — correcto para CI, no para producción cliente.

### 2.4 Documentación y marketing

Referencias a "CEO demo en 5 minutos", `CEO_DEMO`, cuentas `@autonomuscrm.local` en manuales y landing.

---

## 3. Config keys que parecen cliente pero son demo-only

| Key | Usado en seeder | Usado en UI |
|-----|-----------------|-------------|
| `Seed:AdminEmail` | ❌ No | ✅ Login hints |
| `Seed:AdminPassword` | ❌ No | ✅ Login hints |
| `Seed:EnsureRoleUsers` | ❌ No (en `appsettings.Development.json` only) | — |

**Conclusión:** cambiar `Seed:AdminPassword` no afecta el seeder real — confusión operativa.

---

## 4. Qué depende de datos demo

| Área | Depende de demo | Sin demo |
|------|-----------------|----------|
| **Login inicial** | Solo si seed está on | Requiere provisioning API |
| **Command Center / Executive OS** | CEO_DEMO tiene métricas ricas | Empty state — funcional pero vacío |
| **Revenue OS** | Demo deals/pipeline | 0 deals hasta crear |
| **Trust Studio** | CEO_DEMO tiene audits | Cola vacía — normal |
| **Business Memory** | CEO_DEMO tiene facts | Vacío hasta actividad |
| **Customer 360** | Demo customers | Vacío |
| **Workflows** | No seeded (ni demo ni real) | Siempre vacío al inicio |
| **Policies ABAC** | No seeded | Permisivo (allow all) |
| **Agents page** | Muestra agentes del sistema | Funciona — no requiere demo data |
| **Audit** | Sin eventos hasta actividad | Vacío |
| **Billing** | Auto-crea cuenta free | Funciona |
| **Integrations** | Sin conexiones | Empty state |
| **Tests integración** | `Login_WithSeededAdmin_ShouldReturnToken` | Requiere seed o fixture |

---

## 5. Qué dejaría de funcionar en cliente nuevo (honesto)

### ❌ No funciona sin intervención ops

| Función | Por qué |
|---------|---------|
| Login | Sin usuarios en BD |
| Cualquier pantalla autenticada | Sin sesión |

### ⚠️ Funciona pero vacío / degradado

| Función | Estado sin demo |
|---------|-----------------|
| Executive OS | Empty state, sin KPIs |
| Revenue OS | Pipeline vacío |
| Trust Studio | Sin decisiones pendientes |
| Command Center | `HasData=false` |
| Workflows | Lista vacía — debe crear manualmente |
| Tasks | Sin tareas hasta workflows/eventos |
| Customer Success OS | Sin tickets |
| Memory / Semantic search | Sin embeddings hasta actividad + LLM |
| IA conversacional | `LlmNotConfiguredException` sin API key |
| Automatizaciones LLM (Workers 15min) | Workers corren reglas, no LLM |
| Workflow actions Communicate/ActivateAgent | Solo log, no ejecutan |

### ✅ Funciona correctamente sin demo

| Función | Notas |
|---------|-------|
| Crear leads/clientes/deals manualmente | Admin/Manager/Sales |
| Gestión usuarios + roles | Tras provisioning |
| Settings (lectura) | Defaults hardcoded |
| Billing dashboard | Plan free auto-creado |
| Policies CRUD | Vacío = allow all |
| Audit (una vez hay actividad) | Registra acciones |
| Health endpoints | Independiente de data |

---

## 6. Qué debe inicializarse automáticamente (gap actual)

| Elemento | Estado actual | Debería auto-inicializarse |
|----------|---------------|---------------------------|
| Primer tenant + Admin | ✅ vía Provisioning API | ✅ (requiere API key manual) |
| Trial 14 días | ✅ en provisioning | ✅ |
| Billing account free | ✅ lazy en primera visita billing | ✅ |
| Trust threshold 70 | ✅ default código | ✅ |
| Workflows ejemplo | ❌ no existe | ⚠️ Recomendable para onboarding |
| Policies mínimas | ❌ no existe | ⚠️ Recomendable (hoy allow-all) |
| Rol en CreateUser | ❌ usuarios sin rol | ❌ **Bug/gap** — requiere paso manual en Edit |
| Email templates | ❌ | Opcional |
| Integración LLM | ❌ | Opcional |

---

## 7. Matriz de dependencia demo por componente

```
┌─────────────────────┬──────────────┬─────────────────────────────┐
│ Componente          │ Seed:Enabled │ Cliente limpio (Seed:false) │
├─────────────────────┼──────────────┼─────────────────────────────┤
│ PostgreSQL schema   │ Migrate      │ Migrate                     │
│ Tenants             │ 3+ demo      │ 1 (provisioned)             │
│ Users               │ 5+ demo      │ 1+ (creados)                │
│ CRM entities        │ Sample data  │ Vacío                       │
│ CEO_DEMO dataset    │ 50+ clients  │ No existe                   │
│ Login demo panel    │ Visible      │ Oculto                      │
│ Executive dashboards│ Poblados     │ Empty state                 │
│ Workers/agents      │ Eventos demo │ Eventos reales del cliente  │
└─────────────────────┴──────────────┴─────────────────────────────┘
```

---

## 8. Recomendaciones (descubrimiento — no implementado)

1. **Separar flags:** `Seed:Enabled` vs `Seed:DemoData` vs `Seed:QaTenants` — hoy un solo flag controla todo.
2. **VPS compose:** cambiar `Seed__Enabled` default a `false` para clientes.
3. **Eliminar re-seed en BD poblada:** L31–37 de `DatabaseSeeder` no debería correr en producción cliente.
4. **Login multi-tenant:** resolver tenant por email sin depender del primer tenant.
5. **CreateUser con rol:** asignar rol en creación, no paso separado.
6. **Quitar credenciales demo** del deploy script en builds de cliente.

---

## 9. Archivos afectados

| Archivo | Tipo dependencia |
|---------|------------------|
| `Infrastructure/Persistence/Seed/DatabaseSeeder.cs` | Inyección principal |
| `Infrastructure/Persistence/Seed/DemoRoleUsers.cs` | Credenciales fijas |
| `Infrastructure/Persistence/Seed/CeoDemoSeeder.cs` | Dataset ejecutivo |
| `Infrastructure/Persistence/Seed/QaTenantSeeder.cs` | Tenant QA |
| `Application/Common/Tenancy/TenantIds.cs` | GUIDs fijos |
| `API/Pages/Account/Login.cshtml.cs` | UI demo |
| `docker-compose.yml` L111 | Seed on local Docker |
| `deploy/docker-compose.vps.yml` L76 | Seed on VPS |
| `deploy/deploy-vps.ps1` | Mensaje demo |
| `Tests/Integration/ApiIntegrationTests.cs` | Test con seed |
