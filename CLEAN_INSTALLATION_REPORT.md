# CLEAN INSTALLATION REPORT

**Proyecto:** AutonomusCRM  
**Fecha:** 2026-05-28  
**Escenario:** Base de datos completamente vacía, `Seed:Enabled=false`, sin cuentas demo  
**Método:** Análisis de código + validación parcial en entorno local (post-reset SQL, sin re-seed)

---

## Resumen

| Pregunta | Respuesta | Evidencia |
|----------|-----------|-----------|
| ¿El sistema inicia? | ✅ Sí, si PostgreSQL + config mínima | `ApplyMigrationsAsync`, guard solo en Prod |
| ¿El login funciona? | ❌ No, hasta provisioning | `LoginCommandHandler` sin usuarios → 401 |
| ¿Puede crearse el primer usuario? | ✅ Sí, vía provisioning API | `TenantProvisioningService` crea Admin |
| ¿Puede crearse el primer tenant? | ✅ Sí, vía provisioning API | `POST /api/provisioning/tenants` |
| ¿Puede operarse sin datos demo? | ✅ Sí, manualmente | Empty states en UI; sin crash |

**Veredicto simulación:** Instalación limpia es **viable** con intervención ops (provisioning API). No es self-service para el cliente final.

---

## 1. Simulación ejecutada

### 1.1 Entorno local (evidencia sesión previa + config actual)

| Paso | Acción | Resultado |
|------|--------|-----------|
| Reset SQL | `ops/database/09_reset_test_data.sql` | 0 customers, 0 leads, 0 deals |
| Seed desactivado | `Seed.Enabled: false` en `appsettings.Development.json` | No re-inyección demo al reiniciar |
| Tenants/users conservados | Script conserva tenants/users | **No simula BD 100% vacía** |
| PostgreSQL | Nativo Windows :5432 | `psql` no en PATH en esta sesión — sin drop/create DB |

### 1.2 Simulación lógica BD 100% vacía (código)

| Paso | Esperado | Código |
|------|----------|--------|
| `dotnet run` / container start | MigrateAsync crea schema | `WebApplicationExtensions.cs` L9–27 |
| `InitializeDatabaseAsync` | Skip (Seed=false) | L32–33 |
| GET `/Account/Login` | Página carga, sin demo panel | `Login.cshtml.cs` ShowDemoAccounts=false |
| POST login | 401 / error credenciales | Sin users en `Users` table |
| POST `/api/provisioning/tenants` sin key | 401 | `ProvisioningController.cs` L25–27 |
| POST provisioning con key válida | 201 + tenantId | Crea tenant + Admin |
| POST login con admin provisionado | ✅ Cookie session | `RoleHomeRedirect` → `/executive` |

---

## 2. Migraciones

**Total:** 17 migraciones aplicadas secuencialmente por EF Core.

| Migración inicial | `20251224185349_InitialCreate` |
| Migración más reciente | `20260605030856_DatabasePerformanceIndexesPhase2` |

**Comportamiento en BD vacía:**
- `Database:AutoMigrate=true` → tablas creadas al primer arranque
- Sin datos insertados si `Seed:Enabled=false`
- Tablas críticas: `Tenants`, `Users`, `Leads`, `Customers`, `Deals`, `Workflows`, `Policies`, `AiDecisionAudits`, `BusinessMemoryRoots`, `TenantBillingAccounts`, etc.

**Resultado:** ✅ Migraciones no dependen de seed.

---

## 3. Inicialización / seed mínimo obligatorio

### 3.1 Seed mínimo real (no demo)

El sistema **no tiene** un "minimal seed" automático. El mínimo obligatorio es:

```
1 registro Tenants  (vía provisioning)
1 registro Users    (Admin, vía provisioning)
0 registros CRM     (leads/customers/deals — cliente los crea)
```

### 3.2 Lo que NO se seedea automáticamente (correcto para cliente limpio)

- Workflows
- Policies
- Integraciones
- Billing account (se crea lazy)
- Business memory
- AI decision audits

---

## 4. Validación por pregunta

### 4.1 ¿El sistema inicia?

| Entorno | Resultado |
|---------|-----------|
| Development + PG local + Jwt dev key | ✅ Arranca |
| Production sin Redis | ❌ Guard fail-fast |
| Production sin RabbitMQ host | ❌ Guard fail-fast |
| Production completo + Seed=false | ✅ Arranca |

