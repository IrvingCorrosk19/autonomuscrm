-- Phase 2 scalability projections (run after seeding large datasets)
-- Simulates planner behavior for analytics hot paths at scale.

\echo '=== REP PERFORMANCE GROUP BY (Deals) ==='
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT "AssignedToUserId",
       SUM(CASE WHEN "Stage" = 5 AND "ClosedAt" >= NOW() - INTERVAL '30 days' THEN "Amount" ELSE 0 END),
       SUM(CASE WHEN "Status" = 0 THEN "Amount" * COALESCE("Probability", 0) / 100.0 ELSE 0 END),
       COUNT(*) FILTER (WHERE "Status" = 0)
FROM "Deals"
WHERE "TenantId" = '00000000-0000-0000-0000-000000000001'::uuid
  AND "AssignedToUserId" IS NOT NULL
GROUP BY "AssignedToUserId";

\echo '=== FORECAST HORIZON (open deals) ==='
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT SUM("Amount" * COALESCE("Probability", 0) / 100.0), SUM("Amount")
FROM "Deals"
WHERE "TenantId" = '00000000-0000-0000-0000-000000000001'::uuid
  AND "Status" = 0
  AND ("ExpectedCloseDate" IS NULL OR "ExpectedCloseDate" <= NOW() + INTERVAL '90 days');

\echo '=== LEAD SEARCH ILike (trigram candidate) ==='
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT COUNT(*)
FROM "Leads"
WHERE "TenantId" = '00000000-0000-0000-0000-000000000001'::uuid
  AND ("Name" ILIKE '%acme%' OR COALESCE("Email", '') ILIKE '%acme%' OR COALESCE("Company", '') ILIKE '%acme%');

\echo '=== DOMAIN EVENTS — partition readiness ==='
SELECT COUNT(*) AS total_events,
       date_trunc('month', "OccurredOn") AS month,
       COUNT(*) AS events_in_month
FROM "DomainEvents"
GROUP BY 1, 2
ORDER BY 2 DESC
LIMIT 12;
