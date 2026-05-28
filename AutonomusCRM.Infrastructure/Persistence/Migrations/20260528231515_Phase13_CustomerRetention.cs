using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase13_CustomerRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerCommunicationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Recipient = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TrackingId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Variables = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCommunicationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDealId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RenewalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnnualValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TermMonths = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContracts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCommunicationLogs_TenantId",
                table: "CustomerCommunicationLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCommunicationLogs_TenantId_CustomerId",
                table: "CustomerCommunicationLogs",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContracts_TenantId",
                table: "CustomerContracts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContracts_TenantId_CustomerId",
                table: "CustomerContracts",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContracts_TenantId_RenewalDate",
                table: "CustomerContracts",
                columns: new[] { "TenantId", "RenewalDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerCommunicationLogs");

            migrationBuilder.DropTable(
                name: "CustomerContracts");
        }
    }
}
