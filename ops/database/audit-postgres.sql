-- AutonomusCRM PostgreSQL audit snapshot
\echo '=== TABLE SIZES ==='
SELECT relname AS table_name,
       pg_size_pretty(pg_total_relation_size(relid)) AS total_size,
       n_live_tup AS live_rows,
       n_dead_tup AS dead_rows,
       last_vacuum,
       last_autovacuum,
       last_analyze,
       last_autoanalyze
FROM pg_stat_user_tables
ORDER BY pg_total_relation_size(relid) DESC;

\echo '=== INDEXES ==='
SELECT tablename, indexname, indexdef
FROM pg_indexes
WHERE schemaname = 'public'
ORDER BY tablename, indexname;

\echo '=== UNUSED / LOW SCAN INDEXES ==='
SELECT s.schemaname, s.relname AS table_name, s.indexrelname AS index_name,
       s.idx_scan, pg_size_pretty(pg_relation_size(s.indexrelid)) AS index_size
FROM pg_stat_user_indexes s
JOIN pg_index i ON i.indexrelid = s.indexrelid
WHERE s.schemaname = 'public'
ORDER BY s.idx_scan ASC, pg_relation_size(s.indexrelid) DESC;

\echo '=== FK WITHOUT INDEX (child columns) ==='
SELECT c.conrelid::regclass AS table_name, a.attname AS column_name
FROM pg_constraint c
JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = ANY (c.conkey)
WHERE c.contype = 'f'
  AND NOT EXISTS (
    SELECT 1 FROM pg_index i
    WHERE i.indrelid = c.conrelid AND a.attnum = ANY (i.indkey)
  );

\echo '=== SEQUENTIAL SCANS (high) ==='
SELECT relname, seq_scan, seq_tup_read, idx_scan,
       CASE WHEN seq_scan + idx_scan > 0 THEN round(100.0 * seq_scan / (seq_scan + idx_scan), 1) ELSE 0 END AS seq_pct
FROM pg_stat_user_tables
WHERE seq_scan > 0
ORDER BY seq_tup_read DESC
LIMIT 25;
