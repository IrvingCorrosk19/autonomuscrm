-- 06_vacuum_analyze.sql — Mantenimiento post-migración/seed
\echo '=== VACUUM ANALYZE core tables ==='

VACUUM (ANALYZE) "Users";
VACUUM (ANALYZE) "Customers";
VACUUM (ANALYZE) "Leads";
VACUUM (ANALYZE) "Deals";
VACUUM (ANALYZE) "WorkflowTasks";
VACUUM (ANALYZE) "DomainEvents";
VACUUM (ANALYZE) "AiDecisionAudits";

SELECT relname, last_vacuum, last_analyze, n_live_tup
FROM pg_stat_user_tables
WHERE relname IN ('Users','Customers','Leads','Deals','WorkflowTasks','DomainEvents')
ORDER BY relname;
