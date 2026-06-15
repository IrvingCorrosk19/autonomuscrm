using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseIntelligenceS2BusinessDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DbBusinessDiscoveryJobs",
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
                    TablesAnalyzed = table.Column<int>(type: "integer", nullable: false),
                    EntitiesDetected = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbBusinessDiscoveryJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbTableBusinessMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessDiscoveryJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    SchemaName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TableName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    InferredEntityType = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedEntityType = table.Column<int>(type: "integer", nullable: true),
                    ConfidencePercent = table.Column<int>(type: "integer", nullable: false),
                    ExplanationJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbTableBusinessMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbBusinessDiscoveryJobs_Status_CreatedAtUtc",
                table: "DbBusinessDiscoveryJobs",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DbBusinessDiscoveryJobs_TenantId_ConnectionProfileId_Create~",
                table: "DbBusinessDiscoveryJobs",
                columns: new[] { "TenantId", "ConnectionProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DbTableBusinessMappings_TenantId_ConnectionProfileId_Snapsh~",
                table: "DbTableBusinessMappings",
                columns: new[] { "TenantId", "ConnectionProfileId", "SnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_DbTableBusinessMappings_TenantId_SchemaName_TableName",
                table: "DbTableBusinessMappings",
                columns: new[] { "TenantId", "SchemaName", "TableName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbBusinessDiscoveryJobs");

            migrationBuilder.DropTable(
                name: "DbTableBusinessMappings");
        }
    }
}
