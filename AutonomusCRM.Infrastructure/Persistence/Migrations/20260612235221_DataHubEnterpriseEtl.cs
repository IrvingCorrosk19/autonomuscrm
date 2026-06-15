using System;
using System.Collections.Generic;
using AutonomusCRM.Application.DataHub;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutonomusCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DataHubEnterpriseEtl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataHubImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileFormat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetEntity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LoadMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    ProcessedRows = table.Column<int>(type: "integer", nullable: false),
                    SuccessRows = table.Column<int>(type: "integer", nullable: false),
                    FailedRows = table.Column<int>(type: "integer", nullable: false),
                    SkippedRows = table.Column<int>(type: "integer", nullable: false),
                    CreatedRecords = table.Column<int>(type: "integer", nullable: false),
                    UpdatedRecords = table.Column<int>(type: "integer", nullable: false),
                    StoredFilePath = table.Column<string>(type: "text", nullable: true),
                    DetectedEncoding = table.Column<string>(type: "text", nullable: true),
                    DetectedDelimiter = table.Column<string>(type: "text", nullable: true),
                    DetectedColumns = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    IsDryRun = table.Column<bool>(type: "boolean", nullable: false),
                    RollbackAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorSummary = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHubImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataHubImportTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetEntity = table.Column<string>(type: "text", nullable: false),
                    FileFormat = table.Column<string>(type: "text", nullable: false),
                    Mappings = table.Column<List<DataHubTemplateMapping>>(type: "jsonb", nullable: false),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHubImportTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataHubTransformationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetEntity = table.Column<string>(type: "text", nullable: false),
                    TargetField = table.Column<string>(type: "text", nullable: false),
                    TransformType = table.Column<string>(type: "text", nullable: false),
                    Parameters = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHubTransformationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataHubValidationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetEntity = table.Column<string>(type: "text", nullable: false),
                    TargetField = table.Column<string>(type: "text", nullable: false),
                    ValidationType = table.Column<string>(type: "text", nullable: false),
                    Parameters = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHubValidationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataHubImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<int>(type: "integer", nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: false),
                    ProcessedCount = table.Column<int>(type: "integer", nullable: false),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHubImportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataHubImportBatches_DataHubImportJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "DataHubImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataHubImportErrors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FieldName = table.Column<string>(type: "text", nullable: true),
                    RawValue = table.Column<string>(type: "text", nullable: true),
                    IsRetryable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHubImportErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataHubImportErrors_DataHubImportJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "DataHubImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataHubImportLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Details = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHubImportLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataHubImportLogs_DataHubImportJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "DataHubImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataHubImportMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceColumn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetField = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultValue = table.Column<string>(type: "text", nullable: true),
                    TransformRule = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHubImportMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataHubImportMappings_DataHubImportJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "DataHubImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataHubImportRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    BatchNumber = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RawData = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    TransformedData = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    CreatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityType = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHubImportRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataHubImportRows_DataHubImportJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "DataHubImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataHubRollbackSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    PreviousState = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataHubRollbackSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataHubRollbackSnapshots_DataHubImportJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "DataHubImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataHubImportBatches_JobId_BatchNumber",
                table: "DataHubImportBatches",
                columns: new[] { "JobId", "BatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHubImportErrors_JobId_RowNumber",
                table: "DataHubImportErrors",
                columns: new[] { "JobId", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHubImportJobs_TenantId_CreatedAt",
                table: "DataHubImportJobs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHubImportJobs_TenantId_Status",
                table: "DataHubImportJobs",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHubImportLogs_JobId_CreatedAt",
                table: "DataHubImportLogs",
                columns: new[] { "JobId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHubImportMappings_JobId_SortOrder",
                table: "DataHubImportMappings",
                columns: new[] { "JobId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHubImportRows_JobId_RowNumber",
                table: "DataHubImportRows",
                columns: new[] { "JobId", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHubImportTemplates_TenantId_Name",
                table: "DataHubImportTemplates",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHubRollbackSnapshots_JobId_EntityId",
                table: "DataHubRollbackSnapshots",
                columns: new[] { "JobId", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHubTransformationRules_TenantId_TargetEntity",
                table: "DataHubTransformationRules",
                columns: new[] { "TenantId", "TargetEntity" });

            migrationBuilder.CreateIndex(
                name: "IX_DataHubValidationRules_TenantId_TargetEntity",
                table: "DataHubValidationRules",
                columns: new[] { "TenantId", "TargetEntity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataHubImportBatches");

            migrationBuilder.DropTable(
                name: "DataHubImportErrors");

            migrationBuilder.DropTable(
                name: "DataHubImportLogs");

            migrationBuilder.DropTable(
                name: "DataHubImportMappings");

            migrationBuilder.DropTable(
                name: "DataHubImportRows");

            migrationBuilder.DropTable(
                name: "DataHubImportTemplates");

            migrationBuilder.DropTable(
                name: "DataHubRollbackSnapshots");

            migrationBuilder.DropTable(
                name: "DataHubTransformationRules");

            migrationBuilder.DropTable(
                name: "DataHubValidationRules");

            migrationBuilder.DropTable(
                name: "DataHubImportJobs");
        }
    }
}
