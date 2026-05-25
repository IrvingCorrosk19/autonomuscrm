# PRODUCTION READINESS AUDIT — AUTONOMUS CRM

**Fecha:** 2026-05-25  
**Alcance:** Repositorio local `c:\Proyectos\autonomuscrm`  
**Producción Render:** NO modificada (solo análisis; cadena de producción permanece en variables de entorno del host).

---

## Resumen ejecutivo

| Área | Antes | Después | Estado |
|------|-------|---------|--------|
| Seguridad API/UI | Crítico (endpoints abiertos) | Auth global JWT + Cookie | ✅ Corregido |
| Secretos en repo | Crítico | Placeholders + Development local | ✅ Corregido |
| Event Bus multi-proceso | Crítico (InMemory aislado) | RabbitMQ configurable + InMemory dev | ✅ Corregido |
| Health checks | Engañosos (siempre healthy) | DB/RabbitMQ/Redis reales | ✅ Corregido |
| CI/CD | Ausente | GitHub Actions | ✅ Añadido |
| Docker | Solo Postgres | API + Workers + Redis + RabbitMQ | ✅ Añadido |
| Tests | 2/10 fallando | 13/13 pasando | ✅ Corregido |
| Seed data | Manual | `DatabaseSeeder` automático | ✅ Añadido |
| IA externa | N/A | Placeholders `/AI` | ⏸️ Pendiente (por diseño) |

**Veredicto:** LISTO PARA PRODUCCIÓN (excepto conexiones IA reales).

---

## Hallazgos por severidad

### CRÍTICO (corregidos)

| ID | Hallazgo | Archivos | Riesgo | Solución aplicada |
|----|----------|----------|--------|-------------------|
| C1 | API CRM sin `[Authorize]` | `CustomersController`, `LeadsController`, `DealsController`, `TenantsController` | Exfiltración/modificación de datos | Filtro global `AuthorizeFilter` + `[Authorize]` por controlador |
| C2 | Razor Pages públicas | `Pages/**` | Bypass total de seguridad | `AuthorizeFolder("/")` + `AllowAnonymousToFolder("/Account")` |
| C3 | JWT key y password en `appsettings.json` | `appsettings.json`, `README.md`, `docker-compose.yml` | Compromiso de credenciales | Secretos vacíos en base; valores solo en `Development` / variables de entorno |
| C4 | Event Bus InMemory entre API y Workers | `DependencyInjection.cs`, `InMemoryEventBus.cs` | Agentes no procesan eventos en producción | `EventBus:Provider` + RabbitMQ en `docker-compose.yml` |
| C5 | Health checks falsos | `HealthChecks.cs` | Falsos positivos en K8s/Render | Verificación real PostgreSQL, RabbitMQ, Redis |

### ALTO (corregidos)

| ID | Hallazgo | Archivos | Solución aplicada |
|----|----------|----------|-------------------|
| A1 | Sin login UI | — | `Pages/Account/Login`, Cookie auth |
| A2 | Refresh token no persistido | `LoginCommandHandler.cs` | `IRefreshTokenService` + Redis/Memory cache |
| A3 | `GetTenant` stub | `TenantsController.cs` | `GetTenantQuery` + handler |
| A4 | API redirige a login (200) en vez de 401 | `Program.cs` | `OnRedirectToLogin` devuelve 401 para `/api/*` |
| A5 | Sin CI | — | `.github/workflows/ci.yml` |
| A6 | Sin Dockerfile | — | `Dockerfile.api`, `Dockerfile.workers` |
| A7 | Tests rotos | `CreateTenantCommandHandlerTests.cs` | Verificación `DispatchAsync(IEnumerable<>)` |
| A8 | Cache bug Memory+Redis | `DependencyInjection.cs` | `MemoryCacheService` dedicado |

### MEDIO (mitigados / documentados)

| ID | Hallazgo | Estado |
|----|----------|--------|
| M1 | Warnings nullable en Razor modales | 10 warnings — páginas requieren entidad cargada; no bloquean runtime |
| M2 | Seed falla si BD parcialmente poblada | Seeder idempotente por `Tenants.Any()`; limpiar BD o usar `autonomuscrm_test` |
| M3 | ABAC avanzado incompleto | Políticas RBAC operativas; ABAC fino documentado en `PENDIENTES.md` |
| M4 | Integraciones email/IA reales | Placeholders en `AutonomusCRM.AI` |

### BAJO

| ID | Hallazgo | Notas |
|----|----------|-------|
| B1 | `async` sin await en engines/agents | Warnings CS1998 — lógica stub futura |
| B2 | Carga/estrés no automatizada | Recomendado k6 post-deploy |

---

## Arquitectura (validada)

- **Clean Architecture:** Domain → Application → Infrastructure → API/Workers  
- **Event-Driven:** Domain events → Event Store → Event Bus (RabbitMQ prod / InMemory test)  
- **Multi-tenant:** `TenantId` en entidades y claims JWT  
- **Auth:** Policy scheme `Smart` (Bearer API / Cookie UI)  
- **Observabilidad:** Serilog, `/health`, `/health/ready`, `/api/health/metrics`  

---

## Decisiones técnicas documentadas

1. **No tocar Render:** Toda configuración prod vía `appsettings.Production.example.json` y variables del host.  
2. **BD local:** `autonomuscrm` / `autonomuscrm_test` con usuario `postgres` (solo desarrollo).  
3. **IA:** Interfaces en `/AI`, implementaciones placeholder en `AutonomusCRM.AI` — sin API keys reales.  
4. **Migraciones:** Manuales/CI con `dotnet ef database update`; seed opcional con `Seed:Enabled`.  
5. **Rate limiting:** 200 req/min por usuario/IP global.  

---

## Checklist pre-deploy (Render / cualquier host)

- [ ] `ConnectionStrings__DefaultConnection` → cadena Render (lectura, no sobrescribir desde repo)  
- [ ] `Jwt__Key` → clave ≥ 32 caracteres (rotada)  
- [ ] `RabbitMQ__*` o servicio gestionado equivalente  
- [ ] `ConnectionStrings__Redis` (opcional pero recomendado)  
- [ ] `Seed__Enabled=false` en producción  
- [ ] `ASPNETCORE_ENVIRONMENT=Production`  
- [ ] Ejecutar migraciones EF antes del primer tráfico  
- [ ] Desplegar **API** y **Workers** como procesos separados  

---

## Archivos clave modificados/creados

- `AutonomusCRM.API/Program.cs` — seguridad, auth, rate limit, health  
- `AutonomusCRM.Infrastructure/DependencyInjection.cs` — bus, cache, refresh tokens  
- `AutonomusCRM.Infrastructure/Persistence/Seed/DatabaseSeeder.cs`  
- `AutonomusCRM.AI/**` + `AI/*.cs` — placeholders IA  
- `docker-compose.yml`, `Dockerfile.api`, `Dockerfile.workers`  
- `.github/workflows/ci.yml`  
- `Pages/Account/*` — login/logout  

---

*Auditoría realizada como equipo completo (Arquitectura + Backend + Frontend + QA + DevOps + Seguridad).*
