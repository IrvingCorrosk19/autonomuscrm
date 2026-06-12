-- Local certification: EXPLAIN ANALYZE on hot paths
\timing on

\echo '=== DASHBOARD KPIs (Deals aggregates) ==='
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT COUNT(*) FILTER (WHERE "Stage" = 5) AS won,
       COUNT(*) FILTER (WHERE "Stage" = 6) AS lost,
       SUM("Amount") FILTER (WHERE "Stage" = 5) AS revenue_closed
FROM "Deals"
WHERE "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1);

\echo '=== LEADS list (tenant + order) ==='
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT "Id", "Name", "Status", "CreatedAt"
FROM "Leads"
WHERE "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1)
ORDER BY "CreatedAt" DESC
LIMIT 50;

\echo '=== DEALS pipeline ==='
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT "Id", "Title", "Amount", "Stage", "Status"
FROM "Deals"
WHERE "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1)
  AND "Status" = 0
ORDER BY "CreatedAt" DESC
LIMIT 50;

\echo '=== CUSTOMERS by status ==='
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT "Id", "Name", "Status", "LifetimeValue"
FROM "Customers"
WHERE "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1)
ORDER BY "CreatedAt" DESC
LIMIT 50;

\echo '=== SEARCH ILike (Leads) ==='
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT COUNT(*)
FROM "Leads"
WHERE "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1)
  AND ("Name" ILIKE '%test%' OR COALESCE("Email", '') ILIKE '%test%');

\echo '=== ANALYTICS rep performance ==='
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT "AssignedToUserId",
       SUM(CASE WHEN "Stage" = 5 THEN "Amount" ELSE 0 END),
       COUNT(*) FILTER (WHERE "Status" = 0)
FROM "Deals"
WHERE "TenantId" = (SELECT "Id" FROM "Tenants" LIMIT 1)
  AND "AssignedToUserId" IS NOT NULL
GROUP BY "AssignedToUserId";
