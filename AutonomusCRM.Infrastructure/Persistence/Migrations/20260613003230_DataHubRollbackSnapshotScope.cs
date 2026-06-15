using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DataHubRollbackSnapshotScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchNumber",
                table: "DataHubRollbackSnapshots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RolledBack",
                table: "DataHubRollbackSnapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RowNumber",
                table: "DataHubRollbackSnapshots",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "DataHubRollbackSnapshots");

            migrationBuilder.DropColumn(
                name: "RolledBack",
                table: "DataHubRollbackSnapshots");

            migrationBuilder.DropColumn(
                name: "RowNumber",
                table: "DataHubRollbackSnapshots");
        }
    }
}
