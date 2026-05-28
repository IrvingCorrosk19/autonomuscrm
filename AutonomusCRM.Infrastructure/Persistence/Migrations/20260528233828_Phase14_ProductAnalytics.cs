using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase14_ProductAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerAnalyticsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HealthScore = table.Column<int>(type: "integer", nullable: false),
                    ChurnRiskScore = table.Column<int>(type: "integer", nullable: false),
                    NpsScore = table.Column<int>(type: "integer", nullable: true),
                    CsatScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    RevenueAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpansionScore = table.Column<int>(type: "integer", nullable: false),
                    Segment = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EngagementScore = table.Column<int>(type: "integer", nullable: false),
                    AdoptionScore = table.Column<int>(type: "integer", nullable: false),
                    ActiveUsers = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAnalyticsSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedbackType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Segment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductUsageEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUsageEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAnalyticsSnapshots_TenantId",
                table: "CustomerAnalyticsSnapshots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAnalyticsSnapshots_TenantId_CustomerId_SnapshotDate",
                table: "CustomerAnalyticsSnapshots",
                columns: new[] { "TenantId", "CustomerId", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedbacks_TenantId",
                table: "CustomerFeedbacks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedbacks_TenantId_CustomerId",
                table: "CustomerFeedbacks",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedbacks_TenantId_FeedbackType",
                table: "CustomerFeedbacks",
                columns: new[] { "TenantId", "FeedbackType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUsageEvents_TenantId",
                table: "ProductUsageEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUsageEvents_TenantId_Module",
                table: "ProductUsageEvents",
                columns: new[] { "TenantId", "Module" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUsageEvents_TenantId_RecordedAt",
                table: "ProductUsageEvents",
                columns: new[] { "TenantId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerAnalyticsSnapshots");

            migrationBuilder.DropTable(
                name: "CustomerFeedbacks");

            migrationBuilder.DropTable(
                name: "ProductUsageEvents");
        }
    }
}
