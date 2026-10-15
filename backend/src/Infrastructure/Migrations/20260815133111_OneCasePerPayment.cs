using Core.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OneCasePerPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConditionMatches",
                schema: "Service",
                table: "RuleEvaluations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeverityId",
                schema: "Service",
                table: "Cases",
                type: "integer",
                nullable: false,
                defaultValue: (int)RuleSeverity.Info);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_SeverityId",
                schema: "Service",
                table: "Cases",
                column: "SeverityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_RuleSeverities_SeverityId",
                schema: "Service",
                table: "Cases",
                column: "SeverityId",
                principalSchema: "Service",
                principalTable: "RuleSeverities",
                principalColumn: "RuleSeverityId",
                onDelete: ReferentialAction.Cascade);

            // Cases opened before this column take the severity of the highest-severity version they
            // fired on, so history reads the same as anything opened from here on. A case with no
            // linked evaluation keeps the Info default.
            migrationBuilder.Sql(
                """
                UPDATE "Service"."Cases" AS c
                SET "SeverityId" = severities."SeverityId"
                FROM (
                    SELECT link."CaseId", MAX(version."SeverityId") AS "SeverityId"
                    FROM "Service"."CaseRuleEvaluationLinks" AS link
                    JOIN "Service"."RuleEvaluations" AS evaluation ON evaluation."RuleEvaluationId" = link."RuleEvaluationId"
                    JOIN "Service"."RuleVersions" AS version ON version."RuleVersionId" = evaluation."RuleVersionId"
                    GROUP BY link."CaseId"
                ) AS severities
                WHERE severities."CaseId" = c."CaseId";

                ALTER TABLE "Service"."Cases" ALTER COLUMN "SeverityId" DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_RuleSeverities_SeverityId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_SeverityId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ConditionMatches",
                schema: "Service",
                table: "RuleEvaluations");

            migrationBuilder.DropColumn(
                name: "SeverityId",
                schema: "Service",
                table: "Cases");
        }
    }
}
