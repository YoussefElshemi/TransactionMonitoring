using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationAlertPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CaseGroupingEnabled",
                schema: "Service",
                table: "Organizations",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaseGroupingScopeId",
                schema: "Service",
                table: "Organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaseGroupingSeverityPolicyId",
                schema: "Service",
                table: "Organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeduplicationEnabled",
                schema: "Service",
                table: "Organizations",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAlertsPerCase",
                schema: "Service",
                table: "Organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseGroupingScopes",
                schema: "Service",
                columns: table => new
                {
                    CaseGroupingScopeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseGroupingScopes", x => x.CaseGroupingScopeId);
                });

            migrationBuilder.CreateTable(
                name: "CaseGroupingSeverityPolicies",
                schema: "Service",
                columns: table => new
                {
                    CaseGroupingSeverityPolicyId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseGroupingSeverityPolicies", x => x.CaseGroupingSeverityPolicyId);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "CaseGroupingScopes",
                columns: new[] { "CaseGroupingScopeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Party", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "PartyAndRule", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "CaseGroupingSeverityPolicies",
                columns: new[] { "CaseGroupingSeverityPolicyId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Any", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AtLeastAsSevere", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SameSeverity", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.UpdateData(
                schema: "Service",
                table: "Organizations",
                keyColumn: "OrganizationId",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CaseGroupingEnabled", "CaseGroupingScopeId", "CaseGroupingSeverityPolicyId", "DeduplicationEnabled", "MaxAlertsPerCase" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CaseGroupingScopeId",
                schema: "Service",
                table: "Organizations",
                column: "CaseGroupingScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CaseGroupingSeverityPolicyId",
                schema: "Service",
                table: "Organizations",
                column: "CaseGroupingSeverityPolicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_CaseGroupingScopes_CaseGroupingScopeId",
                schema: "Service",
                table: "Organizations",
                column: "CaseGroupingScopeId",
                principalSchema: "Service",
                principalTable: "CaseGroupingScopes",
                principalColumn: "CaseGroupingScopeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_CaseGroupingSeverityPolicies_CaseGroupingSeve~",
                schema: "Service",
                table: "Organizations",
                column: "CaseGroupingSeverityPolicyId",
                principalSchema: "Service",
                principalTable: "CaseGroupingSeverityPolicies",
                principalColumn: "CaseGroupingSeverityPolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_CaseGroupingScopes_CaseGroupingScopeId",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_CaseGroupingSeverityPolicies_CaseGroupingSeve~",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropTable(
                name: "CaseGroupingScopes",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "CaseGroupingSeverityPolicies",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_CaseGroupingScopeId",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_CaseGroupingSeverityPolicyId",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CaseGroupingEnabled",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CaseGroupingScopeId",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CaseGroupingSeverityPolicyId",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DeduplicationEnabled",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "MaxAlertsPerCase",
                schema: "Service",
                table: "Organizations");
        }
    }
}
