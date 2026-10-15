using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModesAndSandbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VelocityProfiles_OrganizationId",
                schema: "Service",
                table: "VelocityProfiles");

            migrationBuilder.DropIndex(
                name: "IX_TransactionSchemas_OrganizationId",
                schema: "Service",
                table: "TransactionSchemas");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrganizationId_ExternalReference",
                schema: "Service",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SuspiciousActivityReportPolicies",
                schema: "Service",
                table: "SuspiciousActivityReportPolicies");

            migrationBuilder.DropIndex(
                name: "IX_Queues_OrganizationId_Name",
                schema: "Service",
                table: "Queues");

            migrationBuilder.DropIndex(
                name: "IX_PartyMappings_OrganizationId",
                schema: "Service",
                table: "PartyMappings");

            migrationBuilder.DropIndex(
                name: "IX_Parties_OrganizationId_Identifier",
                schema: "Service",
                table: "Parties");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IdempotencyRecords",
                schema: "Service",
                table: "IdempotencyRecords");

            migrationBuilder.DropIndex(
                name: "IX_EscalationRules_OrganizationId_Name",
                schema: "Service",
                table: "EscalationRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CaseSlaTargets",
                schema: "Service",
                table: "CaseSlaTargets");

            migrationBuilder.DropIndex(
                name: "IX_CaseReasonCodes_OrganizationId_Code",
                schema: "Service",
                table: "CaseReasonCodes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApprovalPolicies",
                schema: "Service",
                table: "ApprovalPolicies");

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "WebhookConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "VelocityProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "VelocityObservations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "VelocityBackfills",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "Usages",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "TransactionSchemaVersions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "TransactionSchemas",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "SuspiciousActivityReports",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "SuspiciousActivityReportPolicies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "RuleVersions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "RuleSets",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "Rules",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "RuleEvaluations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "RuleDeployments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "Queues",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "PartyMappingVersions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "PartyMappings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "Parties",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "Service",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SuppressedAt",
                schema: "Service",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "NotificationSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "IdempotencyRecords",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "EscalationRules",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "Decisions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "CaseSlaTargets",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "Cases",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "CaseReasonCodes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "CaseNotes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "CaseLinks",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "BackTests",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "Attachments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "Approvals",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "ApprovalPolicies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Identity",
                table: "ApiClientSecrets",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Identity",
                table: "ApiClients",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ModeId",
                schema: "Service",
                table: "Alerts",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SuspiciousActivityReportPolicies",
                schema: "Service",
                table: "SuspiciousActivityReportPolicies",
                columns: new[] { "OrganizationId", "ModeId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_IdempotencyRecords",
                schema: "Service",
                table: "IdempotencyRecords",
                columns: new[] { "OrganizationId", "ModeId", "IdempotencyKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_CaseSlaTargets",
                schema: "Service",
                table: "CaseSlaTargets",
                columns: new[] { "OrganizationId", "ModeId", "SeverityId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApprovalPolicies",
                schema: "Service",
                table: "ApprovalPolicies",
                columns: new[] { "OrganizationId", "ModeId" });

            migrationBuilder.CreateTable(
                name: "Modes",
                schema: "Service",
                columns: table => new
                {
                    ModeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modes", x => x.ModeId);
                });

            migrationBuilder.CreateTable(
                name: "SimulationScenarios",
                schema: "Service",
                columns: table => new
                {
                    SimulationScenarioId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationScenarios", x => x.SimulationScenarioId);
                });

            migrationBuilder.CreateTable(
                name: "SimulationStatuses",
                schema: "Service",
                columns: table => new
                {
                    SimulationStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationStatuses", x => x.SimulationStatusId);
                });

            migrationBuilder.CreateTable(
                name: "Simulations",
                schema: "Service",
                columns: table => new
                {
                    SimulationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModeId = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    TotalPayments = table.Column<int>(type: "integer", nullable: false),
                    ProcessedCount = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Simulations", x => x.SimulationId);
                    table.ForeignKey(
                        name: "FK_Simulations_Modes_ModeId",
                        column: x => x.ModeId,
                        principalSchema: "Service",
                        principalTable: "Modes",
                        principalColumn: "ModeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Simulations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Simulations_SimulationScenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalSchema: "Service",
                        principalTable: "SimulationScenarios",
                        principalColumn: "SimulationScenarioId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Simulations_SimulationStatuses_StatusId",
                        column: x => x.StatusId,
                        principalSchema: "Service",
                        principalTable: "SimulationStatuses",
                        principalColumn: "SimulationStatusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Simulations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "Modes",
                columns: new[] { "ModeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Live", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Test", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 79, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "sandbox:read", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 80, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "sandbox:write", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 81, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "sandbox:reset", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "SimulationScenarios",
                columns: new[] { "SimulationScenarioId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Baseline", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Structuring", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "VelocitySpike", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "HighValue", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Mixed", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "SimulationStatuses",
                columns: new[] { "SimulationStatusId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Pending", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Running", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Completed", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Failed", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Identity",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { 79, new Guid("00000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 79, new Guid("00000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 80, new Guid("00000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 81, new Guid("00000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 79, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 80, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 81, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 79, new Guid("00000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 80, new Guid("00000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 81, new Guid("00000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookConfigurations_ModeId",
                schema: "Service",
                table: "WebhookConfigurations",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_VelocityProfiles_ModeId",
                schema: "Service",
                table: "VelocityProfiles",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_VelocityProfiles_OrganizationId_ModeId",
                schema: "Service",
                table: "VelocityProfiles",
                columns: new[] { "OrganizationId", "ModeId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_VelocityObservations_ModeId",
                schema: "Service",
                table: "VelocityObservations",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_VelocityBackfills_ModeId",
                schema: "Service",
                table: "VelocityBackfills",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Usages_ModeId",
                schema: "Service",
                table: "Usages",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSchemaVersions_ModeId",
                schema: "Service",
                table: "TransactionSchemaVersions",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSchemas_ModeId",
                schema: "Service",
                table: "TransactionSchemas",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSchemas_OrganizationId_ModeId",
                schema: "Service",
                table: "TransactionSchemas",
                columns: new[] { "OrganizationId", "ModeId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ModeId",
                schema: "Service",
                table: "Transactions",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrganizationId_ModeId_ExternalReference",
                schema: "Service",
                table: "Transactions",
                columns: new[] { "OrganizationId", "ModeId", "ExternalReference" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivityReports_ModeId",
                schema: "Service",
                table: "SuspiciousActivityReports",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivityReportPolicies_ModeId",
                schema: "Service",
                table: "SuspiciousActivityReportPolicies",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleVersions_ModeId",
                schema: "Service",
                table: "RuleVersions",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSets_ModeId",
                schema: "Service",
                table: "RuleSets",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_ModeId",
                schema: "Service",
                table: "Rules",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleEvaluations_ModeId",
                schema: "Service",
                table: "RuleEvaluations",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleDeployments_ModeId",
                schema: "Service",
                table: "RuleDeployments",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_ModeId",
                schema: "Service",
                table: "Queues",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_OrganizationId_ModeId_Name",
                schema: "Service",
                table: "Queues",
                columns: new[] { "OrganizationId", "ModeId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PartyMappingVersions_ModeId",
                schema: "Service",
                table: "PartyMappingVersions",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyMappings_ModeId",
                schema: "Service",
                table: "PartyMappings",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyMappings_OrganizationId_ModeId",
                schema: "Service",
                table: "PartyMappings",
                columns: new[] { "OrganizationId", "ModeId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_ModeId",
                schema: "Service",
                table: "Parties",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_OrganizationId_ModeId_Identifier",
                schema: "Service",
                table: "Parties",
                columns: new[] { "OrganizationId", "ModeId", "Identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ModeId",
                schema: "Service",
                table: "OutboxMessages",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OrganizationId_ChannelId_CreatedAt",
                schema: "Service",
                table: "OutboxMessages",
                columns: new[] { "OrganizationId", "ChannelId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSubscriptions_ModeId",
                schema: "Service",
                table: "NotificationSubscriptions",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_ModeId",
                schema: "Service",
                table: "IdempotencyRecords",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_ModeId",
                schema: "Service",
                table: "EscalationRules",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_OrganizationId_ModeId_Name",
                schema: "Service",
                table: "EscalationRules",
                columns: new[] { "OrganizationId", "ModeId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_ModeId",
                schema: "Service",
                table: "Decisions",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseSlaTargets_ModeId",
                schema: "Service",
                table: "CaseSlaTargets",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_ModeId",
                schema: "Service",
                table: "Cases",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReasonCodes_ModeId",
                schema: "Service",
                table: "CaseReasonCodes",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReasonCodes_OrganizationId_ModeId_Code",
                schema: "Service",
                table: "CaseReasonCodes",
                columns: new[] { "OrganizationId", "ModeId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CaseNotes_ModeId",
                schema: "Service",
                table: "CaseNotes",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseLinks_ModeId",
                schema: "Service",
                table: "CaseLinks",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_BackTests_ModeId",
                schema: "Service",
                table: "BackTests",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_ModeId",
                schema: "Service",
                table: "Attachments",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_ModeId",
                schema: "Service",
                table: "Approvals",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalPolicies_ModeId",
                schema: "Service",
                table: "ApprovalPolicies",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClientSecrets_ModeId",
                schema: "Identity",
                table: "ApiClientSecrets",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_ModeId",
                schema: "Identity",
                table: "ApiClients",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_ModeId",
                schema: "Service",
                table: "Alerts",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_CreatedByUserId",
                schema: "Service",
                table: "Simulations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_ModeId",
                schema: "Service",
                table: "Simulations",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_OrganizationId_CreatedAt",
                schema: "Service",
                table: "Simulations",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_ScenarioId",
                schema: "Service",
                table: "Simulations",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_StatusId",
                schema: "Service",
                table: "Simulations",
                column: "StatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Modes_ModeId",
                schema: "Service",
                table: "Alerts",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiClients_Modes_ModeId",
                schema: "Identity",
                table: "ApiClients",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiClientSecrets_Modes_ModeId",
                schema: "Identity",
                table: "ApiClientSecrets",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalPolicies_Modes_ModeId",
                schema: "Service",
                table: "ApprovalPolicies",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Modes_ModeId",
                schema: "Service",
                table: "Approvals",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Modes_ModeId",
                schema: "Service",
                table: "Attachments",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BackTests_Modes_ModeId",
                schema: "Service",
                table: "BackTests",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseLinks_Modes_ModeId",
                schema: "Service",
                table: "CaseLinks",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseNotes_Modes_ModeId",
                schema: "Service",
                table: "CaseNotes",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseReasonCodes_Modes_ModeId",
                schema: "Service",
                table: "CaseReasonCodes",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Modes_ModeId",
                schema: "Service",
                table: "Cases",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseSlaTargets_Modes_ModeId",
                schema: "Service",
                table: "CaseSlaTargets",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_Modes_ModeId",
                schema: "Service",
                table: "Decisions",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EscalationRules_Modes_ModeId",
                schema: "Service",
                table: "EscalationRules",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IdempotencyRecords_Modes_ModeId",
                schema: "Service",
                table: "IdempotencyRecords",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationSubscriptions_Modes_ModeId",
                schema: "Service",
                table: "NotificationSubscriptions",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OutboxMessages_Modes_ModeId",
                schema: "Service",
                table: "OutboxMessages",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OutboxMessages_Organizations_OrganizationId",
                schema: "Service",
                table: "OutboxMessages",
                column: "OrganizationId",
                principalSchema: "Service",
                principalTable: "Organizations",
                principalColumn: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Parties_Modes_ModeId",
                schema: "Service",
                table: "Parties",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PartyMappings_Modes_ModeId",
                schema: "Service",
                table: "PartyMappings",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PartyMappingVersions_Modes_ModeId",
                schema: "Service",
                table: "PartyMappingVersions",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Queues_Modes_ModeId",
                schema: "Service",
                table: "Queues",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleDeployments_Modes_ModeId",
                schema: "Service",
                table: "RuleDeployments",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleEvaluations_Modes_ModeId",
                schema: "Service",
                table: "RuleEvaluations",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_Modes_ModeId",
                schema: "Service",
                table: "Rules",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleSets_Modes_ModeId",
                schema: "Service",
                table: "RuleSets",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleVersions_Modes_ModeId",
                schema: "Service",
                table: "RuleVersions",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SuspiciousActivityReportPolicies_Modes_ModeId",
                schema: "Service",
                table: "SuspiciousActivityReportPolicies",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SuspiciousActivityReports_Modes_ModeId",
                schema: "Service",
                table: "SuspiciousActivityReports",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Modes_ModeId",
                schema: "Service",
                table: "Transactions",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionSchemas_Modes_ModeId",
                schema: "Service",
                table: "TransactionSchemas",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionSchemaVersions_Modes_ModeId",
                schema: "Service",
                table: "TransactionSchemaVersions",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usages_Modes_ModeId",
                schema: "Service",
                table: "Usages",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VelocityBackfills_Modes_ModeId",
                schema: "Service",
                table: "VelocityBackfills",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VelocityObservations_Modes_ModeId",
                schema: "Service",
                table: "VelocityObservations",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VelocityProfiles_Modes_ModeId",
                schema: "Service",
                table: "VelocityProfiles",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WebhookConfigurations_Modes_ModeId",
                schema: "Service",
                table: "WebhookConfigurations",
                column: "ModeId",
                principalSchema: "Service",
                principalTable: "Modes",
                principalColumn: "ModeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Modes_ModeId",
                schema: "Service",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiClients_Modes_ModeId",
                schema: "Identity",
                table: "ApiClients");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiClientSecrets_Modes_ModeId",
                schema: "Identity",
                table: "ApiClientSecrets");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalPolicies_Modes_ModeId",
                schema: "Service",
                table: "ApprovalPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_Modes_ModeId",
                schema: "Service",
                table: "Approvals");

            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Modes_ModeId",
                schema: "Service",
                table: "Attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_BackTests_Modes_ModeId",
                schema: "Service",
                table: "BackTests");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseLinks_Modes_ModeId",
                schema: "Service",
                table: "CaseLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseNotes_Modes_ModeId",
                schema: "Service",
                table: "CaseNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseReasonCodes_Modes_ModeId",
                schema: "Service",
                table: "CaseReasonCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Modes_ModeId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseSlaTargets_Modes_ModeId",
                schema: "Service",
                table: "CaseSlaTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_Modes_ModeId",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_EscalationRules_Modes_ModeId",
                schema: "Service",
                table: "EscalationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_IdempotencyRecords_Modes_ModeId",
                schema: "Service",
                table: "IdempotencyRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationSubscriptions_Modes_ModeId",
                schema: "Service",
                table: "NotificationSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_OutboxMessages_Modes_ModeId",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_OutboxMessages_Organizations_OrganizationId",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_Parties_Modes_ModeId",
                schema: "Service",
                table: "Parties");

            migrationBuilder.DropForeignKey(
                name: "FK_PartyMappings_Modes_ModeId",
                schema: "Service",
                table: "PartyMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_PartyMappingVersions_Modes_ModeId",
                schema: "Service",
                table: "PartyMappingVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_Queues_Modes_ModeId",
                schema: "Service",
                table: "Queues");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleDeployments_Modes_ModeId",
                schema: "Service",
                table: "RuleDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleEvaluations_Modes_ModeId",
                schema: "Service",
                table: "RuleEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_Rules_Modes_ModeId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleSets_Modes_ModeId",
                schema: "Service",
                table: "RuleSets");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleVersions_Modes_ModeId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_SuspiciousActivityReportPolicies_Modes_ModeId",
                schema: "Service",
                table: "SuspiciousActivityReportPolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_SuspiciousActivityReports_Modes_ModeId",
                schema: "Service",
                table: "SuspiciousActivityReports");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Modes_ModeId",
                schema: "Service",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionSchemas_Modes_ModeId",
                schema: "Service",
                table: "TransactionSchemas");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionSchemaVersions_Modes_ModeId",
                schema: "Service",
                table: "TransactionSchemaVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_Usages_Modes_ModeId",
                schema: "Service",
                table: "Usages");

            migrationBuilder.DropForeignKey(
                name: "FK_VelocityBackfills_Modes_ModeId",
                schema: "Service",
                table: "VelocityBackfills");

            migrationBuilder.DropForeignKey(
                name: "FK_VelocityObservations_Modes_ModeId",
                schema: "Service",
                table: "VelocityObservations");

            migrationBuilder.DropForeignKey(
                name: "FK_VelocityProfiles_Modes_ModeId",
                schema: "Service",
                table: "VelocityProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_WebhookConfigurations_Modes_ModeId",
                schema: "Service",
                table: "WebhookConfigurations");

            migrationBuilder.DropTable(
                name: "Simulations",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "Modes",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "SimulationScenarios",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "SimulationStatuses",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_WebhookConfigurations_ModeId",
                schema: "Service",
                table: "WebhookConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_VelocityProfiles_ModeId",
                schema: "Service",
                table: "VelocityProfiles");

            migrationBuilder.DropIndex(
                name: "IX_VelocityProfiles_OrganizationId_ModeId",
                schema: "Service",
                table: "VelocityProfiles");

            migrationBuilder.DropIndex(
                name: "IX_VelocityObservations_ModeId",
                schema: "Service",
                table: "VelocityObservations");

            migrationBuilder.DropIndex(
                name: "IX_VelocityBackfills_ModeId",
                schema: "Service",
                table: "VelocityBackfills");

            migrationBuilder.DropIndex(
                name: "IX_Usages_ModeId",
                schema: "Service",
                table: "Usages");

            migrationBuilder.DropIndex(
                name: "IX_TransactionSchemaVersions_ModeId",
                schema: "Service",
                table: "TransactionSchemaVersions");

            migrationBuilder.DropIndex(
                name: "IX_TransactionSchemas_ModeId",
                schema: "Service",
                table: "TransactionSchemas");

            migrationBuilder.DropIndex(
                name: "IX_TransactionSchemas_OrganizationId_ModeId",
                schema: "Service",
                table: "TransactionSchemas");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ModeId",
                schema: "Service",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrganizationId_ModeId_ExternalReference",
                schema: "Service",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_SuspiciousActivityReports_ModeId",
                schema: "Service",
                table: "SuspiciousActivityReports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SuspiciousActivityReportPolicies",
                schema: "Service",
                table: "SuspiciousActivityReportPolicies");

            migrationBuilder.DropIndex(
                name: "IX_SuspiciousActivityReportPolicies_ModeId",
                schema: "Service",
                table: "SuspiciousActivityReportPolicies");

            migrationBuilder.DropIndex(
                name: "IX_RuleVersions_ModeId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropIndex(
                name: "IX_RuleSets_ModeId",
                schema: "Service",
                table: "RuleSets");

            migrationBuilder.DropIndex(
                name: "IX_Rules_ModeId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropIndex(
                name: "IX_RuleEvaluations_ModeId",
                schema: "Service",
                table: "RuleEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_RuleDeployments_ModeId",
                schema: "Service",
                table: "RuleDeployments");

            migrationBuilder.DropIndex(
                name: "IX_Queues_ModeId",
                schema: "Service",
                table: "Queues");

            migrationBuilder.DropIndex(
                name: "IX_Queues_OrganizationId_ModeId_Name",
                schema: "Service",
                table: "Queues");

            migrationBuilder.DropIndex(
                name: "IX_PartyMappingVersions_ModeId",
                schema: "Service",
                table: "PartyMappingVersions");

            migrationBuilder.DropIndex(
                name: "IX_PartyMappings_ModeId",
                schema: "Service",
                table: "PartyMappings");

            migrationBuilder.DropIndex(
                name: "IX_PartyMappings_OrganizationId_ModeId",
                schema: "Service",
                table: "PartyMappings");

            migrationBuilder.DropIndex(
                name: "IX_Parties_ModeId",
                schema: "Service",
                table: "Parties");

            migrationBuilder.DropIndex(
                name: "IX_Parties_OrganizationId_ModeId_Identifier",
                schema: "Service",
                table: "Parties");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ModeId",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_OrganizationId_ChannelId_CreatedAt",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_NotificationSubscriptions_ModeId",
                schema: "Service",
                table: "NotificationSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IdempotencyRecords",
                schema: "Service",
                table: "IdempotencyRecords");

            migrationBuilder.DropIndex(
                name: "IX_IdempotencyRecords_ModeId",
                schema: "Service",
                table: "IdempotencyRecords");

            migrationBuilder.DropIndex(
                name: "IX_EscalationRules_ModeId",
                schema: "Service",
                table: "EscalationRules");

            migrationBuilder.DropIndex(
                name: "IX_EscalationRules_OrganizationId_ModeId_Name",
                schema: "Service",
                table: "EscalationRules");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_ModeId",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CaseSlaTargets",
                schema: "Service",
                table: "CaseSlaTargets");

            migrationBuilder.DropIndex(
                name: "IX_CaseSlaTargets_ModeId",
                schema: "Service",
                table: "CaseSlaTargets");

            migrationBuilder.DropIndex(
                name: "IX_Cases_ModeId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_CaseReasonCodes_ModeId",
                schema: "Service",
                table: "CaseReasonCodes");

            migrationBuilder.DropIndex(
                name: "IX_CaseReasonCodes_OrganizationId_ModeId_Code",
                schema: "Service",
                table: "CaseReasonCodes");

            migrationBuilder.DropIndex(
                name: "IX_CaseNotes_ModeId",
                schema: "Service",
                table: "CaseNotes");

            migrationBuilder.DropIndex(
                name: "IX_CaseLinks_ModeId",
                schema: "Service",
                table: "CaseLinks");

            migrationBuilder.DropIndex(
                name: "IX_BackTests_ModeId",
                schema: "Service",
                table: "BackTests");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_ModeId",
                schema: "Service",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Approvals_ModeId",
                schema: "Service",
                table: "Approvals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApprovalPolicies",
                schema: "Service",
                table: "ApprovalPolicies");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalPolicies_ModeId",
                schema: "Service",
                table: "ApprovalPolicies");

            migrationBuilder.DropIndex(
                name: "IX_ApiClientSecrets_ModeId",
                schema: "Identity",
                table: "ApiClientSecrets");

            migrationBuilder.DropIndex(
                name: "IX_ApiClients_ModeId",
                schema: "Identity",
                table: "ApiClients");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_ModeId",
                schema: "Service",
                table: "Alerts");

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 79, new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 79, new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 80, new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 81, new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 79, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 80, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 81, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 79, new Guid("00000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 80, new Guid("00000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 81, new Guid("00000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 79);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 80);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 81);

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "WebhookConfigurations");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "VelocityProfiles");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "VelocityObservations");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "VelocityBackfills");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "Usages");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "TransactionSchemaVersions");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "TransactionSchemas");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "SuspiciousActivityReports");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "SuspiciousActivityReportPolicies");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "RuleSets");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "RuleEvaluations");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "RuleDeployments");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "Queues");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "PartyMappingVersions");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "PartyMappings");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "SuppressedAt",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "NotificationSubscriptions");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "IdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "EscalationRules");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "CaseSlaTargets");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "CaseReasonCodes");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "CaseNotes");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "CaseLinks");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "BackTests");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "Approvals");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "ApprovalPolicies");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Identity",
                table: "ApiClientSecrets");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Identity",
                table: "ApiClients");

            migrationBuilder.DropColumn(
                name: "ModeId",
                schema: "Service",
                table: "Alerts");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SuspiciousActivityReportPolicies",
                schema: "Service",
                table: "SuspiciousActivityReportPolicies",
                column: "OrganizationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IdempotencyRecords",
                schema: "Service",
                table: "IdempotencyRecords",
                columns: new[] { "OrganizationId", "IdempotencyKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_CaseSlaTargets",
                schema: "Service",
                table: "CaseSlaTargets",
                columns: new[] { "OrganizationId", "SeverityId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApprovalPolicies",
                schema: "Service",
                table: "ApprovalPolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_VelocityProfiles_OrganizationId",
                schema: "Service",
                table: "VelocityProfiles",
                column: "OrganizationId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSchemas_OrganizationId",
                schema: "Service",
                table: "TransactionSchemas",
                column: "OrganizationId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrganizationId_ExternalReference",
                schema: "Service",
                table: "Transactions",
                columns: new[] { "OrganizationId", "ExternalReference" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_OrganizationId_Name",
                schema: "Service",
                table: "Queues",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PartyMappings_OrganizationId",
                schema: "Service",
                table: "PartyMappings",
                column: "OrganizationId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_OrganizationId_Identifier",
                schema: "Service",
                table: "Parties",
                columns: new[] { "OrganizationId", "Identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_OrganizationId_Name",
                schema: "Service",
                table: "EscalationRules",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReasonCodes_OrganizationId_Code",
                schema: "Service",
                table: "CaseReasonCodes",
                columns: new[] { "OrganizationId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
