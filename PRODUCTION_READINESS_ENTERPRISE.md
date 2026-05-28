# PRODUCTION_READINESS_ENTERPRISE

**Fecha:** 2026-05-27

---

## Checklist producción

| Ítem | Estado | Notas |
|------|--------|-------|
| Build 0 errors | OK | `dotnet build AutonomusCRM.sln` |
| Migraciones EF | OK | `Phase3_DealVersion_WorkflowTasks` |
| Health `/health`, `/health/ready` | OK | P0 API-001 |
| Secrets en env | Parcial | Usar variables en VPS, no appsettings |
| HTTPS / HSTS | Prod config | `Program.cs` condicional |
| PostgreSQL | OK local | Connection string por entorno |
| RabbitMQ + Workers | **Pendiente** | `docker-compose.yml` definido |
| Redis cache | Opcional | Fallback MemoryCache |
| Logging estructurado | Serilog | CorrelationId middleware añadido |
| Multi-tenant QA 2 tenants | OK | QA-B seed |
| Backups BD | Operacional | Fuera de código |
| Rate limiting | OK | 200/min global |

---

## Docker

`docker-compose.yml` incluye: postgres, redis, rabbitmq, api, workers.

**Arranque recomendado VPS:**

```bash
export POSTGRES_PASSWORD='***'
docker compose up -d
```

API expone 8080 en compose; local dev 5154.

---

## Variables críticas

| Variable | Uso |
|----------|-----|
| `ConnectionStrings__DefaultConnection` | PostgreSQL |
| `Jwt__Key` | Firma JWT (≥32 chars) |
| `EventBus__Provider` | `RabbitMQ` o `InMemory` |
| `RabbitMQ__HostName` | Broker |
| `Seed__Enabled` | `false` en producción real |

---

## Gaps antes de SaaS multi-cliente

1. Worker + RabbitMQ validados 24h soak test.
2. Pentest OWASP manual.
3. Import stress automatizado + parser CSV robusto.
4. Global tenant query filter EF.
5. Secret rotation JWT / refresh tokens en vault.

---

## Veredicto readiness

| Nivel | Estado |
|-------|--------|
| Piloto enterprise interno | **LISTO** |
| SaaS multi-tenant público | **NO LISTO** (ver gaps) |
