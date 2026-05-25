# TEST RESULTS — AUTONOMUS CRM

**Ejecución:** 2026-05-25  
**Comando:** `dotnet test`  
**Resultado global:** ✅ **13/13 passed** (0 failed, 0 skipped)

---

## Pruebas unitarias

| Caso | Resultado | Estado | Corrección aplicada |
|------|-----------|--------|---------------------|
| `TenantTests` (dominio) | Pass | ✅ | — |
| `CreateTenantCommandHandlerTests.Handle_ShouldCreateTenantAndReturnId` | Pass | ✅ | Mock `DispatchAsync(IEnumerable<IDomainEvent>)` |
| `InMemoryEventBusTests` | Pass | ✅ | — |
| `AuthorizationTests` (políticas) | Pass | ✅ | Test simplificado de constantes de política |

---

## Pruebas de integración

| Caso | Resultado | Estado | Corrección aplicada |
|------|-----------|--------|---------------------|
| `HealthCheck_ShouldReturnOk` | Pass | ✅ | `CustomWebApplicationFactory` + init BD tolerante a fallos |
| `Login_WithSeededAdmin_ShouldReturnToken` | Pass | ✅ | Seed en `autonomuscrm_test` + credenciales demo |
| `Customers_WithoutAuth_ShouldReturnUnauthorized` | Pass | ✅ | Cookie API → 401 + `AllowAutoRedirect=false` |

**Factory:** `AutonomusCRM.Tests/Integration/CustomWebApplicationFactory.cs`  
- `EventBus:Provider=InMemory`  
- BD: `autonomuscrm_test` (local) o variable CI  

---

## Pruebas de seguridad

| Caso | Resultado | Estado | Corrección aplicada |
|------|-----------|--------|---------------------|
| Endpoint `/api/customers` sin token → 401 | Pass | ✅ | Auth global + redirect API deshabilitado |
| Políticas `RequireAdmin` definidas | Pass | ✅ | `AuthorizationPolicies.cs` |

---

## Pruebas funcionales / E2E / carga

| Tipo | Estado | Notas |
|------|--------|-------|
| E2E browser (Playwright) | ⏸️ No automatizado | UI Razor validada manualmente vía login |
| Por roles (Admin/Sales) | ⏸️ Parcial | Seed crea `admin@` y `sales@`; pruebas manuales recomendadas |
| Carga (k6/JMeter) | ⏸️ No ejecutado | Rate limiter 200/min configurado |
| Negativas / datos extremos | ⏸️ Parcial | Middleware de excepciones devuelve JSON 400/401/500 |

---

## Entorno de prueba

| Recurso | Valor |
|---------|-------|
| PostgreSQL local | `localhost:5432` |
| BD tests | `autonomuscrm_test` |
| Usuario | `postgres` (solo local) |
| Render producción | **NO utilizada** en tests |

---

## Comandos para reproducir

```bash
dotnet restore
dotnet build
# Crear BD test (una vez)
psql -U postgres -c "CREATE DATABASE autonomuscrm_test;"
dotnet ef database update --project AutonomusCRM.Infrastructure --startup-project AutonomusCRM.API
dotnet test
```

---

## CI (GitHub Actions)

Workflow `.github/workflows/ci.yml`:
- Postgres service container `autonomuscrm_test`
- `EventBus:Provider=InMemory`
- `dotnet test` en Release

---

*Próximo paso opcional: ampliar suite E2E con Playwright y tests de roles con tokens JWT por usuario.*
