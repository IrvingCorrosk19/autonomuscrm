# AUTONOMUSFLOW — GAPS AND RISKS

**Versión:** v0.9 · **Madurez:** ~90/100

## Matriz por área (actualizada)

| Área | Existe | Falta | Riesgo | P | Estado v0.9 |
|------|--------|-------|--------|---|-------------|
| **Comms** | SendGrid/SES/WA + retry audit | Keys VPS + envío E2E probado | Alto | P0 | 🟡 Código listo |
| **Integrations** | OAuth + refresh + conflicts | Sync HubSpot prod documentado | Alto | P0 | 🟡 |
| **Outcome Fabric** | Servicio + attribution | 100% decisiones históricas | Medio | P1 | 🟢 |
| **Data Cloud** | Merge+stream+export | Warehouse BigQuery/Snowflake | Medio | P2 | 🟢 MVP |
| **Trust** | SLA alerts + policy | Explainability LLM | Medio | P2 | 🟢 |
| **Voice** | Twilio webhook | Recording + transcripción IA | Medio | P1 | 🟡 |
| **Governance** | SCIM Groups + SAML metadata | SAML ACS login | Alto | P1 | 🟡 |
| **Tests** | 45 unit + Testcontainers | CI Docker obligatorio | Medio | P1 | 🟢 |

## Riesgos activos

| ID | Riesgo | Sev. | Mitigación v0.9 |
|----|--------|------|-------------------|
| R1 | Keys comms no en VPS | P0 | compose exige SendGrid; `AllowSimulation=false` |
| R2 | OAuth sin client IDs | P0 | vars en docker-compose.vps.yml |
| R3 | Ecosistema SF | Estratégico | ABOS positioning |
| R4 | Integration tests sin Docker CI | P1 | Testcontainers fixture añadido |
| R7 | Identity merge | — | **Resuelto** `POST /api/data/identity/merge` |

## Impedimento Salesforce

**Técnico:** Falta validación prod (comms live, 1 sync E2E, SAML login, 7d VPS).  
**Estratégico:** AppExchange, SOC2 audit, verticales, migración datos históricos.
