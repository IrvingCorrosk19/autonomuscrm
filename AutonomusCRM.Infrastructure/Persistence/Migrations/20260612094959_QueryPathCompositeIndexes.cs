using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QueryPathCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTasks_TenantId_Status_CreatedAt",
                table: "WorkflowTasks",
                columns: new[] { "TenantId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_TenantId_AssignedToUserId",
                table: "Leads",
                columns: new[] { "TenantId", "AssignedToUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_TenantId_CreatedAt",
                table: "Leads",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_TenantId_CreatedAt",
                table: "Deals",
                columns: new[] { "TenantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowTasks_TenantId_Status_CreatedAt",
                table: "WorkflowTasks");

            migrationBuilder.DropIndex(
                name: "IX_Leads_TenantId_AssignedToUserId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_TenantId_CreatedAt",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Deals_TenantId_CreatedAt",
                table: "Deals");
        }
    }
}
