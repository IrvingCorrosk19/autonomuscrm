-- =============================================================================
-- AUTONOMUSFLOW — OPTIMIZATION SCRIPT (RECOMMENDED ONLY — DO NOT AUTO-RUN)
-- =============================================================================
-- Database: autonomuscrm (PostgreSQL 14+ / tested on 18)
-- Generated: 2026-06-04
-- Author: DBA audit (read-only assessment phase)
--
-- RULES:
--   - Review each section in staging before production
--   - Run during low-traffic window
--   - Use CONCURRENTLY on production for CREATE INDEX (cannot run inside transaction)
--   - Backup before REINDEX / DROP
-- =============================================================================

-- -----------------------------------------------------------------------------
-- SECTION 0: PRE-FLIGHT CHECKS (read-only)
-- -----------------------------------------------------------------------------
SELECT version();
SELECT current_database(), pg_size_pretty(pg_database_size(current_database()));
SELECT count(*) AS tables FROM pg_tables WHERE schemaname = 'public';
SELECT count(*) AS indexes FROM pg_indexes WHERE schemaname = 'public';

-- Verify no orphan approvals before FK (optional future)
-- SELECT COUNT(*) FROM "AiApprovalRequests" ap
-- LEFT JOIN "AiDecisionAudits" au ON au."Id" = ap."AuditId"
-- WHERE au."Id" IS NULL;

-- -----------------------------------------------------------------------------
-- SECTION 1: EXTENSIONS & OBSERVABILITY (HIGH IMPACT — requires restart for preload)
-- -----------------------------------------------------------------------------
-- Step 1a (postgresql.conf): shared_preload_libraries = 'pg_stat_statements'
-- Step 1b (after restart):
-- CREATE EXTENSION IF NOT EXISTS pg_stat_statements;
-- SELECT query, calls, mean_exec_time, total_exec_time
-- FROM pg_stat_statements
-- ORDER BY total_exec_time DESC
-- LIMIT 20;

-- Optional: pgvector for semantic memory (HIGH IMPACT — separate migration project)
-- CREATE EXTENSION IF NOT EXISTS vector;
-- ALTER TABLE "MemoryEmbeddings" ADD COLUMN IF NOT EXISTS embedding vector(1536);
-- CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_memoryembeddings_vector_hnsw
--   ON "MemoryEmbeddings" USING hnsw (embedding vector_cosine_ops);

-- -----------------------------------------------------------------------------
-- SECTION 2: RECOMMENDED INDEXES — CRM / ABOS HOT PATHS
-- -----------------------------------------------------------------------------

-- H2: AiDecisionAudits — Customer360 / Trust timeline ORDER BY CreatedAt
-- Impact: Customer 360, Trust Studio, Executive audits
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_AiDecisionAudits_TenantId_CustomerId_CreatedAt"
    ON "AiDecisionAudits" ("TenantId", "CustomerId", "CreatedAt" DESC);

-- H3: AiApprovalRequests — JOIN on AuditId (Customer360EnterpriseService)
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_AiApprovalRequests_AuditId"
    ON "AiApprovalRequests" ("AuditId");

-- M2: WorkflowTasks — Customer Success OS tickets/cases
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_WorkflowTasks_TenantId_RelatedEntityId_Status"
    ON "WorkflowTasks" ("TenantId", "RelatedEntityId", "Status")
    WHERE "RelatedEntityId" IS NOT NULL;

-- DomainEvents — correlation / aggregate lookups (if not using idx enough)
-- Already have IX_DomainEvents_CorrelationId; add composite for tenant timeline:
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_DomainEvents_TenantId_OccurredOn"
    ON "DomainEvents" ("TenantId", "OccurredOn" DESC);

-- CustomerCommunicationLogs — comms timeline per customer
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CustomerCommunicationLogs_TenantId_CustomerId_SentAt"
    ON "CustomerCommunicationLogs" ("TenantId", "CustomerId", "SentAt" DESC NULLS LAST);

-- VoiceCallLogs — voice timeline
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_VoiceCallLogs_TenantId_CustomerId_StartedAt"
    ON "VoiceCallLogs" ("TenantId", "CustomerId", "StartedAt" DESC);

