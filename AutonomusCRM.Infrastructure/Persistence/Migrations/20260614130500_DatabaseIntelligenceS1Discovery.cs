using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseIntelligenceS1Discovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DbDiscoveryJobs_TenantId_ConnectionProfileId",
                table: "DbDiscoveryJobs");

            migrationBuilder.AddColumn<Guid>(
                name: "CatalogSnapshotId",
                table: "DbDiscoveryJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ColumnsDiscovered",
                table: "DbDiscoveryJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "DbDiscoveryJobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "DbDiscoveryJobs",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogsJson",
                table: "DbDiscoveryJobs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercent",
                table: "DbDiscoveryJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RelationshipsDiscovered",
                table: "DbDiscoveryJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "DbDiscoveryJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TablesDiscovered",
                table: "DbDiscoveryJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewsDiscovered",
                table: "DbDiscoveryJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ColumnCount",
                table: "DbCatalogSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IndexCount",
                table: "DbCatalogSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RelationshipCount",
                table: "DbCatalogSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SchemaCount",
                table: "DbCatalogSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TableCount",
                table: "DbCatalogSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "DbCatalogSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DbCatalogColumns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ObjectName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ColumnName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DataType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsNullable = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsPrimaryKey = table.Column<bool>(type: "boolean", nullable: false),
                    IsForeignKey = table.Column<bool>(type: "boolean", nullable: false),
                    IsIndexed = table.Column<bool>(type: "boolean", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbCatalogColumns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbCatalogConstraints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ObjectName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ConstraintName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ConstraintType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ColumnNames = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbCatalogConstraints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbCatalogIndexes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ObjectName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IndexName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsUnique = table.Column<bool>(type: "boolean", nullable: false),
                    ColumnNames = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbCatalogIndexes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbCatalogRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromSchema = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FromTable = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FromColumn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ToSchema = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ToTable = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ToColumn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfidencePercent = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbCatalogRelationships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbCatalogSchemas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbCatalogSchemas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbCatalogTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ObjectName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ObjectType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EstimatedRowCount = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbCatalogTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbCatalogViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchemaName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ObjectName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbCatalogViews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbDiscoveryJobs_Status_CreatedAtUtc",
                table: "DbDiscoveryJobs",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DbDiscoveryJobs_TenantId_ConnectionProfileId_CreatedAtUtc",
                table: "DbDiscoveryJobs",
                columns: new[] { "TenantId", "ConnectionProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DbCatalogColumns_SnapshotId_SchemaName_ObjectName_Ordinal",
                table: "DbCatalogColumns",
                columns: new[] { "SnapshotId", "SchemaName", "ObjectName", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_DbCatalogConstraints_SnapshotId",
                table: "DbCatalogConstraints",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DbCatalogIndexes_SnapshotId",
                table: "DbCatalogIndexes",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DbCatalogRelationships_SnapshotId",
                table: "DbCatalogRelationships",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DbCatalogSchemas_SnapshotId",
                table: "DbCatalogSchemas",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DbCatalogTables_SnapshotId",
                table: "DbCatalogTables",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DbCatalogViews_SnapshotId",
                table: "DbCatalogViews",
                column: "SnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbCatalogColumns");

            migrationBuilder.DropTable(
                name: "DbCatalogConstraints");

            migrationBuilder.DropTable(
                name: "DbCatalogIndexes");

            migrationBuilder.DropTable(
                name: "DbCatalogRelationships");

            migrationBuilder.DropTable(
                name: "DbCatalogSchemas");

            migrationBuilder.DropTable(
                name: "DbCatalogTables");

            migrationBuilder.DropTable(
                name: "DbCatalogViews");

            migrationBuilder.DropIndex(
                name: "IX_DbDiscoveryJobs_Status_CreatedAtUtc",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropIndex(
                name: "IX_DbDiscoveryJobs_TenantId_ConnectionProfileId_CreatedAtUtc",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropColumn(
                name: "CatalogSnapshotId",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropColumn(
                name: "ColumnsDiscovered",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropColumn(
                name: "LogsJson",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropColumn(
                name: "RelationshipsDiscovered",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropColumn(
                name: "TablesDiscovered",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropColumn(
                name: "ViewsDiscovered",
                table: "DbDiscoveryJobs");

            migrationBuilder.DropColumn(
                name: "ColumnCount",
                table: "DbCatalogSnapshots");

            migrationBuilder.DropColumn(
                name: "IndexCount",
                table: "DbCatalogSnapshots");

            migrationBuilder.DropColumn(
                name: "RelationshipCount",
                table: "DbCatalogSnapshots");

            migrationBuilder.DropColumn(
                name: "SchemaCount",
                table: "DbCatalogSnapshots");

            migrationBuilder.DropColumn(
                name: "TableCount",
                table: "DbCatalogSnapshots");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "DbCatalogSnapshots");

            migrationBuilder.CreateIndex(
                name: "IX_DbDiscoveryJobs_TenantId_ConnectionProfileId",
                table: "DbDiscoveryJobs",
                columns: new[] { "TenantId", "ConnectionProfileId" });
        }
    }
}
