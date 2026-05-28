using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4_FailedEvents_GlobalFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FailedEventMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RoutingKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedEventMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FailedEventMessages_FailedAt",
                table: "FailedEventMessages",
                column: "FailedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FailedEventMessages_MessageId",
                table: "FailedEventMessages",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FailedEventMessages_TenantId",
                table: "FailedEventMessages",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailedEventMessages");
        }
    }
}
