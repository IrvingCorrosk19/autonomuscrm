using AutonomusCRM.Application.Common.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Seed;

internal static class GlobalManufacturingBulkSql
{
    internal static async Task BulkInsertCustomersAsync(
        ApplicationDbContext db,
        Guid tenantId,
        int count,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Customers" (
                "Id", "TenantId", "Name", "Email", "Phone", "Company", "Status",
                "Metadata", "CreatedAt", "LifetimeValue", "RiskScore", "LastContactAt")
            SELECT
                gen_random_uuid(),
                {tenantId},
                'GMG Customer ' || gs.i,
                'customer' || gs.i || '@gmg-demo.com',
                '+1555' || lpad(gs.i::text, 7, '0'),
                'GMG Division ' || ((gs.i % {GlobalManufacturingDemoTargets.Companies}) + 1),
                CASE
                    WHEN gs.i % 25 = 0 THEN 4
                    WHEN gs.i % 5 = 0 THEN 3
                    WHEN gs.i % 3 = 0 THEN 2
                    ELSE 1
                END,
                jsonb_build_object(
                    'Segment', CASE WHEN gs.i % 10 = 0 THEN 'Enterprise' WHEN gs.i % 3 = 0 THEN 'Mid-Market' ELSE 'SMB' END,
                    'Region', CASE gs.i % 4 WHEN 0 THEN 'NA' WHEN 1 THEN 'EMEA' WHEN 2 THEN 'APAC' ELSE 'LATAM' END,
                    'ProductLine', 'Industrial,Components,Services'),
                NOW() - ((gs.i % 365) || ' days')::interval,
                (2500 + (gs.i % 400) * 125)::numeric,
                15 + (gs.i % 70),
                NOW() - ((gs.i % 120) || ' days')::interval
            FROM generate_series(1, {count}) AS gs(i)
            """, cancellationToken);
    }

    internal static async Task BulkInsertLeadsAsync(
        ApplicationDbContext db,
        Guid tenantId,
        int count,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Leads" (
                "Id", "TenantId", "Name", "Email", "Phone", "Company", "Status", "Source",
                "Metadata", "CreatedAt", "Score")
            SELECT
                gen_random_uuid(),
                {tenantId},
                'GMG Lead ' || gs.i,
                'lead' || gs.i || '@gmg-prospect.com',
                '+1444' || lpad(gs.i::text, 7, '0'),
                'Prospect Co ' || ((gs.i % 120) + 1),
                CASE gs.i % 5
                    WHEN 0 THEN 3
                    WHEN 1 THEN 2
                    WHEN 2 THEN 1
                    ELSE 0
                END,
                CASE gs.i % 4
                    WHEN 0 THEN 0
                    WHEN 1 THEN 1
                    WHEN 2 THEN 2
                    ELSE 3
                END,
                jsonb_build_object('Campaign', 'Manufacturing 2026'),
                NOW() - ((gs.i % 60) || ' days')::interval,
                20 + (gs.i % 80)
            FROM generate_series(1, {count}) AS gs(i)
            """, cancellationToken);
    }

    internal static async Task BulkInsertDealsAsync(
        ApplicationDbContext db,
        Guid tenantId,
        int count,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Deals" (
                "Id", "TenantId", "CustomerId", "Title", "Description", "Amount", "ExpectedAmount",
                "Status", "Stage", "Probability", "CreatedAt", "ExpectedCloseDate", "Metadata")
            SELECT
                gen_random_uuid(),
                {tenantId},
                ranked."Id",
                'GMG Opportunity ' || ranked.rn,
                'Manufacturing equipment and services bundle',
                (15000 + (ranked.rn % 40) * 2500)::numeric,
                (12000 + (ranked.rn % 40) * 2000)::numeric,
                CASE WHEN ranked.rn % 6 = 0 THEN 2 ELSE 0 END,
                CASE ranked.rn % 5 WHEN 0 THEN 0 WHEN 1 THEN 1 WHEN 2 THEN 2 WHEN 3 THEN 3 ELSE 4 END,
                25 + (ranked.rn % 70),
                NOW() - ((ranked.rn % 90) || ' days')::interval,
                NOW() + ((ranked.rn % 120) || ' days')::interval,
                jsonb_build_object()
            FROM (
                SELECT c."Id", row_number() OVER (ORDER BY c."CreatedAt") AS rn
                FROM "Customers" c
                WHERE c."TenantId" = {tenantId}
                LIMIT {count}
            ) ranked
            """, cancellationToken);
    }

    internal static async Task BulkInsertTasksAsync(
        ApplicationDbContext db,
        Guid tenantId,
        Guid workflowId,
        int count,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "WorkflowTasks" (
                "Id", "TenantId", "WorkflowId", "Title", "Description", "Status", "Priority",
                "TaskType", "CreatedAt", "DueDate")
            SELECT
                gen_random_uuid(),
                {tenantId},
                {workflowId},
                CASE gs.i % 4
                    WHEN 0 THEN 'Follow up quote'
                    WHEN 1 THEN 'Schedule plant visit'
                    WHEN 2 THEN 'Renew service contract'
                    ELSE 'Resolve quality issue'
                END,
                'GMG operational task',
                CASE WHEN gs.i % 5 = 0 THEN 'Completed' ELSE 'Open' END,
                CASE WHEN gs.i % 7 = 0 THEN 'High' ELSE 'Normal' END,
                'Sales',
                NOW() - ((gs.i % 30) || ' days')::interval,
                NOW() + ((gs.i % 14) || ' days')::interval
            FROM generate_series(1, {count}) AS gs(i)
            """, cancellationToken);
    }

    internal static async Task BulkInsertProductEventsAsync(
        ApplicationDbContext db,
        Guid tenantId,
        int count,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProductUsageEvents" (
                "Id", "TenantId", "Module", "EventType", "DurationMinutes", "Industry",
                "RecordedAt", "CreatedAt")
            SELECT
                gen_random_uuid(),
                {tenantId},
                'Product-' || lpad(((gs.i % {GlobalManufacturingDemoTargets.ErpProducts}) + 1)::text, 3, '0'),
                CASE gs.i % 3 WHEN 0 THEN 'Login' WHEN 1 THEN 'FeatureUse' ELSE 'Export' END,
                5 + (gs.i % 45),
                'Manufacturing',
                NOW() - ((gs.i % 180) || ' days')::interval,
                NOW() - ((gs.i % 180) || ' days')::interval
            FROM generate_series(1, {count}) AS gs(i)
            """, cancellationToken);
    }
}
