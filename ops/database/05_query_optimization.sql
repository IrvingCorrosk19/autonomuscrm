-- 05_query_optimization.sql — EXPLAIN ANALYZE plantillas para consultas críticas
-- Reemplazar :tenant_id con UUID real antes de ejecutar en producción

\echo '=== EXPLAIN templates (set tenant first) ==='
-- SET app.tenant_id no aplica; usar filtro explícito

EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT * FROM "Leads" WHERE "TenantId" = '00000000-0000-0000-0000-000000000000' ORDER BY "CreatedAt" DESC LIMIT 50;

EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT * FROM "Customers" WHERE "TenantId" = '00000000-0000-0000-0000-000000000000' AND "Status" = 0 LIMIT 50;

EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT * FROM "Deals" WHERE "TenantId" = '00000000-0000-0000-0000-000000000000' ORDER BY "CreatedAt" DESC LIMIT 50;

EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT "EventType", COUNT(*) FROM "DomainEvents"
WHERE "TenantId" = '00000000-0000-0000-0000-000000000000'
GROUP BY "EventType";

EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT COUNT(*) FROM "WorkflowTasks"
WHERE "TenantId" = '00000000-0000-0000-0000-000000000000' AND "Status" = 'Open';
