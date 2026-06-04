# LIVE INTEGRATIONS REPORT

**Programa:** AutonomusFlow Live Integrations  
**Fecha:** 2026-05-28  
**Principio:** Sin PASS inventado — solo evidencia reproducible.

---

## Resumen ejecutivo

| Veredicto | Detalle |
|-----------|---------|
| **Arquitectura** | Integrations Hub, OAuth, Health Center, webhooks y conectores **implementados en código** |
| **Live HTTP** | **No verificado** en este entorno — cero credenciales de proveedor configuradas |
| **Smoke unitario** | **14/14 PASS** (PreConnection, HubSpot OAuth contract, LLM smoke logic, Stripe webhook security, comms status) |
| **Smoke Phase4 (API+PG)** | **BLOCKED** — PostgreSQL/Testcontainers no disponible localmente |
| **Producción lista** | **No** para OpenAI, SendGrid, HubSpot, Salesforce live |

---

## Evidencia de ejecución

### Tests unitarios (2026-05-28)

```text
dotnet test --filter "PreConnection|HubSpotE2E|LlmSmoke|StripeWebhook|CommunicationStatus"
→ Passed: 14, Failed: 0
```

Incluye:
- `PreConnectionCertificationTests` — catálogo 11 providers, smoke BLOCKED sin credenciales, encryption badge
- `HubSpotE2EFlowTests` — OAuth URL cuando ClientId presente; NOT configured sin secrets
- `LlmSmokeServiceTests` — NotConfigured / BlockedNoLiveOptIn / Configured sin live opt-in
- `StripeWebhookSecurityTests` — Production rechaza webhook sin secret
- `CommunicationStatusTests` — detección modo SendGrid vs log

### Variables de entorno (máquina local auditada)

| Variable | Presente |
|----------|----------|
| `AI__ApiKey` / `AI__OpenAI__ApiKey` / `OPENAI_API_KEY` | **No** |
| `Communications__SendGridApiKey` / `SENDGRID_API_KEY` | **No** |
| `IntegrationOAuth__HubSpotClientId` / `HUBSPOT_CLIENT_ID` | **No** |
| `IntegrationOAuth__SalesforceClientId` | **No** |
| `INTEGRATION_SMOKE_LIVE` | **No** |

`deploy/.env.vps` contiene solo infra (Postgres, RabbitMQ, JWT) — **sin keys de integraciones**.

### Phase4 operational (requiere PostgreSQL)

```text
dotnet test --filter Phase4OperationalValidationTests
→ Failed: 6 (fixture PostgreSQL unavailable)
```

Cuando CI tiene PG, `Phase4_Integrations_SendGrid_HubSpot_DocumentBlocked` espera smoke **BLOCKED** sin credenciales (comportamiento correcto documentado).

---

## 1. OpenAI

| Capacidad | Código | Live verificado | Notas |
|-----------|--------|-----------------|-------|
| Provider real HTTP | `OpenAiLlmProvider.cs` | **NO** | Chat completions → `api.openai.com/v1/chat/completions` |
| Embeddings | `ProductionEmbeddingProvider.cs` | **NO** | Fallback local si key ausente |
| Health | `GET /api/ai/llm/health` | **NO** (sin app running + key) | `LlmSmokeService.GetHealth()` |
| Smoke | `POST /api/ai/llm/smoke` | **NO** | Requiere key + `INTEGRATION_SMOKE_LIVE=1` |
| Usage tracking | `ResilientLlmProvider` + `LlmUsageTracker` | **Código sí** | `TotalRequests`, `TotalTokens`, `EstimatedCostUsd` en memoria |
| Cost tracking | `EstimateCost()` en ResilientLlmProvider | **Código sí** | Heurística por tokens; **no persistido en BD** |
| Fallback | `ResilientLlmProvider` chain + circuit breaker | **Código sí** | Embeddings → local deterministic |
| OAuth | — | N/A | API key only: `AI:ApiKey`, `AI:OpenAI:ApiKey` |

