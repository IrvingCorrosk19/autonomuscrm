using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseIntelligenceS3DataHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataHealthFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    HealthJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: true),
                    Severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Explanation = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    BusinessImpact = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Evidence = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Recommendation = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SchemaName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TableName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AffectedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHealthFindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataHealthJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ScanMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Stage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    GlobalScore = table.Column<int>(type: "integer", nullable: false),
                    FindingsCount = table.Column<int>(type: "integer", nullable: false),
                    CriticalFindings = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHealthJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataHealthScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    HealthJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    CompletenessScore = table.Column<int>(type: "integer", nullable: false),
                    ValidityScore = table.Column<int>(type: "integer", nullable: false),
                    ConsistencyScore = table.Column<int>(type: "integer", nullable: false),
                    DuplicateScore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHealthScores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataHealthFindings_TenantId_ConnectionProfileId_CreatedAtUtc",
                table: "DataHealthFindings",
                columns: new[] { "TenantId", "ConnectionProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHealthJobs_TenantId_ConnectionProfileId_CreatedAtUtc",
                table: "DataHealthJobs",
                columns: new[] { "TenantId", "ConnectionProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHealthScores_HealthJobId",
                table: "DataHealthScores",
                column: "HealthJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataHealthFindings");

            migrationBuilder.DropTable(
                name: "DataHealthJobs");

            migrationBuilder.DropTable(
                name: "DataHealthScores");
        }
    }
}
