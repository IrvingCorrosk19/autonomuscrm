# AUTONOMUSFLOW — PRODUCTION READINESS

**Versión:** v0.9 · **Go/No-Go comercial:** 🟡 **CONDICIONAL** (POC/demo ✅ · Enterprise 🟡)

## Checklist técnico

| # | Criterio | Estado |
|---|----------|--------|
| 1 | API compila | ✅ |
| 2 | Tests ≥40 pass | ✅ **45** |
| 3 | Migraciones auto prod | ✅ Phase20 incluida |
| 4 | Outcome Fabric | ✅ |
| 5 | Comms retry + audit | ✅ |
| 6 | Comms reales VPS | 🟡 Requiere `SENDGRID_API_KEY` |
| 7 | OAuth refresh API | ✅ |
| 8 | Integración E2E prod | ❌ Keys pendientes |
| 9 | Identity merge auto | ✅ |
| 10 | CDP event stream | ✅ |
| 11 | Trust SLA alerts | ✅ |
| 12 | Twilio webhook | ✅ (token pendiente) |
| 13 | SCIM Groups | ✅ |
| 14 | SAML metadata | ✅ ACS login ❌ |
| 15 | AI Command Center | ✅ ampliado |
| 16 | VPS 7d estable | ❌ |

## Variables VPS requeridas (v0.9)

```
SENDGRID_API_KEY=
HUBSPOT_CLIENT_ID=
HUBSPOT_CLIENT_SECRET=
COMMS_ALLOW_SIMULATION=false
TWILIO_AUTH_TOKEN=
```

## Madurez por criterio programa 95+

| Criterio | v0.9 |
|----------|------|
| Comms Live (código) | 🟡 |
| OAuth Live (código) | 🟡 |
| 1 integración E2E prod | ❌ |
| Outcome Fabric | ✅ |
| Voice funcional | 🟡 |
| SAML funcional | 🟡 metadata only |
| SCIM Groups | ✅ |
| Customer360 | ✅ |
| AI Command Center | ✅ |
| 40+ tests | ✅ |
| VPS 7d | ❌ |
| **Madurez** | **~90** |

## Iteración UI v2 — Command & Trust

| Criterio Fase 2 | Estado |
|-----------------|--------|
| Post-login = Flow Command | ✅ `/` |
| Agents sin números fake | ✅ |
| Trust 3-column studio | ✅ |
| Revenue hero datos reales | ✅ (vacío honesto si sin audits) |
| Command sub-rutas | ✅ |

**UI readiness comercial:** ~82/100 (sube desde ~70 post Fase 1).

## Para Go pleno (95+)

1. Desplegar v0.9 en VPS con keys
2. Validar 1 email SendGrid + 1 sync HubSpot
3. SAML ACS
4. 7 días sin reinicio worker/API
5. ~~Fase 3 Revenue OS + Customer 360 detail~~ ✅ (2026-05-28)
6. Playwright E2E con PostgreSQL en CI
7. Deals pipeline visual Flow (Fase 4)
