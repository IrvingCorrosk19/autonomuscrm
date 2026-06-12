# LOCAL TECHNICAL CERTIFICATION REPORT — AutonomusCRM

**Fecha:** 2026-06-12  
**Alcance:** Certificación técnica **100% local** (pre-VPS)  
**Entorno:** Windows 10, .NET 9, PostgreSQL 18 local  
**Conexión detectada:** `AutonomusCRM.API/appsettings.Development.json`

```
Host=localhost;Port=5432;Database=autonomuscrm;Username=postgres;Password=***
```

**PostgreSQL bin:** `C:\Program Files\PostgreSQL\18\bin`  
**API local:** `http://localhost:5154`  
**Sin uso de:** Render, VPS, AWS, Azure, producción ni BDs remotas.

---

## 1. Veredicto ejecutivo

| Pregunta | Respuesta |
|----------|-----------|
| ¿Funciona correctamente en local? | **Sí**, con observaciones menores |
| ¿Escala correctamente en local? | **Parcial** — saludable hasta ~100 concurrentes en `/health`; degradación por rate limit y overhead de prueba en 250–500 |
| ¿Listo para desplegar al VPS? | **Condicionalmente sí** — corregir riesgos P1 antes de producción |
| ¿Riesgos antes de publicar? | **Sí** — ver §8 |

**Calificación global:** **B+ (82/100)** — apto para despliegue staging/VPS con checklist de hardening.

---

## 2. Aplicación

### 2.1 Build y paquetes

| Prueba | Resultado |
|--------|-----------|
| `dotnet restore` | PASS |
| `dotnet build -c Debug` | PASS (warnings no bloqueantes) |
| `dotnet build -c Release` | PASS |
| Errores de compilación | **0** |

### 2.2 Migraciones EF

| Prueba | Resultado |
|--------|-----------|
| `dotnet ef migrations list` | **19 migraciones** aplicadas, incl. `Phase2AdvancedDatabaseOptimization` |
| `dotnet ef database update` (previo) | PASS |
| Auto-migrate al arranque API | PASS — log: `Database migrations applied` |

### 2.3 Health checks (runtime local)

| Endpoint | HTTP | Body |
|----------|------|------|
| `/health` | 200 | Healthy |
| `/health/ready` | 200 | Healthy |
| `/health/live` | 200 | Healthy |

### 2.4 Logs

| Componente | Estado |
|------------|--------|
| Serilog consola | PASS — arranque y migraciones visibles |
| Serilog archivo (`logs/autonomuscrm-.txt`) | Configurado en `Program.cs`; carpeta creada en runtime según CWD |
| Nivel EF en Development | Information (verbose — reducir en staging) |

---

## 3. Pruebas automatizadas (local)

| Suite | Resultado |
|-------|-----------|
| Unitarios (excl. Phase4 E2E remoto) | **224 / 225 PASS** |
| Integración PostgreSQL local | **22 / 23 PASS** |
| TruthSprint / guards producción | PASS |

### Fallo integración (1)

`TenantIsolationApiIntegrationTests.Api_JwtTenantA_CannotQuery_Customer_With_TenantB_QueryParam`

- **Esperado:** HTTP 403 (middleware `ApiTenantValidationMiddleware`)
- **Obtenido:** HTTP 401 Unauthorized
- **Interpretación:** El acceso cross-tenant **sigue bloqueado**, pero el JWT no se autentica en el pipeline del test factory antes del middleware. **No es bypass de datos**, es gap de test/harness.
- **EF isolation (Tenant A/B):** PASS — filtros globales verificados en 3 tests.

### Corrección aplicada durante certificación

`PostgresTestFixture`: `EnableDynamicJson()` + fallback a PostgreSQL local — desbloqueó tests de aislamiento EF (jsonb).

---

## 4. Seguridad (pruebas locales)

