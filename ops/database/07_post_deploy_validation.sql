-- 07_post_deploy_validation.sql — Validación post-despliegue
\echo '=== Post-deploy validation ==='

SELECT CASE WHEN COUNT(*) >= 1 THEN 'OK' ELSE 'FAIL' END AS admin_user
FROM "Users" WHERE "Email" = 'admin@autonomuscrm.local';

SELECT COUNT(*) AS migration_count FROM "__EFMigrationsHistory";
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;

SELECT 'Customers' AS t, COUNT(*) FROM "Customers"
UNION ALL SELECT 'Leads', COUNT(*) FROM "Leads"
UNION ALL SELECT 'Deals', COUNT(*) FROM "Deals"
UNION ALL SELECT 'WorkflowTasks', COUNT(*) FROM "WorkflowTasks";

SELECT indexname FROM pg_indexes WHERE schemaname = 'public'
  AND indexname IN (
    'IX_DomainEvents_TenantId_OccurredOn',
    'IX_DomainEvents_TenantId_EventType',
    'IX_Leads_TenantId_Email',
    'IX_WorkflowTasks_TenantId_AssignedToUserId_Status'
  ) ORDER BY indexname;
