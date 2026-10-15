using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkDecisionsToRuleEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DecisionRuleEvaluationLinks",
                schema: "Service",
                columns: table => new
                {
                    DecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleEvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionRuleEvaluationLinks", x => new { x.DecisionId, x.RuleEvaluationId });
                    table.ForeignKey(
                        name: "FK_DecisionRuleEvaluationLinks_Decisions_DecisionId",
                        column: x => x.DecisionId,
                        principalSchema: "Service",
                        principalTable: "Decisions",
                        principalColumn: "DecisionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecisionRuleEvaluationLinks_RuleEvaluations_RuleEvaluationId",
                        column: x => x.RuleEvaluationId,
                        principalSchema: "Service",
                        principalTable: "RuleEvaluations",
                        principalColumn: "RuleEvaluationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DecisionRuleEvaluationLinks_RuleEvaluationId",
                schema: "Service",
                table: "DecisionRuleEvaluationLinks",
                column: "RuleEvaluationId");

            // Decisions recorded before this table reported the fired rules their case was linked to, or every
            // evaluation on the transaction when they had no case. Backfilling from those two sources keeps
            // history reading as it did rather than emptying it.
            migrationBuilder.Sql(
                """
                INSERT INTO "Service"."DecisionRuleEvaluationLinks" ("DecisionId", "RuleEvaluationId", "CreatedAt", "UpdatedAt")
                SELECT decision."DecisionId", link."RuleEvaluationId", decision."CreatedAt", decision."UpdatedAt"
                FROM "Service"."Decisions" AS decision
                JOIN "Service"."CaseRuleEvaluationLinks" AS link ON link."CaseId" = decision."CaseId"
                WHERE decision."CaseId" IS NOT NULL;

                INSERT INTO "Service"."DecisionRuleEvaluationLinks" ("DecisionId", "RuleEvaluationId", "CreatedAt", "UpdatedAt")
                SELECT decision."DecisionId", evaluation."RuleEvaluationId", decision."CreatedAt", decision."UpdatedAt"
                FROM "Service"."Decisions" AS decision
                JOIN "Service"."RuleEvaluations" AS evaluation ON evaluation."TransactionId" = decision."TransactionId"
                WHERE decision."CaseId" IS NULL AND evaluation."IsDeleted" = FALSE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecisionRuleEvaluationLinks",
                schema: "Service");
        }
    }
}
