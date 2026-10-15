using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseDispositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CaseReasonCodeId",
                schema: "Service",
                table: "Cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DispositionId",
                schema: "Service",
                table: "Cases",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReopenedAt",
                schema: "Service",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseDispositions",
                schema: "Service",
                columns: table => new
                {
                    CaseDispositionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseDispositions", x => x.CaseDispositionId);
                });

            migrationBuilder.CreateTable(
                name: "CaseReasonCodes",
                schema: "Service",
                columns: table => new
                {
                    CaseReasonCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DispositionId = table.Column<int>(type: "integer", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReasonCodes", x => x.CaseReasonCodeId);
                    table.ForeignKey(
                        name: "FK_CaseReasonCodes_CaseDispositions_DispositionId",
                        column: x => x.DispositionId,
                        principalSchema: "Service",
                        principalTable: "CaseDispositions",
                        principalColumn: "CaseDispositionId");
                    table.ForeignKey(
                        name: "FK_CaseReasonCodes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 18, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CaseReasonCode", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "CaseDispositions",
                columns: new[] { "CaseDispositionId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "TruePositive", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "FalsePositive", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "CaseStatuses",
                columns: new[] { "CaseStatusId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 6, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "InReview", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 7, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Escalated", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 8, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "PendingInformation", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 9, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "OnHold", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CaseReasonCodeId",
                schema: "Service",
                table: "Cases",
                column: "CaseReasonCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_DispositionId",
                schema: "Service",
                table: "Cases",
                column: "DispositionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReasonCodes_DispositionId",
                schema: "Service",
                table: "CaseReasonCodes",
                column: "DispositionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReasonCodes_OrganizationId_Code",
                schema: "Service",
                table: "CaseReasonCodes",
                columns: new[] { "OrganizationId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_CaseDispositions_DispositionId",
                schema: "Service",
                table: "Cases",
                column: "DispositionId",
                principalSchema: "Service",
                principalTable: "CaseDispositions",
                principalColumn: "CaseDispositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_CaseReasonCodes_CaseReasonCodeId",
                schema: "Service",
                table: "Cases",
                column: "CaseReasonCodeId",
                principalSchema: "Service",
                principalTable: "CaseReasonCodes",
                principalColumn: "CaseReasonCodeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_CaseDispositions_DispositionId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Cases_CaseReasonCodes_CaseReasonCodeId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropTable(
                name: "CaseReasonCodes",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "CaseDispositions",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_Cases_CaseReasonCodeId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_DispositionId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 18);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "CaseStatuses",
                keyColumn: "CaseStatusId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "CaseStatuses",
                keyColumn: "CaseStatusId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "CaseStatuses",
                keyColumn: "CaseStatusId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "CaseStatuses",
                keyColumn: "CaseStatusId",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "CaseReasonCodeId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "DispositionId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ReopenedAt",
                schema: "Service",
                table: "Cases");
        }
    }
}
