-- AutonomusCRM: limpieza para pruebas manuales
-- CONSERVA: Tenants, Users, Policies, TenantIntegrations, TenantBillingAccounts, ScimGroups
-- ELIMINA: clientes, leads, deals, tareas, IA, memoria, auditoría operativa, métricas
-- Ejecutar solo tras backup. Idempotente.

BEGIN;

-- Marca tenant CEO_DEMO para no re-sembrar dataset ejecutivo (requiere CeoDemoSeeder actualizado)
UPDATE "Tenants"
SET "Settings" = COALESCE("Settings", '{}'::jsonb) || '{"CeoDemo:SkipDataset":"true"}'::jsonb
WHERE "Name" = 'CEO_DEMO' OR "Id" = 'c0e00000-0000-4000-8000-000000000001';

-- Hijos primero (TRUNCATE CASCADE en tablas operativas)
TRUNCATE TABLE
    "WorkflowTasks",
    "VoiceCallLogs",
    "SalesQuotas",
    "ProductUsageEvents",
    "NbaOutcomeRecords",
    "MemoryEmbeddings",
    "CustomerMemoryProfiles",
    "CustomerFeedbacks",
    "CustomerContracts",
    "CustomerCommunicationLogs",
    "CustomerAnalyticsSnapshots",
    "CdpStreamEvents",
    "BusinessMemoryObservations",
    "BusinessMemoryLearnings",
    "BusinessMemoryInsights",
    "BusinessMemoryRelationships",
    "BusinessMemoryOutcomes",
    "BusinessMemoryDecisions",
    "BusinessMemoryEvents",
    "BusinessMemoryFacts",
    "BusinessMemoryContexts",
    "BusinessMemories",
    "BusinessKnowledgeGraphEdges",
    "BusinessKnowledgeRecords",
    "AutonomousPlaybookStates",
    "AiApprovalRequests",
    "AiDecisionAudits",
    "MlFeatureSnapshots",
    "MlDriftReports",
    "MlPipelineRuns",
    "FailedEventMessages",
    "DomainEvents",
    "Snapshots",
    "TimeSeriesMetrics",
    "Deals",
    "Leads",
    "Customers",
    "Workflows"
RESTART IDENTITY CASCADE;

COMMIT;

-- Verificación
SELECT 'Tenants' AS entity, COUNT(*)::text AS remaining FROM "Tenants"
UNION ALL SELECT 'Users', COUNT(*)::text FROM "Users"
UNION ALL SELECT 'Customers', COUNT(*)::text FROM "Customers"
UNION ALL SELECT 'Leads', COUNT(*)::text FROM "Leads"
UNION ALL SELECT 'Deals', COUNT(*)::text FROM "Deals"
UNION ALL SELECT 'WorkflowTasks', COUNT(*)::text FROM "WorkflowTasks"
UNION ALL SELECT 'DomainEvents', COUNT(*)::text FROM "DomainEvents";