**Estado:** **DISCONNECTED** (sin API key en config/env).

**Smoke esperado sin credenciales:** `NotConfigured` o `Configured` (sin live) — **PASS lógico en tests**, **NO live PASS**.

---

## 2. SendGrid

| Capacidad | Código | Live verificado | Notas |
|-----------|--------|-----------------|-------|
| Send email | `SendGridEmailDeliveryProvider.cs` | **NO** | POST `/v3/mail/send` |
| Template | — | **NO IMPLEMENTADO** | Solo HTML body inline |
| Tracking (opens/clicks) | — | **NO IMPLEMENTADO** | — |
| Bounce handling | — | **NO IMPLEMENTADO** | Webhook ingress stub |
| Health | Integration Health Center | **NO live** | Status = Disconnected sin key |
| Smoke API | `POST /api/integrations/smoke/SendGrid` | **BLOCKED** (unit test) | No HTTP call |
| Fallback | `LogEmailDeliveryProvider` | **Código sí** | Cuando `EmailProvider != SendGrid` o key vacía |

**Webhook:** `POST /api/integrations/webhooks/sendgrid` — HMAC custom `X-Autonomus-Signature` (no verificación nativa SendGrid Event Webhook). **No procesa bounces.**

**Estado:** **DISCONNECTED**.

**Credenciales:** `Communications:SendGridApiKey`, `Communications:FromAddress`, `Communications:FromName`, `Communications:EmailProvider=SendGrid`.

---

## 3. HubSpot

| Capacidad | Código | Live verificado | Notas |
|-----------|--------|-----------------|-------|
| OAuth | `IntegrationOAuthService` | **NO live** | Auth URL generada si ClientId+Secret (unit PASS) |
| Contacts sync pull | `HubSpotConnector` CRM v3 | **NO live** | `GET /crm/v3/objects/contacts` |
| Contacts push | `HubSpotConnector` | **NO live** | Max 20 create |
| Companies | — | **NO IMPLEMENTADO** | — |
| Deals | — | **NO IMPLEMENTADO** | — |
| Activities | — | **NO IMPLEMENTADO** | — |
| Sync orchestration | `IntegrationConnectorBase` + Hub | **Código sí** | `POST /api/integrations/sync/HubSpot` |
| Webhook | `IntegrationProviderWebhooksController` | **Stub** | Accept + audit; no CRM processing |

**Estado:** **MISCONFIGURED** (OAuth app credentials vacías en `appsettings.json`).

**Credenciales:** `IntegrationOAuth:HubSpotClientId`, `IntegrationOAuth:HubSpotClientSecret`, `IntegrationOAuth:AppBaseUrl` + tenant OAuth token en `TenantIntegrations`.

---

## 4. Salesforce

| Capacidad | Código | Live verificado | Notas |
|-----------|--------|-----------------|-------|
| OAuth | `IntegrationOAuthService` | **NO live** | Token + `instance_url` |
| Accounts | — | **NO IMPLEMENTADO** | — |
| Contacts pull | `SalesforceConnector` SOQL | **NO live** | `SELECT ... FROM Contact LIMIT 100` |
| Opportunities | — | **NO IMPLEMENTADO** | — |
| Activities | — | **NO IMPLEMENTADO** | — |
| Push | `PushLocalChangesAsync` | **STUB** | Retorna `0` siempre |
| Webhook | Ingress stub | **Stub** | Accept + audit |

**Estado:** **MISCONFIGURED** (OAuth vacío).

**Credenciales:** `IntegrationOAuth:SalesforceClientId`, `IntegrationOAuth:SalesforceClientSecret` + tenant token + `InstanceUrl`.

---

## Integration Center (`/Integrations`)

| Elemento | Estado |
|----------|--------|
| Marketplace cards (HubSpot, SF, Gmail, Outlook, Stripe) | **Implementado** |
| Integration Center — Connected / Disconnected / Pending / Expired | **Implementado** (métricas + tabla 11 providers) |
| OAuth connect button | **Implementado** (si OAuth configured) |
| Manual token connect | **Implementado** |
| Sync button | **Implementado** |
| API health | `GET /api/integrations/health` |