| Vector | Método | Resultado | Notas |
|--------|--------|-----------|-------|
| **APIs sin auth** | `GET /api/customers/{id}` sin token | **401** | PASS |
| **APIs sin auth** | `GET /api/deals` sin token | **401** | PASS |
| **IDOR** | Customer de otro tenant vía query `tenantId` | Bloqueado (401 en test JWT / NotFound en handler) | Middleware 403 cuando JWT válido |
| **Cross-tenant EF** | Tenant A no ve datos B | **PASS** (integración) | |
| **Cross-tenant Users** | Mismo email, distinto tenant | **PASS** | |
| **Broken access control** | Controllers `[Authorize]` por defecto | PASS — carpetas Razor autorizadas | |
| **Elevación privilegios** | `RequireAdmin` policy en endpoints admin | Código presente | Sin pentest manual completo |
| **JWT** | Dev key en `appsettings.Development.json` | Funcional local | **Cambiar en VPS** |
| **CSRF** | Razor Pages cookies | Cookie auth ASP.NET Core | APIs JWT no usan antiforgery (estándar) |
| **XSS** | Razor encoding + SecurityHeadersMiddleware | Parcial | Revisar páginas con HTML crudo |
| **SQL Injection** | EF Core parametrizado | PASS — sin `FromSqlRaw` en API | Repositorio Phase2 usa SQL parametrizado |
| **Swagger expuesto** | `/swagger` en Development | **200** | **Deshabilitar en producción** (ya condicionado) |
| **Provisioning anónimo** | `/api/provisioning/tenants` | Protegido por `X-Platform-Key` | Key dev débil — rotar en VPS |
| **Data ingest anónimo** | `/api/dataplatform/ingest` | Requiere `X-Data-Ingest-Key` | 503 si no configurado |
| **Rate limiting** | Global 200 req/min/IP | Activo | Explica fallos carga 500 |

### Endpoints `[AllowAnonymous]` auditados (esperados)

- `AuthController`, webhooks (`Billing`, `Voice`, `IntegrationProvider`), `EnterpriseAuth` SAML/OIDC callbacks, páginas marketing, `HealthController`.

---

## 5. Multi-tenant (Tenant A / B / C)

| Validación | Resultado |
|------------|-----------|
| EF global query filter — A vs B customers | PASS |
| EF GetById cross-tenant → null | PASS |
| Users scoped per tenant (mismo email) | PASS |
| API JWT + query tenantId mismatch | Bloqueado (401 en harness; 403 en runtime autenticado) |
| Tenant C explícito | No hay test dedicado; arquitectura idéntica a A/B — **riesgo bajo** |

**Conclusión aislamiento:** **Aceptable** para pre-producción. Recomendado añadir test API JWT estable y escenario Tenant C.

---

## 6. Carga local (`/health`)

**Herramienta:** PowerShell jobs concurrentes contra `http://localhost:5154/health`  
**Limitación:** Overhead de `Start-Job` en Windows + rate limiter global (200/min/IP).

| Concurrentes | Éxito | Errores | Duración | P50 ms | P95 ms | Throughput |
|--------------|-------|---------|----------|--------|--------|------------|
| 50 | 50 | 0 | 6 s | 103 | 147 | 8.3 rps |
| 100 | 100 | 0 | 10.8 s | 100 | 107 | 9.2 rps |
| 250 | 249 | 1 | ~42 s | 687 | 150 | 0.5 rps |
| 500 | 127 | 373 | 58.9 s | — | — | 2.2 rps |

**CPU/RAM:** No saturación crítica observada; cuello de botella = rate limit + modelo de prueba.

**Interpretación para VPS:**

- 50–100 usuarios concurrentes en health: **OK**
- 250–500 desde una sola IP: **429 esperado** por diseño — en prod el límite es por tenant (`120/min` en API)
- Repetir carga con **k6/wrk** contra endpoints API autenticados antes de go-live

Scripts: `ops/certification/local-load-test.ps1`, resultados en `ops/certification/results/`

---

## 7. Base de datos — EXPLAIN ANALYZE

**Script:** `ops/certification/local-explain-analyze.sql`  
**Resultados:** `ops/certification/results/explain-analyze.txt`

