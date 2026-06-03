# AUTONOMUSFLOW — TEST EVIDENCE

**Última ejecución:** 2026-06-02 · **Comando:** `dotnet test --filter "Category!=Integration"`

## Resumen

| Métrica | Valor |
|---------|-------|
| Build | ✅ 0 errors |
| Unit tests | **45/45 pass** |
| Integration (Testcontainers) | 1 fixture + 1 test (skip si no Docker) |
| Meta 40+ tests | ✅ **Cumplida** |

## Nuevas suites v0.9

| Suite | Tests | Archivo |
|-------|-------|---------|
| Outcome Fabric | 3 | `OutcomeFabricTests.cs` |
| SCIM Groups | 2 | `ScimGroupTests.cs` |
| SAML metadata | 2 | `SamlMetadataTests.cs` |
| CDP stream | 1 | `CdpStreamEventTests.cs` |
| Comms options | 2 | `CommunicationOptionsTests.cs` |
| HubSpot E2E contract | 3 | `HubSpotE2EFlowTests.cs` |
| Twilio webhook | 2 | `TwilioWebhookTests.cs` |
| Tenant isolation | 2 | `TenantIsolationTests.cs` |

## HubSpot E2E (documentado — prod pendiente)

1. Configurar `HUBSPOT_CLIENT_ID/SECRET` en VPS
2. `GET /Integrations` → Conectar HubSpot → OAuth callback
3. `POST /api/integrations/tokens/refresh`
4. `POST /api/integrations/sync/HubSpot`
5. `GET /api/integrations/sync/HubSpot/conflicts`

## Comando reproducible

```bash
dotnet build
dotnet test --filter "Category!=Integration"
dotnet test --filter "Category=Integration"   # requiere Docker
```

## Pendiente v0.10

- Stripe webhook signature test
- Customer360 integration con Testcontainers
- Playwright Trust Inbox E2E
