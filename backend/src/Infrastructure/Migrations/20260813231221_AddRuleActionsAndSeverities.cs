using System;
using Core.Enums;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleActionsAndSeverities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActionId",
                schema: "Service",
                table: "RuleVersions",
                type: "integer",
                nullable: false,
                defaultValue: (int)RuleAction.Review);

            migrationBuilder.AddColumn<int>(
                name: "RiskWeight",
                schema: "Service",
                table: "RuleVersions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SeverityId",
                schema: "Service",
                table: "RuleVersions",
                type: "integer",
                nullable: false,
                defaultValue: (int)RuleSeverity.Medium);

            migrationBuilder.AddColumn<int>(
                name: "ReviewThreshold",
                schema: "Service",
                table: "RuleSets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActionId",
                schema: "Service",
                table: "Rules",
                type: "integer",
                nullable: false,
                defaultValue: (int)RuleAction.Review);

            migrationBuilder.AddColumn<int>(
                name: "RiskWeight",
                schema: "Service",
                table: "Rules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SeverityId",
                schema: "Service",
                table: "Rules",
                type: "integer",
                nullable: false,
                defaultValue: (int)RuleSeverity.Medium);

            migrationBuilder.CreateTable(
                name: "RuleActions",
                schema: "Service",
                columns: table => new
                {
                    RuleActionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleActions", x => x.RuleActionId);
                });

            migrationBuilder.CreateTable(
                name: "RuleSeverities",
                schema: "Service",
                columns: table => new
                {
                    RuleSeverityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSeverities", x => x.RuleSeverityId);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "RuleActions",
                columns: new[] { "RuleActionId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Allow", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Flag", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Review", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Block", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "RuleSeverities",
                columns: new[] { "RuleSeverityId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Info", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Low", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Medium", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "High", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Critical", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuleVersions_ActionId",
                schema: "Service",
                table: "RuleVersions",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleVersions_SeverityId",
                schema: "Service",
                table: "RuleVersions",
                column: "SeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_ActionId",
                schema: "Service",
                table: "Rules",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_SeverityId",
                schema: "Service",
                table: "Rules",
                column: "SeverityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_RuleActions_ActionId",
                schema: "Service",
                table: "Rules",
                column: "ActionId",
                principalSchema: "Service",
                principalTable: "RuleActions",
                principalColumn: "RuleActionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_RuleSeverities_SeverityId",
                schema: "Service",
                table: "Rules",
                column: "SeverityId",
                principalSchema: "Service",
                principalTable: "RuleSeverities",
                principalColumn: "RuleSeverityId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleVersions_RuleActions_ActionId",
                schema: "Service",
                table: "RuleVersions",
                column: "ActionId",
                principalSchema: "Service",
                principalTable: "RuleActions",
                principalColumn: "RuleActionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleVersions_RuleSeverities_SeverityId",
                schema: "Service",
                table: "RuleVersions",
                column: "SeverityId",
                principalSchema: "Service",
                principalTable: "RuleSeverities",
                principalColumn: "RuleSeverityId",
                onDelete: ReferentialAction.Cascade);

            // The defaults above exist only to backfill existing rows to today's behaviour
            // (fired => case). New rows always supply these explicitly.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Service"."Rules" ALTER COLUMN "ActionId" DROP DEFAULT;
                ALTER TABLE "Service"."Rules" ALTER COLUMN "SeverityId" DROP DEFAULT;
                ALTER TABLE "Service"."Rules" ALTER COLUMN "RiskWeight" DROP DEFAULT;
                ALTER TABLE "Service"."RuleVersions" ALTER COLUMN "ActionId" DROP DEFAULT;
                ALTER TABLE "Service"."RuleVersions" ALTER COLUMN "SeverityId" DROP DEFAULT;
                ALTER TABLE "Service"."RuleVersions" ALTER COLUMN "RiskWeight" DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rules_RuleActions_ActionId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropForeignKey(
                name: "FK_Rules_RuleSeverities_SeverityId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleVersions_RuleActions_ActionId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleVersions_RuleSeverities_SeverityId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropTable(
                name: "RuleActions",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "RuleSeverities",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_RuleVersions_ActionId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropIndex(
                name: "IX_RuleVersions_SeverityId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropIndex(
                name: "IX_Rules_ActionId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropIndex(
                name: "IX_Rules_SeverityId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropColumn(
                name: "ActionId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropColumn(
                name: "RiskWeight",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropColumn(
                name: "SeverityId",
                schema: "Service",
                table: "RuleVersions");

            migrationBuilder.DropColumn(
                name: "ReviewThreshold",
                schema: "Service",
                table: "RuleSets");

            migrationBuilder.DropColumn(
                name: "ActionId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropColumn(
                name: "RiskWeight",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropColumn(
                name: "SeverityId",
                schema: "Service",
                table: "Rules");
        }
    }
}