-- FailedEventMessages — ops UI / recovery
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_FailedEventMessages_TenantId_FailedAt"
    ON "FailedEventMessages" ("TenantId", "FailedAt" DESC);

-- Customers — directory search by status (if not covered by email composite)
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Customers_TenantId_Status"
    ON "Customers" ("TenantId", "Status");

-- Deals — pipeline boards ORDER BY amount/date
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Deals_TenantId_Status_Stage"
    ON "Deals" ("TenantId", "Status", "Stage");

-- BusinessKnowledgeGraphEdges — graph neighbor lookup (OR query mitigation)
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_BKGraphEdges_TenantId_TargetType_TargetId"
    ON "BusinessKnowledgeGraphEdges" ("TenantId", "TargetType", "TargetId");

-- -----------------------------------------------------------------------------
-- SECTION 3: ANALYZE — refresh planner statistics (LOW RISK)
-- -----------------------------------------------------------------------------
ANALYZE "Customers";
ANALYZE "Deals";
ANALYZE "Leads";
ANALYZE "Users";
ANALYZE "AiDecisionAudits";
ANALYZE "AiApprovalRequests";
ANALYZE "DomainEvents";
ANALYZE "MemoryEmbeddings";
ANALYZE "BusinessMemories";
ANALYZE "WorkflowTasks";
ANALYZE "CustomerCommunicationLogs";

-- Or full database:
-- ANALYZE;

-- -----------------------------------------------------------------------------
-- SECTION 4: REINDEX — only if bloat detected (run manually after pg_stat_user_tables check)
-- -----------------------------------------------------------------------------
-- REINDEX INDEX CONCURRENTLY "IX_DomainEvents_OccurredOn";
-- REINDEX TABLE CONCURRENTLY "DomainEvents";

-- -----------------------------------------------------------------------------
-- SECTION 5: INDEX CLEANUP CANDIDATES (DO NOT RUN without 14d pg_stat_user_indexes in PROD)
-- -----------------------------------------------------------------------------
-- Example: drop duplicate if IX_Workflows_TenantId is subset of IX_Workflows_TenantId_IsActive
-- DROP INDEX CONCURRENTLY IF EXISTS "IX_Workflows_TenantId";

-- -----------------------------------------------------------------------------
-- SECTION 6: FOREIGN KEYS (OPTIONAL — validate orphans first) (MEDIUM RISK)
-- -----------------------------------------------------------------------------
-- ALTER TABLE "Deals"
--   ADD CONSTRAINT "FK_Deals_Customers_CustomerId"
--   FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE RESTRICT;
--
-- ALTER TABLE "Users"
--   ADD CONSTRAINT "FK_Users_Tenants_TenantId"
--   FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE;

-- -----------------------------------------------------------------------------
-- SECTION 7: RETENTION / PARTITIONING (HIGH IMPACT — separate maintenance window)
-- -----------------------------------------------------------------------------
-- Example template for DomainEvents monthly partition (PostgreSQL 18):
-- -- CREATE TABLE "DomainEvents_2026_06" PARTITION OF "DomainEvents"
-- --   FOR VALUES FROM ('2026-06-01') TO ('2026-07-01');

-- Archive job (application-level or SQL):
-- DELETE FROM "DomainEvents" WHERE "OccurredOn" < NOW() - INTERVAL '365 days';

-- -----------------------------------------------------------------------------
-- SECTION 8: POST-VALIDATION QUERIES
-- -----------------------------------------------------------------------------
-- EXPLAIN (ANALYZE, BUFFERS)
-- SELECT * FROM "AiDecisionAudits"
-- WHERE "TenantId" = '...' AND "CustomerId" = '...'
-- ORDER BY "CreatedAt" DESC LIMIT 15;

-- SELECT indexrelname, idx_scan, idx_tup_read
-- FROM pg_stat_user_indexes
-- WHERE schemaname = 'public' AND relname = 'AiDecisionAudits';

-- =============================================================================
-- END OF SCRIPT
-- =============================================================================
