using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhaseA_BusinessMemoryEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EpisodeKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    MemoryType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Importance = table.Column<int>(type: "integer", nullable: false),
                    SourceChannel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tags = table.Column<List<string>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMemories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMemoryContexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContextLayer = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Snapshot = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMemoryContexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMemoryDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AiDecisionAuditId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    WasSuccessful = table.Column<bool>(type: "boolean", nullable: true),
                    Context = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMemoryDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMemoryEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Payload = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorType = table.Column<string>(type: "text", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Narrative = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMemoryEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMemoryFacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FactValue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMemoryFacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMemoryInsights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    MemoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    InsightType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMemoryInsights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMemoryLearnings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StrategyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActionTaken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContextPattern = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    SuccessRate = table.Column<decimal>(type: "numeric", nullable: false),
                    LastAppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastOutcome = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMemoryLearnings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMemoryObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMemoryObservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMemoryOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutcomeCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    RevenueDelta = table.Column<decimal>(type: "numeric", nullable: false),
                    CustomerImpactScore = table.Column<int>(type: "integer", nullable: false),
                    TrustImpactScore = table.Column<int>(type: "integer", nullable: false),
                    Narrative = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMemoryOutcomes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMemoryRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ToId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMemoryRelationships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemories_TenantId_EpisodeKey",
                table: "BusinessMemories",
                columns: new[] { "TenantId", "EpisodeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemories_TenantId_Importance",
                table: "BusinessMemories",
                columns: new[] { "TenantId", "Importance" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemories_TenantId_SubjectType_SubjectId_CreatedAt",
                table: "BusinessMemories",
                columns: new[] { "TenantId", "SubjectType", "SubjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemoryContexts_TenantId_MemoryId_ContextLayer",
                table: "BusinessMemoryContexts",
                columns: new[] { "TenantId", "MemoryId", "ContextLayer" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemoryDecisions_TenantId_AiDecisionAuditId",
                table: "BusinessMemoryDecisions",
                columns: new[] { "TenantId", "AiDecisionAuditId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemoryEvents_DomainEventId",
                table: "BusinessMemoryEvents",
                column: "DomainEventId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemoryEvents_TenantId_MemoryId",
                table: "BusinessMemoryEvents",
                columns: new[] { "TenantId", "MemoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemoryFacts_TenantId_MemoryId_FactKey",
                table: "BusinessMemoryFacts",
                columns: new[] { "TenantId", "MemoryId", "FactKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemoryInsights_TenantId_CustomerId",
                table: "BusinessMemoryInsights",
                columns: new[] { "TenantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemoryLearnings_TenantId_StrategyKey",
                table: "BusinessMemoryLearnings",
                columns: new[] { "TenantId", "StrategyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemoryObservations_TenantId_SubjectType_SubjectId_O~",
                table: "BusinessMemoryObservations",
                columns: new[] { "TenantId", "SubjectType", "SubjectId", "ObservedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemoryOutcomes_TenantId_MemoryId",
                table: "BusinessMemoryOutcomes",
                columns: new[] { "TenantId", "MemoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemoryRelationships_TenantId_FromType_FromId",
                table: "BusinessMemoryRelationships",
                columns: new[] { "TenantId", "FromType", "FromId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMemoryRelationships_TenantId_ToType_ToId",
                table: "BusinessMemoryRelationships",
                columns: new[] { "TenantId", "ToType", "ToId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessMemories");

            migrationBuilder.DropTable(
                name: "BusinessMemoryContexts");

            migrationBuilder.DropTable(
                name: "BusinessMemoryDecisions");

            migrationBuilder.DropTable(
                name: "BusinessMemoryEvents");

            migrationBuilder.DropTable(
                name: "BusinessMemoryFacts");

            migrationBuilder.DropTable(
                name: "BusinessMemoryInsights");

            migrationBuilder.DropTable(
                name: "BusinessMemoryLearnings");

            migrationBuilder.DropTable(
                name: "BusinessMemoryObservations");

            migrationBuilder.DropTable(
                name: "BusinessMemoryOutcomes");

            migrationBuilder.DropTable(
                name: "BusinessMemoryRelationships");
        }
    }
}
