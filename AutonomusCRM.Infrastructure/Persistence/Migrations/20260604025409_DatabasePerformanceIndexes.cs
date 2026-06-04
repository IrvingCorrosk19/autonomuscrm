using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatabasePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTasks_TenantId_RelatedEntityId_Status",
                table: "WorkflowTasks",
                columns: new[] { "TenantId", "RelatedEntityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceCallLogs_TenantId_CustomerId_StartedAt",
                table: "VoiceCallLogs",
                columns: new[] { "TenantId", "CustomerId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FailedEventMessages_TenantId_FailedAt",
                table: "FailedEventMessages",
                columns: new[] { "TenantId", "FailedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_TenantId_Status_Stage",
                table: "Deals",
                columns: new[] { "TenantId", "Status", "Stage" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Status",
                table: "Customers",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCommunicationLogs_TenantId_CustomerId_SentAt",
                table: "CustomerCommunicationLogs",
                columns: new[] { "TenantId", "CustomerId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessKnowledgeGraphEdges_TenantId_TargetType_TargetId",
                table: "BusinessKnowledgeGraphEdges",
                columns: new[] { "TenantId", "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_AiDecisionAudits_TenantId_CustomerId_CreatedAt",
                table: "AiDecisionAudits",
                columns: new[] { "TenantId", "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiApprovalRequests_AuditId",
                table: "AiApprovalRequests",
                column: "AuditId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowTasks_TenantId_RelatedEntityId_Status",
                table: "WorkflowTasks");

            migrationBuilder.DropIndex(
                name: "IX_VoiceCallLogs_TenantId_CustomerId_StartedAt",
                table: "VoiceCallLogs");

            migrationBuilder.DropIndex(
                name: "IX_FailedEventMessages_TenantId_FailedAt",
                table: "FailedEventMessages");

            migrationBuilder.DropIndex(
                name: "IX_Deals_TenantId_Status_Stage",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_Status",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_CustomerCommunicationLogs_TenantId_CustomerId_SentAt",
                table: "CustomerCommunicationLogs");

            migrationBuilder.DropIndex(
                name: "IX_BusinessKnowledgeGraphEdges_TenantId_TargetType_TargetId",
                table: "BusinessKnowledgeGraphEdges");

            migrationBuilder.DropIndex(
                name: "IX_AiDecisionAudits_TenantId_CustomerId_CreatedAt",
                table: "AiDecisionAudits");

            migrationBuilder.DropIndex(
                name: "IX_AiApprovalRequests_AuditId",
                table: "AiApprovalRequests");
        }
    }
}