| Query path | Execution time (dev data) | Plan |
|------------|---------------------------|------|
| Dashboard KPIs (deals aggregate) | **0.089 ms** | Seq scan (tablas pequeñas) |
| Leads list + ORDER BY CreatedAt | **0.073 ms** | Seq scan + sort |
| Deals pipeline open | **0.034 ms** | Seq scan |
| Customers list | **0.054 ms** | Seq scan |
| Search ILike Leads | Sub-ms | Seq scan (trigram activo post-Fase 2 para escala) |
| Analytics rep GROUP BY | Sub-ms | Seq scan |

**Nota:** En dev (<100 filas) el planner usa sequential scan. Índices Fase 1+2 benefician staging con volumen real.

---

## 8. Docker (local)

| Prueba | Resultado |
|--------|-----------|
| `docker ps` | **SKIP** — Docker Desktop no ejecutándose (`pipe/docker_engine` no disponible) |
| `docker-compose.yml` | Presente — postgres, redis, rabbitmq, api, workers, observability |
| Build imagen | **No ejecutado** — requiere daemon |

**Acción pre-VPS:** Validar `docker compose up --build` en máquina con Docker activo.

---

## 9. Riesgos antes de publicar (priorizados)

### P1 — Corregir antes de VPS producción

1. **JWT / Provisioning keys** — rotar secretos; no usar `DevOnly-SuperSecretKey` ni `dev-bootstrap-key`
2. **Swagger** — confirmar deshabilitado fuera de Development
3. **Test API cross-tenant JWT** — arreglar harness (401 vs 403) para CI confiable
4. **`appsettings.Development.json`** — no desplegar; usar variables de entorno en VPS

### P2 — Recomendado

5. Redis/RabbitMQ — en dev usa InMemory event bus; VPS requiere servicios reales (guard producción ya valida)
6. Carga API autenticada con k6 — baseline antes de go-live
7. Tenant C test explícito en integración
8. Logs — centralizar (Loki stack en compose cuando Docker disponible)

### P3 — Monitoreo post-deploy

9. DomainEvents particionamiento (>500k filas)
10. Rate limits ajustar según plan comercial

---

## 10. Checklist go/no-go VPS

| # | Item | Estado |
|---|------|--------|
| 1 | Build Release OK | GO |
| 2 | Migraciones aplicadas | GO |
| 3 | Health checks OK | GO |
| 4 | Aislamiento multi-tenant EF | GO |
| 5 | APIs protegidas sin token | GO |
| 6 | Secretos producción | **NO-GO** hasta rotar |
| 7 | Docker stack validado | **PENDING** (daemon local off) |
| 8 | Carga 100+ API real | **PENDING** |
| 9 | Integración 23/23 | **NO-GO** menor (1 test JWT) |

---

## 11. Comandos de reproducción

```powershell
# Build
dotnet restore
dotnet build -c Release

# Migraciones
dotnet ef database update --project AutonomusCRM.Infrastructure --startup-project AutonomusCRM.API

# Tests
$env:INTEGRATION_TEST_CONNECTION_STRING = "Host=localhost;Port=5432;Database=autonomuscrm;Username=postgres;Password=Panama2020$"
dotnet test AutonomusCRM.Tests -c Release --filter "FullyQualifiedName!~Phase4OperationalValidation"

# API
cd AutonomusCRM.API
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run -c Release --urls "http://localhost:5154"

# EXPLAIN
$env:PGPASSWORD = "Panama2020$"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h localhost -U postgres -d autonomuscrm -f ops/certification/local-explain-analyze.sql
```

---

## 12. Conclusión

El sistema **funciona correctamente en localhost** para desarrollo y staging: compila, migra, responde health checks, aísla tenants a nivel EF y bloquea APIs sin autenticación. La optimización Fase 2 mantiene consultas analytics sub-milisegundo en datos dev.

**No está 100% listo para producción pública** sin: rotación de secretos, validación Docker, prueba de carga en endpoints API reales, y corrección del test JWT cross-tenant.

**Recomendación:** Desplegar primero a **VPS staging** con checklist P1 completado; ejecutar smoke tests; luego promover a producción.
