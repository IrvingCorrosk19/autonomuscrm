-- AutonomusCRM: slate limpio para primer cliente (BORRA tenants y usuarios)
-- Usar solo en entornos de prueba. Ejecutar tras backup.
-- Idempotente.

BEGIN;

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
    "Workflows",
    "Policies",
    "TenantIntegrations",
    "TenantBillingAccounts",
    "Users",
    "Tenants"
RESTART IDENTITY CASCADE;

COMMIT;

SELECT 'Tenants' AS entity, COUNT(*)::text AS remaining FROM "Tenants"
UNION ALL SELECT 'Users', COUNT(*)::text FROM "Users"
UNION ALL SELECT 'Customers', COUNT(*)::text FROM "Customers";
