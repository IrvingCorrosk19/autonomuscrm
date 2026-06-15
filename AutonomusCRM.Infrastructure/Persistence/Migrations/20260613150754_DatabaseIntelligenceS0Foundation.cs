using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseIntelligenceS0Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DbCatalogSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscoveryJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbCatalogSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbConnectionProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EngineType = table.Column<int>(type: "integer", nullable: false),
                    Host = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    DatabaseName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UsernameMasked = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EncryptedConnectionBlob = table.Column<byte[]>(type: "bytea", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastTestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastTestSucceeded = table.Column<bool>(type: "boolean", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbConnectionProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbDiscoveryJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbDiscoveryJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbIntelligenceForensicAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EngineType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    HostMasked = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DatabaseName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbIntelligenceForensicAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbCatalogSnapshots_TenantId_ConnectionProfileId_CreatedAtUtc",
                table: "DbCatalogSnapshots",
                columns: new[] { "TenantId", "ConnectionProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DbConnectionProfiles_TenantId_IsActive",
                table: "DbConnectionProfiles",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DbDiscoveryJobs_TenantId_ConnectionProfileId",
                table: "DbDiscoveryJobs",
                columns: new[] { "TenantId", "ConnectionProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_DbIntelligenceForensicAudits_TenantId_Action_CreatedAtUtc",
                table: "DbIntelligenceForensicAudits",
                columns: new[] { "TenantId", "Action", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DbIntelligenceForensicAudits_TenantId_CreatedAtUtc",
                table: "DbIntelligenceForensicAudits",
                columns: new[] { "TenantId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbCatalogSnapshots");

            migrationBuilder.DropTable(
                name: "DbConnectionProfiles");

            migrationBuilder.DropTable(
                name: "DbDiscoveryJobs");

            migrationBuilder.DropTable(
                name: "DbIntelligenceForensicAudits");
        }
    }
}