**Limitación UI:** columna Smoke muestra READY/BLOCKED/PENDING según health — **no ejecuta live HTTP** desde UI.

---

## Webhooks

| Provider | Signature | Replay protection | Audit | Processing |
|----------|-----------|-------------------|-------|------------|
| Stripe | Stripe-Signature + secret | Parcial (Stripe SDK) | Log | **Real** billing |
| HubSpot | HMAC `X-Autonomus-Signature` | **NO** | Log | **Stub** |
| Salesforce | HMAC custom | **NO** | Log | **Stub** |
| SendGrid | HMAC custom (not native) | **NO** | Log | **Stub** |
| WhatsApp | Verify token + HMAC | **NO** | Log | Challenge only |

**Gap crítico:** si secret vacío, `ValidateSharedSecret` **acepta cualquier request** (modo dev permisivo).

**OAuth callback:** sin parámetro `state` CSRF (documentado como riesgo R018).

---

## Integrations Hub — inventario técnico

| Componente | Archivo principal |
|------------|-------------------|
| API | `IntegrationsController.cs` |
| Webhooks | `IntegrationProviderWebhooksController.cs` |
| Health | `IntegrationHealthService` |
| Smoke | `IntegrationSmokeTestService` (credential check only) |
| OAuth | `IntegrationOAuthService.cs` |
| Token refresh | `IntegrationTokenRefreshService.cs` |
| Encryption | `IntegrationTokenProtector.cs` |
| Resilience helper | `IntegrationResilience.cs` (**definido, no usado en conectores**) |

**Catálogo:** 11 providers (`IntegrationProviderCatalog.All`).

---

## Respuestas al programa

### 1. Qué integraciones funcionan

| Integración | ¿Funciona live? | Evidencia |
|-------------|-----------------|-----------|
| **OpenAI** | **No** (sin key) | Provider HTTP en código; 0 llamadas live |
| **SendGrid** | **No** (sin key) | Provider HTTP en código; fallback log activo |
| **HubSpot** | **No** (sin OAuth + token) | Connector + OAuth en código; 0 sync live |
| **Salesforce** | **No** (sin OAuth + token) | Pull contacts en código; push stub |
| **Stripe webhook** | **Parcial** | Lógica real si `Stripe:WebhookSecret`; no live test |
| **Log email (fallback)** | **Sí** | Modo default sin SendGrid key |

### 2. Qué requieren credenciales

| Provider | Variables requeridas |
|----------|---------------------|
| OpenAI | `AI:OpenAI:ApiKey` o `AI:ApiKey`; opcional `AI:OpenAI:Model`, `AI:OpenAI:BaseUrl` |
| SendGrid | `Communications:SendGridApiKey`, `FromAddress`, `EmailProvider=SendGrid` |
| HubSpot | `IntegrationOAuth:HubSpotClientId/Secret`, `AppBaseUrl` + OAuth tenant token |
| Salesforce | `IntegrationOAuth:SalesforceClientId/Secret` + OAuth token + `InstanceUrl` |
| Live smoke | `INTEGRATION_SMOKE_LIVE=1` (OpenAI); hub smoke API no hace HTTP aún |
| Token encryption prod | `IntegrationEncryption:Key` (base64 32+ bytes) |
| Webhooks prod | `IntegrationWebhooks:*Secret`, `Stripe:WebhookSecret` |

### 3. Qué smoke tests pasaron

