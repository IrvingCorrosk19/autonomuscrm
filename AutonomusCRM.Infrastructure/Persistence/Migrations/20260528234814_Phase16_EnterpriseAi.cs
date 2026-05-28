using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase16_EnterpriseAi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessKnowledgeGraphEdges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessKnowledgeGraphEdges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MlDriftReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DriftScorePercent = table.Column<double>(type: "double precision", nullable: false),
                    AlertTriggered = table.Column<bool>(type: "boolean", nullable: false),
                    Details = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    MeasuredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MlDriftReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MlModelVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    VersionTag = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Weights = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    Metrics = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    TrainingSampleCount = table.Column<int>(type: "integer", nullable: false),
                    TrainedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MlModelVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MlPipelineRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DatasetType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SamplesProcessed = table.Column<int>(type: "integer", nullable: false),
                    ModelVersionTag = table.Column<string>(type: "text", nullable: true),
                    RunMetrics = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MlPipelineRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NbaOutcomeRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendedAction = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Converted = table.Column<bool>(type: "boolean", nullable: false),
                    ImpactScore = table.Column<decimal>(type: "numeric", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NbaOutcomeRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessKnowledgeGraphEdges_TenantId_SourceId_TargetId",
                table: "BusinessKnowledgeGraphEdges",
                columns: new[] { "TenantId", "SourceId", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_MlDriftReports_TenantId",
                table: "MlDriftReports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MlModelVersions_TenantId_ModelType_Status",
                table: "MlModelVersions",
                columns: new[] { "TenantId", "ModelType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MlPipelineRuns_TenantId_DatasetType",
                table: "MlPipelineRuns",
                columns: new[] { "TenantId", "DatasetType" });

            migrationBuilder.CreateIndex(
                name: "IX_NbaOutcomeRecords_TenantId",
                table: "NbaOutcomeRecords",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessKnowledgeGraphEdges");

            migrationBuilder.DropTable(
                name: "MlDriftReports");

            migrationBuilder.DropTable(
                name: "MlModelVersions");

            migrationBuilder.DropTable(
                name: "MlPipelineRuns");

            migrationBuilder.DropTable(
                name: "NbaOutcomeRecords");
        }
    }
}
