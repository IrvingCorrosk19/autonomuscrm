using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseIntelligenceS6AiInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DbIntelligenceInsightJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Stage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    InsightCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbIntelligenceInsightJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbIntelligenceInsights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    EvidenceJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExplainabilityJson = table.Column<string>(type: "jsonb", nullable: false),
                    SuggestedAction = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ImpactScore = table.Column<int>(type: "integer", nullable: false),
                    EffortScore = table.Column<int>(type: "integer", nullable: false),
                    ConfidencePercent = table.Column<int>(type: "integer", nullable: false),
                    SemanticMatchScore = table.Column<int>(type: "integer", nullable: false),
                    PriorityScore = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: true),
                    SchemaName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TableName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbIntelligenceInsights", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbIntelligenceInsightJobs_TenantId_ConnectionProfileId_Crea~",
                table: "DbIntelligenceInsightJobs",
                columns: new[] { "TenantId", "ConnectionProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DbIntelligenceInsights_TenantId_ConnectionProfileId_Priorit~",
                table: "DbIntelligenceInsights",
                columns: new[] { "TenantId", "ConnectionProfileId", "PriorityScore" });

            migrationBuilder.CreateIndex(
                name: "IX_DbIntelligenceInsights_TenantId_JobId",
                table: "DbIntelligenceInsights",
                columns: new[] { "TenantId", "JobId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbIntelligenceInsightJobs");

            migrationBuilder.DropTable(
                name: "DbIntelligenceInsights");
        }
    }
}
