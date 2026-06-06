-- AutonomusCRM VPS Test — Base limpia post-migraciones
-- Empresa: TechSolutions Panama
-- Password todos los usuarios: AutonomusTest123!
-- BCrypt hash generado 2026-06-05
-- Ejecutar DESPUES de dotnet ef / AutoMigrate y ANTES de 05_FUNCTIONAL_TEST_DATA.sql
-- Idempotente: borra datos previos del tenant de prueba y reinserta

\set ON_ERROR_STOP on

-- GUIDs fijos tenant de prueba
-- Tenant: b1000000-0000-4000-8000-000000000001

BEGIN;

-- Limpiar tenant de prueba por nombre o GUID fijo (re-ejecutable)
DELETE FROM "WorkflowTasks" WHERE "TenantId" IN (SELECT "Id" FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001');
DELETE FROM "AiDecisionAudits" WHERE "TenantId" IN (SELECT "Id" FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001');
DELETE FROM "DomainEvents" WHERE "TenantId" IN (SELECT "Id" FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001');
DELETE FROM "ProductUsageEvents" WHERE "TenantId" IN (SELECT "Id" FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001');
DELETE FROM "Deals" WHERE "TenantId" IN (SELECT "Id" FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001');
DELETE FROM "Leads" WHERE "TenantId" IN (SELECT "Id" FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001');
DELETE FROM "Customers" WHERE "TenantId" IN (SELECT "Id" FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001');
DELETE FROM "Workflows" WHERE "TenantId" IN (SELECT "Id" FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001');
DELETE FROM "Policies" WHERE "TenantId" IN (SELECT "Id" FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001');
DELETE FROM "Users" WHERE "TenantId" IN (SELECT "Id" FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001');
DELETE FROM "TenantBillingAccounts" WHERE "TenantId" IN (SELECT "Id" FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001');
DELETE FROM "Tenants" WHERE "Name" = 'TechSolutions Panama' OR "Id" = 'b1000000-0000-4000-8000-000000000001';

-- Tenant
INSERT INTO "Tenants" (
    "Id", "Name", "Description", "IsActive", "IsKillSwitchEnabled", "Settings",
    "SubscriptionExpiresAt", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
) VALUES (
    'b1000000-0000-4000-8000-000000000001',
    'TechSolutions Panama',
    'Tenant de pruebas funcionales VPS — primer cliente simulado',
    true,
    false,
    '{
        "trust.approvalThreshold": "70",
        "ai.enabled": "false",
        "ai.testMode": "true",
        "communications.allowSimulation": "true",
        "billing.testMode": "true",
        "region": "us-east-1",
        "timezone": "America/Panama"
    }'::jsonb,
    NOW() AT TIME ZONE 'UTC' + INTERVAL '14 days',
    NOW() AT TIME ZONE 'UTC',
    NULL,
    'vps-test-seed',
    NULL
);

-- Billing (plan starter = 10 usuarios)
INSERT INTO "TenantBillingAccounts" (
    "Id", "TenantId", "PlanId", "StripeCustomerId", "StripeSubscriptionId",
    "Status", "CurrentPeriodEnd", "MaxUsers", "MaxCustomers"
) VALUES (
    'b1000002-0000-4000-8000-000000000001',
    'b1000000-0000-4000-8000-000000000001',
    'starter',
    NULL,
    NULL,
    'trialing',
    NOW() AT TIME ZONE 'UTC' + INTERVAL '30 days',
    10,
    2000
);

-- Password: AutonomusTest123!
-- NOTA: superadmin@ no existe como rol en codigo — se mapea a Admin (maximo privilegio)
INSERT INTO "Users" (
    "Id", "TenantId", "Email", "PasswordHash", "FirstName", "LastName",
    "IsActive", "IsEmailVerified", "MfaEnabled", "MfaSecret", "LastLoginAt",
    "Roles", "Claims", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
) VALUES
(
    'b1000001-0000-4000-8000-000000000001',
    'b1000000-0000-4000-8000-000000000001',
    'superadmin@autonomuscrm.local',
    '$2a$11$hOsKcM44lZ5yDelfLT2RbOJ8DuvD4r2QuNSIwAqutgDfH0r9KI782',
    'Super', 'Admin',
    true, true, false, NULL, NULL,
    '["Admin"]'::jsonb, '{}'::jsonb,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
),
(
    'b1000001-0000-4000-8000-000000000002',
    'b1000000-0000-4000-8000-000000000001',
    'admin@autonomuscrm.local',
    '$2a$11$hOsKcM44lZ5yDelfLT2RbOJ8DuvD4r2QuNSIwAqutgDfH0r9KI782',
    'Admin', 'Operaciones',
    true, true, false, NULL, NULL,
    '["Admin"]'::jsonb, '{}'::jsonb,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
),
(
    'b1000001-0000-4000-8000-000000000003',
    'b1000000-0000-4000-8000-000000000001',
    'manager@autonomuscrm.local',
    '$2a$11$hOsKcM44lZ5yDelfLT2RbOJ8DuvD4r2QuNSIwAqutgDfH0r9KI782',
    'Roberto', 'Castillo',
    true, true, false, NULL, NULL,
    '["Manager"]'::jsonb, '{}'::jsonb,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
),
(
    'b1000001-0000-4000-8000-000000000004',
    'b1000000-0000-4000-8000-000000000001',
    'sales1@autonomuscrm.local',
    '$2a$11$hOsKcM44lZ5yDelfLT2RbOJ8DuvD4r2QuNSIwAqutgDfH0r9KI782',
    'Ana', 'Rodriguez',
    true, true, false, NULL, NULL,
    '["Sales"]'::jsonb, '{}'::jsonb,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
),
(
    'b1000001-0000-4000-8000-000000000005',
    'b1000000-0000-4000-8000-000000000001',
    'sales2@autonomuscrm.local',
    '$2a$11$hOsKcM44lZ5yDelfLT2RbOJ8DuvD4r2QuNSIwAqutgDfH0r9KI782',
    'Diego', 'Herrera',
    true, true, false, NULL, NULL,
    '["Sales"]'::jsonb, '{}'::jsonb,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
),
(
    'b1000001-0000-4000-8000-000000000006',
    'b1000000-0000-4000-8000-000000000001',
    'support@autonomuscrm.local',
    '$2a$11$hOsKcM44lZ5yDelfLT2RbOJ8DuvD4r2QuNSIwAqutgDfH0r9KI782',
    'Maria', 'Gomez',
    true, true, false, NULL, NULL,
    '["Support"]'::jsonb, '{}'::jsonb,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
),
(
    'b1000001-0000-4000-8000-000000000007',
    'b1000000-0000-4000-8000-000000000001',
    'viewer@autonomuscrm.local',
    '$2a$11$hOsKcM44lZ5yDelfLT2RbOJ8DuvD4r2QuNSIwAqutgDfH0r9KI782',
    'Pedro', 'Santos',
    true, true, false, NULL, NULL,
    '["Viewer"]'::jsonb, '{}'::jsonb,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
);

-- Policy base (lectura comercial para Support/Viewer via UI middleware; ABAC allow si vacio)
INSERT INTO "Policies" (
    "Id", "TenantId", "Name", "Description", "Expression", "IsActive",
    "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
) VALUES (
    'b1000003-0000-4000-8000-000000000001',
    'b1000000-0000-4000-8000-000000000001',
    'Base Test Policy',
    'Politica minima de prueba — allow comercial para roles operativos',
    'role in (Admin, Manager, Sales)',
    true,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
);

-- Workflows minimos (2 activos, 2 inactivos)
INSERT INTO "Workflows" (
    "Id", "TenantId", "Name", "Description", "IsActive",
    "Triggers", "Conditions", "Actions", "ExecutionCount", "LastExecutedAt",
    "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
) VALUES
(
    'b1000004-0000-4000-8000-000000000001',
    'b1000000-0000-4000-8000-000000000001',
    'Auto-asignar lead nuevo',
    'Asigna lead a sales1 al crear',
    true,
    '[{"type":"DomainEvent","eventType":"LeadCreatedEvent"}]'::jsonb,
    '[]'::jsonb,
    '[{"type":"Assign","target":"Lead","parameters":{"userId":"b1000001-0000-4000-8000-000000000004"}}]'::jsonb,
    0, NULL,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
),
(
    'b1000004-0000-4000-8000-000000000002',
    'b1000000-0000-4000-8000-000000000001',
    'Tarea seguimiento deal',
    'Crea tarea al avanzar deal',
    true,
    '[{"type":"DomainEvent","eventType":"DealStageUpdatedEvent"}]'::jsonb,
    '[]'::jsonb,
    '[{"type":"CreateTask","target":"Deal","parameters":{"title":"Seguimiento pipeline","userId":"b1000001-0000-4000-8000-000000000003","priority":"High"}}]'::jsonb,
    0, NULL,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
),
(
    'b1000004-0000-4000-8000-000000000003',
    'b1000000-0000-4000-8000-000000000001',
    'Workflow inactivo — email campana',
    'Desactivado para pruebas',
    false,
    '[{"type":"DomainEvent","eventType":"LeadCreatedEvent"}]'::jsonb,
    '[]'::jsonb,
    '[{"type":"UpdateStatus","target":"Lead","parameters":{"status":"Contacted"}}]'::jsonb,
    0, NULL,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
),
(
    'b1000004-0000-4000-8000-000000000004',
    'b1000000-0000-4000-8000-000000000001',
    'Workflow inactivo — churn',
    'Desactivado para pruebas',
    false,
    '[{"type":"DomainEvent","eventType":"CustomerUpdatedEvent"}]'::jsonb,
    '[]'::jsonb,
    '[{"type":"CreateTask","target":"Customer","parameters":{"title":"Revision churn","priority":"Normal"}}]'::jsonb,
    0, NULL,
    NOW() AT TIME ZONE 'UTC', NULL, 'vps-test-seed', NULL
);

COMMIT;

SELECT 'Tenants' AS entity, COUNT(*)::text AS cnt FROM "Tenants" WHERE "Id" = 'b1000000-0000-4000-8000-000000000001'
UNION ALL SELECT 'Users', COUNT(*)::text FROM "Users" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001'
UNION ALL SELECT 'Workflows', COUNT(*)::text FROM "Workflows" WHERE "TenantId" = 'b1000000-0000-4000-8000-000000000001';
