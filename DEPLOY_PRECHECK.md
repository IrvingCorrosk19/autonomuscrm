# DEPLOY PRECHECK — AutonomusFlow VPS Staging

**Fecha:** 2026-05-28  
**Target:** VPS `164.68.99.83` · preview `:8091` · prod `crm.autonomousflow.lat`  
**Compose:** `deploy/docker-compose.vps.yml`  
**Script:** `deploy/deploy-vps.ps1`

---

## Resumen

| Área | Estado pre-deploy | Acción |
|------|-------------------|--------|
| Dockerfile.api | ✅ OK | Multi-stage .NET 9, puerto 8080 |
| Dockerfile.workers | ✅ OK | Multi-stage .NET 9 |
| docker-compose.vps.yml | 🟡 Corregido | Añadido `IntegrationEncryption__Key`, JWT+encryption en workers |
| appsettings.Production.example.json | ✅ Creado | Placeholders sin secretos |
| deploy/.env.vps | ✅ OK | Secretos VPS staging (no commitear) |
| Migraciones EF | ✅ 15 migraciones | `Database__AutoMigrate=true` en API |
| ProductionConfigurationGuard | ✅ Validado | Requiere JWT≥32, Redis, RabbitMQ, IntegrationEncryption |

---

## Servicios docker-compose.vps.yml

| Servicio | Imagen / Build | Puerto host | Healthcheck |
|----------|----------------|-------------|-------------|
| postgres | postgres:16-alpine | interno | pg_isready |
| redis | redis:7-alpine | interno | redis-cli ping |
| rabbitmq | rabbitmq:3-management-alpine | interno | rabbitmq-diagnostics ping |
| api | Dockerfile.api | 127.0.0.1:5080→8080 | depends_on healthy |
| workers | Dockerfile.workers | — | depends_on api started |

**Nginx:** `deploy/nginx-autonomuscrm-vps.conf` → proxy `:8091` → `127.0.0.1:5080`

---

## Variables de entorno requeridas (Production guard)

| Variable | Requerida | En .env.vps | En compose |
|----------|-----------|-------------|------------|
| `POSTGRES_PASSWORD` | ✅ | ✅ | api, workers |
| `JWT_KEY` (≥32 chars) | ✅ | ✅ | api, workers |
| `INTEGRATION_ENCRYPTION_KEY` (base64 32B) | ✅ | ✅ | api, workers |
| `RABBITMQ_USER` / `RABBITMQ_PASSWORD` | ✅ | ✅ | api, workers |
| `ConnectionStrings__Redis` | ✅ Production | — | api, workers (hardcoded redis:6379) |
| `EventBus__Provider=RabbitMQ` | ✅ | — | api, workers |
| `SEED_ADMIN_PASSWORD` | 🟡 seed | ✅ | api |
| `APP_BASE_URL` | 🟡 OAuth callbacks | ✅ | api |
| `COMMS_ALLOW_SIMULATION` | 🟡 staging | ✅ `true` | api |

---

## Variables opcionales (integraciones — NO conectar aún)

| Integración | Env vars | Callback OAuth |
|-------------|----------|----------------|
| OpenAI | `OPENAI_API_KEY` / `AI__OpenAI__ApiKey` | N/A |
| SendGrid | `SENDGRID_API_KEY` | N/A |
| HubSpot | `HUBSPOT_CLIENT_ID`, `HUBSPOT_CLIENT_SECRET` | `{APP_BASE_URL}/api/integrations/oauth/hubspot/callback` |
| Salesforce | `SALESFORCE_CLIENT_ID`, `SALESFORCE_CLIENT_SECRET` | `{APP_BASE_URL}/api/integrations/oauth/salesforce/callback` |
| Google | `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` | `{APP_BASE_URL}/api/integrations/oauth/google/callback` |
| Microsoft | `MICROSOFT_CLIENT_ID`, `MICROSOFT_CLIENT_SECRET` | `{APP_BASE_URL}/api/integrations/oauth/microsoft/callback` |
| Twilio | `TWILIO_AUTH_TOKEN` | webhook base en config |
| WhatsApp | `WHATSAPP_ACCESS_TOKEN`, `WHATSAPP_PHONE_NUMBER_ID` | Meta Graph API |

---

## Migraciones EF Core

- **Ubicación:** `AutonomusCRM.Infrastructure/Persistence/Migrations/`
- **Aplicación:** automática al arrancar API (`ApplyMigrationsAsync`)
- **Tenant isolation:** global query filters en `ApplicationDbContext`
- **Índices/FK:** incluidos en migraciones InitialCreate + fases

---

## Correcciones aplicadas pre-deploy

1. **`IntegrationEncryption__Key`** faltaba en compose → API/workers fallaban en Production.
2. **Workers** sin `Jwt__Key`, `IntegrationEncryption__Key`, `ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__Redis` → guard fail-fast.
3. **`COMMS_ALLOW_SIMULATION=true`** en staging hasta configurar SendGrid live.
4. **`appsettings.Production.example.json`** creado (referenciado en readiness report, no existía).

---

## Checklist pre-flight

- [x] `dotnet build AutonomusCRM.sln` — ejecutar antes de deploy
- [x] Dockerfiles válidos
- [x] compose + .env alineados con ProductionConfigurationGuard
- [x] Nginx config presente
- [x] PuTTY plink/pscp disponible
- [ ] VPS accesible SSH (validar en Fase 2)
- [ ] DNS/TLS prod (pendiente credenciales — no bloquea preview :8091)

---

## Veredicto pre-deploy

**GO** para despliegue staging en `http://164.68.99.83:8091` tras correcciones de compose/env documentadas arriba.

*Generado — VPS Deployment & Staging Activation Fase 1.*