| Test | Resultado | Tipo |
|------|-----------|------|
| `PreConnectionCertificationTests` (4) | **PASS** | Unit |
| `HubSpotE2EFlowTests` (3) | **PASS** | Unit (OAuth contract) |
| `LlmSmokeServiceTests` (4) | **PASS** | Unit (no live HTTP) |
| `StripeWebhookSecurityTests` (1) | **PASS** | Unit |
| `CommunicationStatusTests` (2) | **PASS** | Unit |
| `Phase4_Integrations_SendGrid_HubSpot_DocumentBlocked` | **BLOCKED** (env) | Integration — PG required |
| OpenAI live smoke | **NOT RUN** | Sin key + sin `INTEGRATION_SMOKE_LIVE=1` |
| SendGrid live send | **NOT RUN** | Sin API key |
| HubSpot live sync | **NOT RUN** | Sin OAuth credentials |
| Salesforce live sync | **NOT RUN** | Sin OAuth credentials |

### 4. Qué falta para producción

**P0 — credenciales y live proof**
- Configurar keys reales en staging (OpenAI, SendGrid, HubSpot, Salesforce)
- Ejecutar smoke con `INTEGRATION_SMOKE_LIVE=1` y documentar respuestas HTTP
- Phase4 CI green con PostgreSQL

**P0 — seguridad webhooks**
- Replay protection (idempotency key + timestamp window)
- Rechazar webhooks cuando secret vacío en Production
- OAuth `state` parameter CSRF
- SendGrid native Event Webhook signature verification

**P1 — cobertura CRM**
- HubSpot: Companies, Deals, Activities
- Salesforce: Accounts, Opportunities, Activities, push real
- Wire `IntegrationResilience` en conectores HTTP

**P1 — SendGrid enterprise**
- Dynamic templates
- Event webhook processing (bounce, open, click)
- Bounce suppression list

**P1 — observabilidad**
- Persist LLM usage/cost (hoy in-memory)
- DB audit trail para webhooks (hoy solo logs)

**P2 — hub smoke live HTTP**
- `IntegrationSmokeTestService` hoy solo verifica credenciales — extender opt-in live como LLM smoke

### 5. Qué score Enterprise aumenta

| Área | Score actual (evidencia) | Con live integrations |
|------|--------------------------|------------------------|
| Integration ops readiness | **~93/100** (Pre-Connection scaffold — master context) | +2–3 con Health Center operativo en staging |
| ABOS Truth Sprint Enterprise | **71/100** (sin live smoke/load) | +4–8 si OpenAI+SendGrid+HubSpot live en staging con CI evidence |
| Breadth vs Salesforce/HubSpot | **14–19/100** (master context) | +5–10 solo con Companies/Deals/Opportunities sync |
| **Comprabilidad $50k enterprise** | **Condicional** | Requiere SSO + **live integrations** + webhook hardening |

**Incremento honesto hoy (solo código audit + unit tests):** **+0–1** en Enterprise — arquitectura ya contabilizada en Pre-Connection; **sin evidencia live no sube score materialmente**.

**Incremento potencial post-live (con evidencia CI/staging):** **+4–8 Enterprise**, **+10–15 integration breadth** si P0+P1 completados.

---

## Checklist prioridad (estado real)

| # | Provider | Connected | Live smoke | Production ready |
|---|----------|-----------|------------|------------------|
| 1 | OpenAI | ❌ | ❌ | ❌ |
| 2 | SendGrid | ❌ | ❌ | ❌ |
| 3 | HubSpot | ❌ | ❌ | ❌ |
| 4 | Salesforce | ❌ | ❌ | ❌ |

---

## Próximos pasos recomendados (orden)

1. Añadir secrets a staging (`deploy/.env.vps` o secrets CI) — **no commitear**
2. `INTEGRATION_SMOKE_LIVE=1` → `POST /api/ai/llm/smoke?provider=openai`
3. Configurar SendGrid sandbox → enviar email test → capturar Message-ID
4. HubSpot developer app → OAuth → sync contacts → verificar import count
5. Salesforce connected app → OAuth → pull contacts
6. Re-ejecutar Phase4 en CI con PostgreSQL
7. Actualizar este reporte con respuestas HTTP reales (status codes, timestamps)

---

*Generado por Live Integrations Program — evidencia: dotnet test 14/14 unit, env audit 2026-05-28, code review Integrations Hub.*
