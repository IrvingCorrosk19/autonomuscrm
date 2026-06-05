-- 02_database_health_check.sql — Diagnóstico de salud PostgreSQL
\echo '=== Database health check ==='

-- Tablas sin PK
SELECT c.relname AS table_missing_pk
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'public' AND c.relkind = 'r'
  AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid = c.oid AND contype = 'p');

-- Índices duplicados (misma definición)
SELECT indrelid::regclass AS table_name, array_agg(indexrelid::regclass) AS duplicate_indexes
FROM pg_index i
JOIN pg_class c ON c.oid = i.indexrelid
WHERE indrelid::regclass::text LIKE 'public.%'
GROUP BY indrelid, indkey HAVING COUNT(*) > 1;

-- Tablas grandes
SELECT relname, n_live_tup, n_dead_tup, last_vacuum, last_analyze
FROM pg_stat_user_tables ORDER BY n_live_tup DESC NULLS LAST LIMIT 15;

-- Secuencias
SELECT sequencename, last_value FROM pg_sequences WHERE schemaname = 'public' LIMIT 20;

-- FK sin índice en columna referenciada (heurística)
SELECT conname, conrelid::regclass AS child_table, confrelid::regclass AS parent_table
FROM pg_constraint WHERE contype = 'f' LIMIT 30;
