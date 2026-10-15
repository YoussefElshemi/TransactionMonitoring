using System;
using Core.Enums;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeMatchToActionExplicit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Result",
                schema: "Service",
                table: "RuleEvaluations",
                newName: "Matched");

            migrationBuilder.RenameColumn(
                name: "OriginalResult",
                schema: "Service",
                table: "BackTestResults",
                newName: "OriginalMatched");

            migrationBuilder.RenameColumn(
                name: "NewResult",
                schema: "Service",
                table: "BackTestResults",
                newName: "NewMatched");

            migrationBuilder.AddColumn<int>(
                name: "MatchPolarityId",
                schema: "Service",
                table: "RuleVersions",
                type: "integer",
                nullable: false,
                defaultValue: (int)RuleMatchPolarity.MatchesWhenFalse);

            migrationBuilder.AddColumn<int>(
                name: "MatchPolarityId",
                schema: "Service",
                table: "Rules",
                type: "integer",
                nullable: false,
                defaultValue: (int)RuleMatchPolarity.MatchesWhenFalse);

            migrationBuilder.CreateTable(
                name: "RuleMatchPolarities",
                schema: "Service",
                columns: table => new
                {
                    RuleMatchPolarityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleMatchPolarities", x => x.RuleMatchPolarityId);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "RuleMatchPolarities",
                columns: new[] { "RuleMatchPolarityId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MatchesWhenTrue", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MatchesWhenFalse", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuleVersions_MatchPolarityId",
                schema: "Service",
                table: "RuleVersions",
                column: "MatchPolarityId");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_MatchPolarityId",
                schema: "Service",
                table: "Rules",
                column: "MatchPolarityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_RuleMatchPolarities_MatchPolarityId",
                schema: "Service",
                table: "Rules",
                column: "MatchPolarityId",
                principalSchema: "Service",
                principalTable: "RuleMatchPolarities",
                principalColumn: "RuleMatchPolarityId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleVersions_RuleMatchPolarities_MatchPolarityId",
                schema: "Service",
                table: "RuleVersions",
                column: "MatchPolarityId",
                principalSchema: "Service",
                principalTable: "RuleMatchPolarities",
                principalColumn: "RuleMatchPolarityId",
                onDelete: ReferentialAction.Cascade);

            // Every row written before this migration stored the raw evaluation result under the
            // inverted polarity, where a rule fired when its body evaluated to false. The columns
            // now mean "the rule matched", so the historical values are flipped once to match.
            migrationBuilder.Sql(
                """
                UPDATE "Service"."RuleEvaluations" SET "Matched" = NOT "Matched";
                UPDATE "Service"."BackTestResults" SET "OriginalMatched" = NOT "OriginalMatched", "NewMatched" = NOT "NewMatched";
                ALTER TABLE "Service"."Rules" ALTER COLUMN "MatchPolarityId" DROP DEFAULT;
                ALTER TABLE "Service"."RuleVersions" ALTER COLUMN "MatchPolarityId" DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Service"."RuleEvaluations" SET "Matched" = NOT "Matched";
                UPDATE "Service"."BackTestResults" SET "OriginalMatched" = NOT "OriginalMatched", "NewMatched" = NOT "NewMatched";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Rules_RuleMatchPolarities_MatchPolarityId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleVersions_RuleMatchPolarities_MatchPolarityId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropTable(
                name: "RuleMatchPolarities",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_RuleVersions_MatchPolarityId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropIndex(
                name: "IX_Rules_MatchPolarityId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropColumn(
                name: "MatchPolarityId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropColumn(
                name: "MatchPolarityId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.RenameColumn(
                name: "Matched",
                schema: "Service",
                table: "RuleEvaluations",
                newName: "Result");

            migrationBuilder.RenameColumn(
                name: "OriginalMatched",
                schema: "Service",
                table: "BackTestResults",
                newName: "OriginalResult");

            migrationBuilder.RenameColumn(
                name: "NewMatched",
                schema: "Service",
                table: "BackTestResults",
                newName: "NewResult");
        }
    }
}
