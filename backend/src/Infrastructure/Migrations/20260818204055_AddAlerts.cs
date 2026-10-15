using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Decisions_CaseId",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Cases_OrganizationId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.CreateSequence<int>(
                name: "AlertNumberSequence",
                schema: "Service");

            migrationBuilder.AddColumn<int>(
                name: "AlertSubjectRoleId",
                schema: "Service",
                table: "Rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeduplicationWindow",
                schema: "Service",
                table: "Rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaseGroupingPeriod",
                schema: "Service",
                table: "Organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeduplicationWindow",
                schema: "Service",
                table: "Organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PartyId",
                schema: "Service",
                table: "Cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AlertStatuses",
                schema: "Service",
                columns: table => new
                {
                    AlertStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertStatuses", x => x.AlertStatusId);
                });

            migrationBuilder.CreateTable(
                name: "AlertSuppressionReasons",
                schema: "Service",
                columns: table => new
                {
                    AlertSuppressionReasonId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertSuppressionReasons", x => x.AlertSuppressionReasonId);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                schema: "Service",
                columns: table => new
                {
                    AlertId = table.Column<Guid>(type: "uuid", nullable: false),
                    FriendlyName = table.Column<string>(type: "text", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    SeverityId = table.Column<int>(type: "integer", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleEvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartyRoleId = table.Column<int>(type: "integer", nullable: true),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    SuppressionReasonId = table.Column<int>(type: "integer", nullable: true),
                    SuppressedByAlertId = table.Column<Guid>(type: "uuid", nullable: true),
                    RaisedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.AlertId);
                    table.ForeignKey(
                        name: "FK_Alerts_AlertStatuses_StatusId",
                        column: x => x.StatusId,
                        principalSchema: "Service",
                        principalTable: "AlertStatuses",
                        principalColumn: "AlertStatusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alerts_AlertSuppressionReasons_SuppressionReasonId",
                        column: x => x.SuppressionReasonId,
                        principalSchema: "Service",
                        principalTable: "AlertSuppressionReasons",
                        principalColumn: "AlertSuppressionReasonId");
                    table.ForeignKey(
                        name: "FK_Alerts_Cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Service",
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alerts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alerts_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "Service",
                        principalTable: "Parties",
                        principalColumn: "PartyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alerts_PartyRoles_PartyRoleId",
                        column: x => x.PartyRoleId,
                        principalSchema: "Service",
                        principalTable: "PartyRoles",
                        principalColumn: "PartyRoleId");
                    table.ForeignKey(
                        name: "FK_Alerts_RuleEvaluations_RuleEvaluationId",
                        column: x => x.RuleEvaluationId,
                        principalSchema: "Service",
                        principalTable: "RuleEvaluations",
                        principalColumn: "RuleEvaluationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alerts_RuleSeverities_SeverityId",
                        column: x => x.SeverityId,
                        principalSchema: "Service",
                        principalTable: "RuleSeverities",
                        principalColumn: "RuleSeverityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alerts_RuleVersions_RuleVersionId",
                        column: x => x.RuleVersionId,
                        principalSchema: "Service",
                        principalTable: "RuleVersions",
                        principalColumn: "RuleVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alerts_Rules_RuleId",
                        column: x => x.RuleId,
                        principalSchema: "Service",
                        principalTable: "Rules",
                        principalColumn: "RuleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alerts_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalSchema: "Service",
                        principalTable: "Transactions",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AlertStatuses",
                columns: new[] { "AlertStatusId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Open", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Suppressed", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Closed", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AlertSuppressionReasons",
                columns: new[] { "AlertSuppressionReasonId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "DuplicateInWindow", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 17, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Alert", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                schema: "Service",
                table: "Organizations",
                keyColumn: "OrganizationId",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CaseGroupingPeriod", "DeduplicationWindow" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Rules_AlertSubjectRoleId",
                schema: "Service",
                table: "Rules",
                column: "AlertSubjectRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_CaseId",
                schema: "Service",
                table: "Decisions",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_OrganizationId_PartyId_StatusId",
                schema: "Service",
                table: "Cases",
                columns: new[] { "OrganizationId", "PartyId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_PartyId",
                schema: "Service",
                table: "Cases",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_CaseId",
                schema: "Service",
                table: "Alerts",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_OrganizationId_PartyId_RuleId_RaisedAt",
                schema: "Service",
                table: "Alerts",
                columns: new[] { "OrganizationId", "PartyId", "RuleId", "RaisedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_OrganizationId_PartyId_StatusId_RaisedAt",
                schema: "Service",
                table: "Alerts",
                columns: new[] { "OrganizationId", "PartyId", "StatusId", "RaisedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_PartyId",
                schema: "Service",
                table: "Alerts",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_PartyRoleId",
                schema: "Service",
                table: "Alerts",
                column: "PartyRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_RuleEvaluationId",
                schema: "Service",
                table: "Alerts",
                column: "RuleEvaluationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_RuleId",
                schema: "Service",
                table: "Alerts",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_RuleVersionId",
                schema: "Service",
                table: "Alerts",
                column: "RuleVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_SeverityId",
                schema: "Service",
                table: "Alerts",
                column: "SeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_StatusId",
                schema: "Service",
                table: "Alerts",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_SuppressionReasonId",
                schema: "Service",
                table: "Alerts",
                column: "SuppressionReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TransactionId",
                schema: "Service",
                table: "Alerts",
                column: "TransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Parties_PartyId",
                schema: "Service",
                table: "Cases",
                column: "PartyId",
                principalSchema: "Service",
                principalTable: "Parties",
                principalColumn: "PartyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_PartyRoles_AlertSubjectRoleId",
                schema: "Service",
                table: "Rules",
                column: "AlertSubjectRoleId",
                principalSchema: "Service",
                principalTable: "PartyRoles",
                principalColumn: "PartyRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Parties_PartyId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Rules_PartyRoles_AlertSubjectRoleId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropTable(
                name: "Alerts",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "AlertStatuses",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "AlertSuppressionReasons",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_Rules_AlertSubjectRoleId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_CaseId",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Cases_OrganizationId_PartyId_StatusId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_PartyId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 17);

            migrationBuilder.DropColumn(
                name: "AlertSubjectRoleId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropColumn(
                name: "DeduplicationWindow",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropColumn(
                name: "CaseGroupingPeriod",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DeduplicationWindow",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PartyId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropSequence(
                name: "AlertNumberSequence",
                schema: "Service");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_CaseId",
                schema: "Service",
                table: "Decisions",
                column: "CaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_OrganizationId",
                schema: "Service",
                table: "Cases",
                column: "OrganizationId");
        }
    }
}
