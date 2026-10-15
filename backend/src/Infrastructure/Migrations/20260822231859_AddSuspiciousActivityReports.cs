using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSuspiciousActivityReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "SuspiciousActivityReportNumberSequence",
                schema: "Service");

            migrationBuilder.AddColumn<Guid>(
                name: "SuspiciousActivityReportId",
                schema: "Service",
                table: "Approvals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SuspiciousActivityReportPolicies",
                schema: "Service",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportingEntityId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FilingDeadlineDays = table.Column<int>(type: "integer", nullable: true),
                    RequireFilingApproval = table.Column<bool>(type: "boolean", nullable: false),
                    AmountPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CurrencyPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspiciousActivityReportPolicies", x => x.OrganizationId);
                    table.ForeignKey(
                        name: "FK_SuspiciousActivityReportPolicies_Organizations_Organization~",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuspiciousActivityReportStatuses",
                schema: "Service",
                columns: table => new
                {
                    SuspiciousActivityReportStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspiciousActivityReportStatuses", x => x.SuspiciousActivityReportStatusId);
                });

            migrationBuilder.CreateTable(
                name: "SuspiciousActivityReports",
                schema: "Service",
                columns: table => new
                {
                    SuspiciousActivityReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FriendlyName = table.Column<string>(type: "text", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    Narrative = table.Column<string>(type: "character varying(32768)", maxLength: 32768, nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OverdueNotifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OpenedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FiledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FilingReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcknowledgementReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NotFiledReason = table.Column<string>(type: "text", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspiciousActivityReports", x => x.SuspiciousActivityReportId);
                    table.ForeignKey(
                        name: "FK_SuspiciousActivityReports_Cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Service",
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuspiciousActivityReports_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuspiciousActivityReports_SuspiciousActivityReportStatuses_~",
                        column: x => x.StatusId,
                        principalSchema: "Service",
                        principalTable: "SuspiciousActivityReportStatuses",
                        principalColumn: "SuspiciousActivityReportStatusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuspiciousActivityReports_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SuspiciousActivityReports_Users_FiledByUserId",
                        column: x => x.FiledByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SuspiciousActivityReports_Users_OpenedByUserId",
                        column: x => x.OpenedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "ApprovalSubjects",
                columns: new[] { "ApprovalSubjectId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SuspiciousActivityReport", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 25, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SuspiciousActivityReport", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "SuspiciousActivityReportStatuses",
                columns: new[] { "SuspiciousActivityReportStatusId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Draft", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "PendingApproval", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Filed", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Acknowledged", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "NotFiled", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_SuspiciousActivityReportId",
                schema: "Service",
                table: "Approvals",
                column: "SuspiciousActivityReportId",
                unique: true,
                filter: "\"StatusId\" = 1 AND \"SuspiciousActivityReportId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivityReports_CaseId",
                schema: "Service",
                table: "SuspiciousActivityReports",
                column: "CaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivityReports_DecidedByUserId",
                schema: "Service",
                table: "SuspiciousActivityReports",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivityReports_DueAt",
                schema: "Service",
                table: "SuspiciousActivityReports",
                column: "DueAt",
                filter: "\"OverdueNotifiedAt\" IS NULL AND \"StatusId\" NOT IN (4, 5, 6)");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivityReports_FiledByUserId",
                schema: "Service",
                table: "SuspiciousActivityReports",
                column: "FiledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivityReports_FriendlyName",
                schema: "Service",
                table: "SuspiciousActivityReports",
                column: "FriendlyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivityReports_OpenedByUserId",
                schema: "Service",
                table: "SuspiciousActivityReports",
                column: "OpenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivityReports_OrganizationId_StatusId_DueAt",
                schema: "Service",
                table: "SuspiciousActivityReports",
                columns: new[] { "OrganizationId", "StatusId", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivityReports_StatusId",
                schema: "Service",
                table: "SuspiciousActivityReports",
                column: "StatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_SuspiciousActivityReports_SuspiciousActivityRepor~",
                schema: "Service",
                table: "Approvals",
                column: "SuspiciousActivityReportId",
                principalSchema: "Service",
                principalTable: "SuspiciousActivityReports",
                principalColumn: "SuspiciousActivityReportId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_SuspiciousActivityReports_SuspiciousActivityRepor~",
                schema: "Service",
                table: "Approvals");

            migrationBuilder.DropTable(
                name: "SuspiciousActivityReportPolicies",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "SuspiciousActivityReports",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "SuspiciousActivityReportStatuses",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_Approvals_SuspiciousActivityReportId",
                schema: "Service",
                table: "Approvals");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "ApprovalSubjects",
                keyColumn: "ApprovalSubjectId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 25);

            migrationBuilder.DropColumn(
                name: "SuspiciousActivityReportId",
                schema: "Service",
                table: "Approvals");

            migrationBuilder.DropSequence(
                name: "SuspiciousActivityReportNumberSequence",
                schema: "Service");
        }
    }
}
