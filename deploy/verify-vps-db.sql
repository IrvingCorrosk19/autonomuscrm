SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 3;
SELECT count(*) AS users FROM "Users";
SELECT count(*) AS indexes FROM pg_indexes WHERE schemaname = 'public';
