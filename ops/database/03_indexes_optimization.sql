-- 03_indexes_optimization.sql — Índices justificados (idempotente)
-- Justificación: consultas Audit (tenant+fecha), Audit (tenant+tipo), Leads búsqueda email, Tasks asignación/vencimiento

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_DomainEvents_TenantId_OccurredOn"
  ON "DomainEvents" ("TenantId", "OccurredOn" DESC);

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_DomainEvents_TenantId_EventType"
  ON "DomainEvents" ("TenantId", "EventType");

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Leads_TenantId_Email"
  ON "Leads" ("TenantId", "Email");

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_WorkflowTasks_TenantId_AssignedToUserId_Status"
  ON "WorkflowTasks" ("TenantId", "AssignedToUserId", "Status");

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_WorkflowTasks_TenantId_Status_DueDate"
  ON "WorkflowTasks" ("TenantId", "Status", "DueDate");

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Deals_TenantId_ExpectedCloseDate"
  ON "Deals" ("TenantId", "ExpectedCloseDate");

-- Revisión post-creación
SELECT indexname, tablename FROM pg_indexes
WHERE schemaname = 'public' AND indexname LIKE 'IX_DomainEvents%' OR indexname LIKE 'IX_Leads_TenantId_Email%'
ORDER BY tablename, indexname;
