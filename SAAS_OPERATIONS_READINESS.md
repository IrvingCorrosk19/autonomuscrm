# SAAS_OPERATIONS_READINESS

## Implementado

`ITenantProvisioningService` / `TenantProvisioningService`:

- `ProvisionTenantAsync` — tenant + admin
- `SuspendTenantAsync` / `ResumeTenantAsync`

## Roadmap operaciones

| Capacidad | Estado |
|-----------|--------|
| Tenant quotas (leads/deals max) | Roadmap |
| Billing hooks (Stripe) | Roadmap |
| Feature flags por tenant | Roadmap |
| Support impersonation (auditado) | Roadmap |
| Export audit JSON | OK (Audit page) |
| Kill switch tenant | Dominio existe |

## Onboarding flujo sugerido

1. Provision tenant API (admin sistema, bypass filter)
2. Email admin tenant
3. Login → TenantScope activo
4. Workers procesan con `TenantId` del evento
