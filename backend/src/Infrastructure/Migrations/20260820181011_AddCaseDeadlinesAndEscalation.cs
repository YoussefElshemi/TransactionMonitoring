using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseDeadlinesAndEscalation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueAt",
                schema: "Service",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // Cases opened before the column get the deadline they would have been given, so the breach
            // rate reads the same over history as it does from here on. The house defaults rather than
            // an organisation's own: the targets table is empty at this point, and every one of these
            // cases opened before anybody could have declared one.
            //
            // The hold is folded in the same way the resolver does it — the earlier of the desk's target
            // and the payment's expiry — because a deadline after the payment was auto-verdicted is one
            // nobody could have met.
            migrationBuilder.Sql(
                """
                UPDATE "Service"."Cases" AS c
                SET "DueAt" = LEAST(
                    c."CreatedAt" + (targets.minutes * INTERVAL '1 minute'),
                    COALESCE((
                        SELECT MIN(d."ExpiresAt")
                        FROM "Service"."Decisions" AS d
                        WHERE d."CaseId" = c."CaseId" AND d."ExpiresAt" IS NOT NULL
                    ), 'infinity'::timestamptz))
                FROM (
                    VALUES (1, 10080), (2, 4320), (3, 1440), (4, 480), (5, 120)
                ) AS targets("SeverityId", minutes)
                WHERE targets."SeverityId" = c."SeverityId";
                """);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EscalatedAt",
                schema: "Service",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EscalationRuleId",
                schema: "Service",
                table: "Cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseSlaTargets",
                schema: "Service",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeverityId = table.Column<int>(type: "integer", nullable: false),
                    TargetMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseSlaTargets", x => new { x.OrganizationId, x.SeverityId });
                    table.ForeignKey(
                        name: "FK_CaseSlaTargets_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseSlaTargets_RuleSeverities_SeverityId",
                        column: x => x.SeverityId,
                        principalSchema: "Service",
                        principalTable: "RuleSeverities",
                        principalColumn: "RuleSeverityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EscalationRules",
                schema: "Service",
                columns: table => new
                {
                    EscalationRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UnassignedForMinutes = table.Column<int>(type: "integer", nullable: true),
                    HoldExpiryWithinMinutes = table.Column<int>(type: "integer", nullable: true),
                    MinimumSeverityId = table.Column<int>(type: "integer", nullable: true),
                    Matcher = table.Column<string>(type: "text", nullable: true),
                    TargetQueueId = table.Column<Guid>(type: "uuid", nullable: true),
                    RaiseSeverityToId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationRules", x => x.EscalationRuleId);
                    table.ForeignKey(
                        name: "FK_EscalationRules_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscalationRules_Queues_TargetQueueId",
                        column: x => x.TargetQueueId,
                        principalSchema: "Service",
                        principalTable: "Queues",
                        principalColumn: "QueueId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscalationRules_RuleSeverities_MinimumSeverityId",
                        column: x => x.MinimumSeverityId,
                        principalSchema: "Service",
                        principalTable: "RuleSeverities",
                        principalColumn: "RuleSeverityId");
                    table.ForeignKey(
                        name: "FK_EscalationRules_RuleSeverities_RaiseSeverityToId",
                        column: x => x.RaiseSeverityToId,
                        principalSchema: "Service",
                        principalTable: "RuleSeverities",
                        principalColumn: "RuleSeverityId");
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 20, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "EscalationRule", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_EscalationRuleId",
                schema: "Service",
                table: "Cases",
                column: "EscalationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_OrganizationId_StatusId_DueAt",
                schema: "Service",
                table: "Cases",
                columns: new[] { "OrganizationId", "StatusId", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseSlaTargets_SeverityId",
                schema: "Service",
                table: "CaseSlaTargets",
                column: "SeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_MinimumSeverityId",
                schema: "Service",
                table: "EscalationRules",
                column: "MinimumSeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_OrganizationId_IsEnabled_Priority",
                schema: "Service",
                table: "EscalationRules",
                columns: new[] { "OrganizationId", "IsEnabled", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_OrganizationId_Name",
                schema: "Service",
                table: "EscalationRules",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_RaiseSeverityToId",
                schema: "Service",
                table: "EscalationRules",
                column: "RaiseSeverityToId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_TargetQueueId",
                schema: "Service",
                table: "EscalationRules",
                column: "TargetQueueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_EscalationRules_EscalationRuleId",
                schema: "Service",
                table: "Cases",
                column: "EscalationRuleId",
                principalSchema: "Service",
                principalTable: "EscalationRules",
                principalColumn: "EscalationRuleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_EscalationRules_EscalationRuleId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropTable(
                name: "CaseSlaTargets",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "EscalationRules",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_Cases_EscalationRuleId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_OrganizationId_StatusId_DueAt",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 20);

            migrationBuilder.DropColumn(
                name: "DueAt",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "EscalatedAt",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "EscalationRuleId",
                schema: "Service",
                table: "Cases");
        }
    }
}
