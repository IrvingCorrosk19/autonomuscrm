-- 04_constraints_integrity.sql — Revisión de integridad (solo lectura + conteos huérfanos)
\echo '=== Integrity checks (read-only) ==='

SELECT 'Users' AS entity, COUNT(*) AS total FROM "Users";
SELECT 'Customers' AS entity, COUNT(*) AS total FROM "Customers";
SELECT 'Leads' AS entity, COUNT(*) AS total FROM "Leads";
SELECT 'Deals' AS entity, COUNT(*) AS total FROM "Deals";
SELECT 'WorkflowTasks' AS entity, COUNT(*) AS total FROM "WorkflowTasks";
SELECT 'DomainEvents' AS entity, COUNT(*) AS total FROM "DomainEvents";

-- Deals sin customer (si CustomerId es NOT NULL debería ser 0)
SELECT COUNT(*) AS deals_orphan_customer
FROM "Deals" d
WHERE NOT EXISTS (SELECT 1 FROM "Customers" c WHERE c."Id" = d."CustomerId");

-- Leads con email duplicado por tenant
SELECT "TenantId", "Email", COUNT(*) AS cnt
FROM "Leads" WHERE "Email" IS NOT NULL AND "Email" <> ''
GROUP BY "TenantId", "Email" HAVING COUNT(*) > 1 LIMIT 10;
