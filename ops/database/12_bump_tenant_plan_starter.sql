-- Eleva tenant a plan starter (10 usuarios) para pruebas QA
-- Uso: psql -v tenant_id='<uuid>' -f 12_bump_tenant_plan_starter.sql

INSERT INTO "TenantBillingAccounts" ("Id", "TenantId", "PlanId", "Status", "MaxUsers", "MaxCustomers")
SELECT gen_random_uuid(), :'tenant_id'::uuid, 'starter', 'trialing', 10, 2000
WHERE NOT EXISTS (
    SELECT 1 FROM "TenantBillingAccounts" WHERE "TenantId" = :'tenant_id'::uuid
);

UPDATE "TenantBillingAccounts"
SET "PlanId" = 'starter', "MaxUsers" = 10, "MaxCustomers" = 2000
WHERE "TenantId" = :'tenant_id'::uuid;
