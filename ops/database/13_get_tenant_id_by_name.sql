-- Devuelve Id del tenant por nombre
-- Uso: psql -v tenant_name='TechSolutions Panama' -f 13_get_tenant_id_by_name.sql

SELECT "Id"::text
FROM "Tenants"
WHERE "Name" = :'tenant_name'
ORDER BY "CreatedAt" DESC
LIMIT 1;
