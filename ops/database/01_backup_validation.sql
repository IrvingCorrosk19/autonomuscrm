-- 01_backup_validation.sql — Verificar que existe backup restaurable (ejecutar en entorno destino)
-- Uso: psql -U postgres -d autonomuscrm -f 01_backup_validation.sql

\echo '=== Backup validation checklist ==='

SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_database WHERE datname = 'autonomuscrm')
  THEN 'OK: database autonomuscrm exists'
  ELSE 'FAIL: database missing' END AS db_check;

SELECT CASE WHEN (SELECT COUNT(*) FROM "__EFMigrationsHistory") > 0
  THEN 'OK: migrations history present (' || (SELECT COUNT(*)::text FROM "__EFMigrationsHistory") || ' rows)'
  ELSE 'FAIL: no migration history' END AS migration_check;

SELECT 'Latest migration: ' || "MigrationId" AS latest_migration
FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;

SELECT tablename, pg_size_pretty(pg_total_relation_size(quote_ident(tablename)::regclass)) AS size
FROM pg_tables WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(quote_ident(tablename)::regclass) DESC LIMIT 10;
