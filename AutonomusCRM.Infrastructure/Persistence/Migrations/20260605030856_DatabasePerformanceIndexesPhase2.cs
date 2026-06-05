using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatabasePerformanceIndexesPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTasks_TenantId_AssignedToUserId_Status",
                table: "WorkflowTasks",
                columns: new[] { "TenantId", "AssignedToUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTasks_TenantId_Status_DueDate",
                table: "WorkflowTasks",
                columns: new[] { "TenantId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_TenantId_Email",
                table: "Leads",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_TenantId_EventType",
                table: "DomainEvents",
                columns: new[] { "TenantId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_TenantId_OccurredOn",
                table: "DomainEvents",
                columns: new[] { "TenantId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_TenantId_ExpectedCloseDate",
                table: "Deals",
                columns: new[] { "TenantId", "ExpectedCloseDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowTasks_TenantId_AssignedToUserId_Status",
                table: "WorkflowTasks");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTasks_TenantId_Status_DueDate",
                table: "WorkflowTasks");

            migrationBuilder.DropIndex(
                name: "IX_Leads_TenantId_Email",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_DomainEvents_TenantId_EventType",
                table: "DomainEvents");

            migrationBuilder.DropIndex(
                name: "IX_DomainEvents_TenantId_OccurredOn",
                table: "DomainEvents");

            migrationBuilder.DropIndex(
                name: "IX_Deals_TenantId_ExpectedCloseDate",
                table: "Deals");
        }
    }
}
