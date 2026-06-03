using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase17_Foecm_BusinessOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessOutcomeDetail",
                table: "AiDecisionAudits",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BusinessRecordedAt",
                table: "AiDecisionAudits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BusinessSucceeded",
                table: "AiDecisionAudits",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessOutcomeDetail",
                table: "AiDecisionAudits");

            migrationBuilder.DropColumn(
                name: "BusinessRecordedAt",
                table: "AiDecisionAudits");

            migrationBuilder.DropColumn(
                name: "BusinessSucceeded",
                table: "AiDecisionAudits");
        }
    }
}
