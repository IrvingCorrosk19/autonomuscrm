using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhaseB_SemanticMemoryEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerMemoryProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    HistorySummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RiskSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PreferencesSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SuccessfulDecisionsSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FailedDecisionsSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EffectiveChannelsSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LastRefreshedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerMemoryProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemoryEmbeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    EmbeddingVector = table.Column<float[]>(type: "jsonb", nullable: false),
                    EmbeddingModel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RelevanceScore = table.Column<double>(type: "double precision", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryEmbeddings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMemoryProfiles_TenantId_CustomerId",
                table: "CustomerMemoryProfiles",
                columns: new[] { "TenantId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemoryEmbeddings_TenantId_RelevanceScore",
                table: "MemoryEmbeddings",
                columns: new[] { "TenantId", "RelevanceScore" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryEmbeddings_TenantId_SourceType_SourceId",
                table: "MemoryEmbeddings",
                columns: new[] { "TenantId", "SourceType", "SourceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerMemoryProfiles");

            migrationBuilder.DropTable(
                name: "MemoryEmbeddings");
        }
    }
}
