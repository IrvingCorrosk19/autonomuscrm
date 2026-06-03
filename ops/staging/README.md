# Staging Environment — AutonomusFlow

Minimal staging stack for validation (health, integration smoke, k6 baseline).

## Prerequisites

- Docker Desktop running
- Copy env template and fill secrets (never commit `.env.staging`)

## Quick start (infrastructure)

```bash
cp ops/staging/.env.staging.example ops/staging/.env.staging
docker compose -f ops/staging/docker-compose.staging.yml up -d
```

Run API locally against staging infra:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Staging"
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=autonomuscrm_staging;Username=postgres;Password=staging_password"
$env:ConnectionStrings__Redis="localhost:6380"
$env:RabbitMQ__HostName="localhost"
$env:RabbitMQ__Port="5673"
$env:Jwt__Key="Staging-Jwt-Secret-Key-Minimum-32-Chars!"
$env:IntegrationEncryption__Key="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="
dotnet run --project AutonomusCRM.API
```

## Required environment variables

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL |
| `ConnectionStrings__Redis` | Redis (recommended) |
| `RabbitMQ__HostName` | RabbitMQ host |
| `Jwt__Key` | Min 32 chars |
| `IntegrationEncryption__Key` | Base64 32+ bytes |
| `Seed__AdminPassword` | Initial admin password |
| `AI__OpenAI__ApiKey` | Optional — LLM live smoke |

## Health validation

```bash
curl http://localhost:8080/health
curl http://localhost:8080/health/ready
```

Expected: HTTP 200, database + eventbus + cache checks pass.

## LLM live smoke (optional)

Set on API container:
- `INTEGRATION_SMOKE_LIVE=1`
- `AI__OpenAI__ApiKey=sk-...`

```bash
# After login, obtain JWT
curl -H "Authorization: Bearer $TOKEN" http://localhost:8080/api/ai/llm/health
curl -X POST -H "Authorization: Bearer $TOKEN" "http://localhost:8080/api/ai/llm/smoke?provider=openai"
```

## k6 baseline

```bash
export BASE_URL=http://localhost:8080
export TENANT_ID=<tenant-guid-from-seed>
./ops/load/run-baseline.sh
```

## Production guards

Staging/Production startup validates:
- No InMemory event bus
- JWT + IntegrationEncryption keys present
- RabbitMQ host configured
- Production requires Redis
- Log email/WhatsApp blocked when `AllowSimulation=false`
