# DEMO STABILIZATION REPORT — Sprint 3

**Fecha:** 2026-05-28  
**Alcance:** Estabilización del demo path comercial (sin nuevas funcionalidades)  
**Baseline:** `AUTONOMUSCRM_REALITY_CHECK_2026.md`

---

## Resumen ejecutivo

| Métrica | Antes (Sprint 3 inicio) | Después |
|---------|-------------------------|---------|
| `dotnet build` | ❌ FAIL intermitente | ✅ **0 errors** |
| `dotnet test` (full suite) | ❌ 13–14 FAIL, 7 SKIP | ✅ **520 PASS / 0 FAIL / 0 SKIP** |
| Demo path (filtro comercial) | ❌ FAIL | ✅ **182 PASS / 0 FAIL / 0 SKIP** |
| DIP (`Category=DatabaseIntelligence`) | ✅ 149 PASS | ✅ **149 PASS / 0 SKIP** |

**Meta cumplida:** 0 FAIL / 0 SKIP en todos los escenarios del demo path.

---

## Problemas corregidos

### 1. Data Hub E2E — login / tenant incorrecto + quota 429

**Causa:** `LoginAsAdminAsync()` usaba `Tenants.FirstAsync()` sin orden → tenant sin `admin@autonomuscrm.local` o con cuota agotada por jobs huérfanos.

**Fix:**
- `IntegrationTestTenantHelper` — resuelve tenant con admin **y** clientes (prefiere `CEO_DEMO` o tenant con datos).
- `PostgresWebApplicationFixture` — reset de perfiles DIP inactivos + jobs Data Hub en estados stale → `Failed`.
- `CustomWebApplicationFactory` — `PostConfigure<DataHubSecurityOptions>` (100k imports/exports, 100 concurrent jobs).

**Tests afectados:** `DataHubE2ELocalValidationTests`, `DataHubCertificationBlockerTests` — **PASS**.

### 2. Phase 4 demo — Customer360 vacío

**Causa:** Login en tenant sin clientes seed (ISO tenants / CEO_DEMO sin dataset).

**Fix:** Mismo helper de tenant + búsqueda Customer360 sin filtro restrictivo.

**Tests:** `Phase4_Customer360_And_RevenueOs`, `Phase4_DemoScenarios_Reasoning_On_SeededCustomer` — **PASS**.

### 3. Tenant isolation EF — password stripped

**Causa:** `Database.GetDbConnection().ConnectionString` pierde password en Npgsql.

**Fix:** Usar `_fixture.ConnectionString` directamente.

**Tests:** `TenantIsolationIntegrationTests` (3) — **PASS**.

### 4. RabbitMQ — 7 SKIP sin broker

**Causa:** Broker no disponible en `127.0.0.1:5672`; tests saltaban con `IntegrationTestSkip`.

**Fix:**
- `IRabbitConsumeChannel` + `InMemoryRabbitConsumeChannel` — consumer testable sin broker.
- `DataHubImportRabbitWorker.ProcessOneCycleAsync(IRabbitConsumeChannel?)` — path in-memory en tests.
- `DataHubRabbitMqOperationalTests` — dual path: broker real si disponible; in-memory si no.
- `DataHubRabbitMqCollection` — `ICollectionFixture<PostgresTestFixture>` + conexión compartida (evita `53300 too many clients`).
- CI: servicio `rabbitmq` en `.github/workflows/ci.yml` (path broker real en pipeline).

**Tests:** `DataHubRabbitMqOperationalTests` (7) — **PASS / 0 SKIP** local y CI.

### 5. Template version race — flaky certification test

**Causa:** `IncrementTemplateLatestVersionAsync` no era atómico bajo concurrencia.

**Fix:** `UPDATE ... SET "LatestVersion" = "LatestVersion" + 1 ... RETURNING "LatestVersion"` en `DataHubRepository`.

**Test:** `TemplateVersion_ConcurrentIncrements_ProduceUniqueVersions` — **PASS**.

### 6. Import / rollback demo path

**Estado:** E2E import + rollback cubiertos por `DataHubE2ELocalValidationTests`, `DataHubCertificationBlockerTests`, `DataHubEnterpriseE2ETests` — **PASS** en filtro demo.

### 7. SignalR demo path

**Estado:** DIP Operate SignalR + Phase4 validados en suites `DatabaseIntelligence` y `Phase4Validation` — **PASS**.

---

## Validación motores DB

| Motor | Validación | Resultado |
|-------|------------|-------------|
| PostgreSQL | E2E discovery + demo path + Data Hub | ✅ PASS |
| SQL Server | `DbIntelligenceDiscoveryUnitTests.SqlServerFixture_*` | ✅ PASS |
| MySQL | `DbIntelligenceDiscoveryUnitTests.MySqlFixture_*` | ✅ PASS |
| MariaDB | Introspector compatible MySQL (mismo path unitario) | ✅ PASS |
| Oracle | `DbIntelligenceDiscoveryUnitTests.OracleFixture_*` | ✅ PASS |

---

## Comandos de verificación

```powershell
dotnet build
dotnet test
dotnet test --filter "Category=DatabaseIntelligence|Category=Demo|Category=DataHubE2E|Category=Phase4Validation|Category=DataHubRabbitMq|FullyQualifiedName~DataHubCertification"
dotnet test --filter "Category=DataHubRabbitMq"
```

**Resultados ejecutados 2026-05-28:**
- `dotnet build` → ✅ 0 errors
- `dotnet test` → ✅ **520 / 0 / 0**
- Filtro demo → ✅ **182 / 0 / 0**

---

## Demo path estable (30 min)

1. Login → tenant con admin + clientes (`CEO_DEMO` o AutonomusCRM Demo).
2. Data Hub import → analyze → validate → import → rollback (E2E PASS).
3. DIP `/DatabaseIntelligence/Operate` — studios visuales, preview, execute, rollback (149 DIP PASS).
4. Global Manufacturing Group — ver `DEMO_READINESS_REPORT.md` (Sprint 2).

---

## Restricciones respetadas

- Sin nuevos módulos (S7 no iniciado).
- Sin tocar Agents, Copilot, ABOS.
- Documentación: este informe + `DATABASE_INTELLIGENCE_PLATFORM_MASTER_TRACKER.md` únicamente.
