using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DataHubRemediationScheduledLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveRunId",
                table: "DataHubScheduledImports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRunning",
                table: "DataHubScheduledImports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RunningLeaseUntil",
                table: "DataHubScheduledImports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataHubScheduledImports_IsRunning_RunningLeaseUntil",
                table: "DataHubScheduledImports",
                columns: new[] { "IsRunning", "RunningLeaseUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataHubScheduledImports_IsRunning_RunningLeaseUntil",
                table: "DataHubScheduledImports");

            migrationBuilder.DropColumn(
                name: "ActiveRunId",
                table: "DataHubScheduledImports");

            migrationBuilder.DropColumn(
                name: "IsRunning",
                table: "DataHubScheduledImports");

            migrationBuilder.DropColumn(
                name: "RunningLeaseUntil",
                table: "DataHubScheduledImports");
        }
    }
}
