using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHoldExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Decisions_StatusId",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.AddColumn<int>(
                name: "HoldExpiryVerdictId",
                schema: "Service",
                table: "Rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HoldWindow",
                schema: "Service",
                table: "Rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HoldExpiryVerdictId",
                schema: "Service",
                table: "Organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HoldWindow",
                schema: "Service",
                table: "Organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                schema: "Service",
                table: "Decisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HoldExpiryVerdictId",
                schema: "Service",
                table: "Decisions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HoldExpiryVerdicts",
                schema: "Service",
                columns: table => new
                {
                    HoldExpiryVerdictId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoldExpiryVerdicts", x => x.HoldExpiryVerdictId);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "CaseStatuses",
                columns: new[] { "CaseStatusId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 5, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Expired", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "HoldExpiryVerdicts",
                columns: new[] { "HoldExpiryVerdictId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Release", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Expire", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Block", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                schema: "Service",
                table: "Organizations",
                keyColumn: "OrganizationId",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "HoldExpiryVerdictId", "HoldWindow" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Rules_HoldExpiryVerdictId",
                schema: "Service",
                table: "Rules",
                column: "HoldExpiryVerdictId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_HoldExpiryVerdictId",
                schema: "Service",
                table: "Organizations",
                column: "HoldExpiryVerdictId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_HoldExpiryVerdictId",
                schema: "Service",
                table: "Decisions",
                column: "HoldExpiryVerdictId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_StatusId_ExpiresAt",
                schema: "Service",
                table: "Decisions",
                columns: new[] { "StatusId", "ExpiresAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Decisions_HoldExpiryVerdicts_HoldExpiryVerdictId",
                schema: "Service",
                table: "Decisions",
                column: "HoldExpiryVerdictId",
                principalSchema: "Service",
                principalTable: "HoldExpiryVerdicts",
                principalColumn: "HoldExpiryVerdictId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_HoldExpiryVerdicts_HoldExpiryVerdictId",
                schema: "Service",
                table: "Organizations",
                column: "HoldExpiryVerdictId",
                principalSchema: "Service",
                principalTable: "HoldExpiryVerdicts",
                principalColumn: "HoldExpiryVerdictId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_HoldExpiryVerdicts_HoldExpiryVerdictId",
                schema: "Service",
                table: "Rules",
                column: "HoldExpiryVerdictId",
                principalSchema: "Service",
                principalTable: "HoldExpiryVerdicts",
                principalColumn: "HoldExpiryVerdictId");

            // Decisions held before this migration have no deadline and would hang forever. Give them the
            // shipped default of a 24 hour window from when they were held, blocking at expiry.
            migrationBuilder.Sql(
                "UPDATE \"Service\".\"Decisions\" SET \"ExpiresAt\" = \"HeldAt\" + INTERVAL '1440 minutes', \"HoldExpiryVerdictId\" = 3 WHERE \"StatusId\" = 2 AND \"HeldAt\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Decisions_HoldExpiryVerdicts_HoldExpiryVerdictId",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_HoldExpiryVerdicts_HoldExpiryVerdictId",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Rules_HoldExpiryVerdicts_HoldExpiryVerdictId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropTable(
                name: "HoldExpiryVerdicts",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_Rules_HoldExpiryVerdictId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_HoldExpiryVerdictId",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_HoldExpiryVerdictId",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Decisions_StatusId_ExpiresAt",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "CaseStatuses",
                keyColumn: "CaseStatusId",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "HoldExpiryVerdictId",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropColumn(
                name: "HoldWindow",
                schema: "Service",
                table: "Rules");

            migrationBuilder.DropColumn(
                name: "HoldExpiryVerdictId",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "HoldWindow",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.DropColumn(
                name: "HoldExpiryVerdictId",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_StatusId",
                schema: "Service",
                table: "Decisions",
                column: "StatusId");
        }
    }
}
