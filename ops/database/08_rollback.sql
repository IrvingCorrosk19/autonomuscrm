-- 08_rollback.sql — Revertir índices de 03_indexes_optimization.sql (reversible)
-- NO elimina datos. Ejecutar solo si rollback de optimización de índices es necesario.

DROP INDEX CONCURRENTLY IF EXISTS "IX_DomainEvents_TenantId_OccurredOn";
DROP INDEX CONCURRENTLY IF EXISTS "IX_DomainEvents_TenantId_EventType";
DROP INDEX CONCURRENTLY IF EXISTS "IX_Leads_TenantId_Email";
DROP INDEX CONCURRENTLY IF EXISTS "IX_WorkflowTasks_TenantId_AssignedToUserId_Status";
DROP INDEX CONCURRENTLY IF EXISTS "IX_WorkflowTasks_TenantId_Status_DueDate";
DROP INDEX CONCURRENTLY IF EXISTS "IX_Deals_TenantId_ExpectedCloseDate";

\echo 'Index rollback complete. To restore DB from backup:'
\echo '  pg_restore -U postgres -d autonomuscrm_restored /path/to/autonomuscrm.dump'
