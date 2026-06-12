using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2AdvancedDatabaseOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_TenantId_AssignedToUserId",
                table: "Deals",
                columns: new[] { "TenantId", "AssignedToUserId" });

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Leads_Name_trgm"
                    ON "Leads" USING gin ("Name" gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS "IX_Leads_Email_trgm"
                    ON "Leads" USING gin (COALESCE("Email", '') gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS "IX_Leads_Company_trgm"
                    ON "Leads" USING gin (COALESCE("Company", '') gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS "IX_Customers_Name_trgm"
                    ON "Customers" USING gin ("Name" gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS "IX_Customers_Email_trgm"
                    ON "Customers" USING gin (COALESCE("Email", '') gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS "IX_Customers_Company_trgm"
                    ON "Customers" USING gin (COALESCE("Company", '') gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS "IX_Deals_Title_trgm"
                    ON "Deals" USING gin ("Title" gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS "IX_Users_Email_trgm"
                    ON "Users" USING gin ("Email" gin_trgm_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_Users_Email_trgm";
                DROP INDEX IF EXISTS "IX_Deals_Title_trgm";
                DROP INDEX IF EXISTS "IX_Customers_Company_trgm";
                DROP INDEX IF EXISTS "IX_Customers_Email_trgm";
                DROP INDEX IF EXISTS "IX_Customers_Name_trgm";
                DROP INDEX IF EXISTS "IX_Leads_Company_trgm";
                DROP INDEX IF EXISTS "IX_Leads_Email_trgm";
                DROP INDEX IF EXISTS "IX_Leads_Name_trgm";
                """);

            migrationBuilder.DropIndex(
                name: "IX_Deals_TenantId_AssignedToUserId",
                table: "Deals");
        }
    }
}
