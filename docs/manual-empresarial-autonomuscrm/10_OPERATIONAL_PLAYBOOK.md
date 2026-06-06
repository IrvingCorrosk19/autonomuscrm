# 10 — Playbook Operativo Empresarial

## Playbook 1 — Lead inbound (formulario web → CRM)

1. Lead creado vía API/import/UI → `LeadStatus.New`
2. `LeadCreatedEvent` → SLA tarea 24h + scoring worker
3. **Sales:** contactar en <24h, cambiar a Contacted
4. Si califica → **Qualify** → revisar tarea auto + deal borrador
5. Actualizar deal con monto real → mover etapas
6. Cierre → Close deal → onboarding tasks CS

## Playbook 2 — Deal estancado

1. Revenue OS o tarea auto señala deal
2. Sales actualiza etapa o ExpectedCloseDate
3. Si perdido → Lose con razón
4. Manager revisa win rate semanal

## Playbook 3 — Cliente en riesgo (Risk >70)

1. Alerta en `/Customers` métricas
2. Support/CS abre ticket en `/customer-success`
3. Sales coordina si hay oportunidad expansión
4. Retention automation puede ejecutar playbook Rescue

## Playbook 4 — Decisión IA pendiente

1. Manager abre `/TrustInbox`
2. Revisa explicación Outcome Fabric
3. Approve / Reject / Rollback
4. Audit registra decisión

## Playbook 5 — Deploy producción

1. `backup-vps.ps1`
2. `deploy-vps.ps1`
3. `apply-db-optimization-vps.ps1` (incluido en deploy)
4. Validar `/health`, login ES/EN, `07_post_deploy_validation.sql`

## Playbook 6 — Rollback

1. Restaurar `autonomuscrm-optimized.dump` o backup timestamp
2. Restaurar tar app desde `/opt/autonomuscrm-backups/`
3. `docker compose up -d --force-recreate`
4. Ejecutar `08_rollback.sql` si índices Phase2 problemáticos
