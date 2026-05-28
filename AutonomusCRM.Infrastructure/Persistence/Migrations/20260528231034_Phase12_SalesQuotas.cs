using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12_SalesQuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesQuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TargetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotas_TenantId",
                table: "SalesQuotas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotas_TenantId_UserId_PeriodType_PeriodStart",
                table: "SalesQuotas",
                columns: new[] { "TenantId", "UserId", "PeriodType", "PeriodStart" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesQuotas");
        }
    }
}