### 4.2 ¿El login funciona?

| Estado BD | Resultado |
|-----------|-----------|
| Vacía | ❌ Imposible |
| Post-provisioning (1 Admin) | ✅ |
| Usuario sin rol (creado vía UI Create) | ⚠️ Login OK pero permisos mínimos |

### 4.3 ¿Puede crearse el primer usuario?

| Método | BD vacía | Notas |
|--------|----------|-------|
| Provisioning API | ✅ Crea Admin con rol | Recomendado |
| `/Users/Create` | ❌ Requiere login previo | — |
| `POST /api/users` | ❌ Requiere Admin JWT | — |
| Seed | ✅ pero demo | No usar |

**Gap:** `CreateUserCommandHandler` no asigna rol — usuario queda sin `ClaimTypes.Role` hasta `/Users/Edit`.

### 4.4 ¿Puede crearse el primer tenant?

| Método | Crea admin | BD vacía |
|--------|------------|----------|
| `POST /api/provisioning/tenants` | ✅ | ✅ |
| `POST /api/tenants` | ❌ | ❌ (requiere Admin) |
| `PageModelTenantExtensions` auto-create | ❌ | Solo páginas anónimas, tenant huérfano |

### 4.5 ¿Puede operarse sin datos demo?

| Módulo | Sin demo |
|--------|----------|
| Leads CRUD | ✅ Empty state + crear |
| Customers CRUD | ✅ |
| Deals CRUD | ✅ |
| Users/Roles | ✅ (con provisioning + asignación manual roles) |
| Workflows | ✅ vacío, crear manual |
| Trust | ✅ cola vacía |
| Executive/Revenue | ✅ empty states |
| IA | ⚠️ off o sin key |
| Workers | ⚠️ sin eventos hasta actividad |

---

## 5. Procedimiento reproducible — instalación limpia

```powershell
# 1. BD vacía (ejemplo)
# DROP DATABASE autonomuscrm; CREATE DATABASE autonomuscrm;

# 2. Variables mínimas
$env:Seed__Enabled = "false"
$env:Database__AutoMigrate = "true"
$env:Provisioning__ApiKey = "<secret>"
# + ConnectionStrings, Jwt, etc.

# 3. Arrancar API
dotnet run --project AutonomusCRM.API

# 4. Provisionar primer cliente
curl -X POST http://localhost:5000/api/provisioning/tenants `
  -H "Content-Type: application/json" `
  -H "X-Platform-Key: <secret>" `
  -d '{"name":"TechSolutions Panamá","adminEmail":"admin@techsolutions.pa","adminPassword":"<strong>"}'

# 5. Login
# http://localhost:5000/Account/Login
```

---

## 6. Hallazgos / bloqueantes de instalación limpia

| ID | Severidad | Hallazgo |
|----|-----------|----------|
| CI-1 | ❌ | BD vacía sin `Provisioning:ApiKey` → sistema inutilizable |
| CI-2 | ❌ | VPS compose fuerza `Seed__Enabled=true` |
| CI-3 | ⚠️ | Usuarios creados en UI sin rol por defecto |
| CI-4 | ⚠️ | Multi-tenant login en Prod usa primer tenant oculto |
| CI-5 | ⚠️ | No hay wizard — ops debe ejecutar curl/script |
| CI-6 | ✅ | Migraciones OK sin seed |
| CI-7 | ✅ | Empty states no crashean UI |

---

## 7. Comparación: seed on vs seed off

| Métrica | Seed:true BD vacía | Seed:false + provisioning |
|---------|-------------------|----------------------------|
| Tenants al arranque | 3+ (Demo, QA-B, CEO_DEMO) | 1 (cliente) |
| Usuarios | 5+ demo + QA + CEO | 1 Admin |
| Customers | 50+ | 0 |
| Login inmediato | ✅ demo creds | ❌ hasta provision |
| Apto cliente real | ❌ | ✅ |

---

## 8. Conclusión

La instalación limpia **funciona técnicamente** con:
1. `Seed:Enabled=false`
2. Migraciones automáticas
3. Una llamada a provisioning API

**No es plug-and-play** para un cliente sin soporte técnico en el día 1.
