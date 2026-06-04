\set ON_ERROR_STOP on
\echo '===DB_GENERAL==='
SELECT current_database() AS db, pg_size_pretty(pg_database_size(current_database())) AS db_size;
SELECT version();
\echo '===COUNTS==='
SELECT (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public' AND c.relkind='r') AS tables,
       (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public' AND c.relkind='i') AS indexes,
       (SELECT count(*) FROM pg_views WHERE schemaname='public') AS views,
       (SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='public' AND p.prokind='f') AS functions,
       (SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='public' AND p.prokind='p') AS procedures,
       (SELECT count(*) FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public' AND NOT t.tgisinternal) AS triggers;
\echo '===TOP50_TABLES==='
SELECT c.relname AS table_name,
       COALESCE(s.n_live_tup,0) AS row_estimate,
       pg_size_pretty(pg_total_relation_size(c.oid)) AS total_size,
       pg_total_relation_size(c.oid) AS size_bytes,
       round(100.0 * pg_total_relation_size(c.oid) / NULLIF((SELECT sum(pg_total_relation_size(st.relid)) FROM pg_catalog.pg_statio_user_tables st),0), 2) AS pct_total
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
LEFT JOIN pg_stat_user_tables s ON s.relid = c.oid
WHERE n.nspname = 'public' AND c.relkind = 'r'
ORDER BY pg_total_relation_size(c.oid) DESC
LIMIT 50;
\echo '===TABLE_DETAILS==='
SELECT t.relname AS table_name,
       COALESCE(st.n_live_tup,0) AS rows_est,
       pg_size_pretty(pg_total_relation_size(t.oid)) AS total_size,
       pg_size_pretty(pg_relation_size(t.oid)) AS heap_size,
       pg_size_pretty(pg_total_relation_size(t.oid)-pg_relation_size(t.oid)) AS index_size
FROM pg_class t
JOIN pg_namespace n ON n.oid=t.relnamespace
LEFT JOIN pg_stat_user_tables st ON st.relid=t.oid
WHERE n.nspname='public' AND t.relkind='r'
ORDER BY pg_total_relation_size(t.oid) DESC;
\echo '===NO_PK==='
SELECT c.relname AS table_name
FROM pg_class c
JOIN pg_namespace n ON n.oid=c.relnamespace
WHERE n.nspname='public' AND c.relkind='r'
  AND NOT EXISTS (
    SELECT 1 FROM pg_constraint con
    WHERE con.conrelid=c.oid AND con.contype='p')
ORDER BY 1;
\echo '===FK_WITHOUT_INDEX==='
SELECT tc.table_name, kcu.column_name, ccu.table_name AS ref_table
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu ON tc.constraint_name=kcu.constraint_name AND tc.table_schema=kcu.table_schema
JOIN information_schema.constraint_column_usage ccu ON ccu.constraint_name=tc.constraint_name AND ccu.table_schema=tc.table_schema
WHERE tc.constraint_type='FOREIGN KEY' AND tc.table_schema='public'
  AND NOT EXISTS (
    SELECT 1 FROM pg_indexes i
    WHERE i.schemaname='public' AND i.tablename=tc.table_name
      AND i.indexdef ILIKE '%(' || kcu.column_name || '%')
  )
ORDER BY 1,2;
\echo '===SEQ_SCAN_HIGH==='
SELECT relname, seq_scan, idx_scan,
       CASE WHEN seq_scan+idx_scan>0 THEN round(100.0*seq_scan/(seq_scan+idx_scan),2) ELSE 0 END AS seq_scan_pct,
       n_live_tup
FROM pg_stat_user_tables
WHERE schemaname='public' AND (seq_scan > 100 OR (seq_scan > idx_scan AND n_live_tup > 1000))
ORDER BY seq_scan DESC
LIMIT 30;
\echo '===UNUSED_INDEXES==='
SELECT schemaname, relname AS table_name, indexrelname AS index_name, idx_scan, pg_size_pretty(pg_relation_size(indexrelid)) AS index_size
FROM pg_stat_user_indexes
WHERE schemaname='public' AND idx_scan = 0 AND indexrelname NOT LIKE '%pkey%'
ORDER BY pg_relation_size(indexrelid) DESC
LIMIT 40;
\echo '===INDEX_LIST==='
SELECT tablename, indexname, indexdef
FROM pg_indexes
WHERE schemaname='public'
ORDER BY tablename, indexname;
\echo '===PG_STAT_STATEMENTS==='
SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname='pg_stat_statements') AS ext_installed;
\echo '===JSON_TEXT_COLUMNS==='
SELECT table_name, column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_schema='public' AND data_type IN ('json','jsonb','text')
ORDER BY table_name, column_name;
\echo '===TENANT_COLUMNS==='
SELECT table_name, column_name
FROM information_schema.columns
WHERE table_schema='public' AND column_name ILIKE '%tenant%'
ORDER BY table_name;
