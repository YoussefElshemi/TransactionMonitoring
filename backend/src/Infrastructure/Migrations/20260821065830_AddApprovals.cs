using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalPolicies",
                schema: "Service",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequireDeploymentApproval = table.Column<bool>(type: "boolean", nullable: false),
                    CaseDecisionMinimumSeverityId = table.Column<int>(type: "integer", nullable: true),
                    CaseDecisionMatcher = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalPolicies", x => x.OrganizationId);
                    table.ForeignKey(
                        name: "FK_ApprovalPolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalPolicies_RuleSeverities_CaseDecisionMinimumSeverity~",
                        column: x => x.CaseDecisionMinimumSeverityId,
                        principalSchema: "Service",
                        principalTable: "RuleSeverities",
                        principalColumn: "RuleSeverityId");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalStatuses",
                schema: "Service",
                columns: table => new
                {
                    ApprovalStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalStatuses", x => x.ApprovalStatusId);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSubjects",
                schema: "Service",
                columns: table => new
                {
                    ApprovalSubjectId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSubjects", x => x.ApprovalSubjectId);
                });

            migrationBuilder.CreateTable(
                name: "Approvals",
                schema: "Service",
                columns: table => new
                {
                    ApprovalId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    RuleDeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposedStatusId = table.Column<int>(type: "integer", nullable: true),
                    ProposedDispositionId = table.Column<int>(type: "integer", nullable: true),
                    ProposedCaseReasonCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposedNote = table.Column<string>(type: "text", nullable: true),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Approvals", x => x.ApprovalId);
                    table.ForeignKey(
                        name: "FK_Approvals_ApprovalStatuses_StatusId",
                        column: x => x.StatusId,
                        principalSchema: "Service",
                        principalTable: "ApprovalStatuses",
                        principalColumn: "ApprovalStatusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Approvals_ApprovalSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "Service",
                        principalTable: "ApprovalSubjects",
                        principalColumn: "ApprovalSubjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Approvals_CaseDispositions_ProposedDispositionId",
                        column: x => x.ProposedDispositionId,
                        principalSchema: "Service",
                        principalTable: "CaseDispositions",
                        principalColumn: "CaseDispositionId");
                    table.ForeignKey(
                        name: "FK_Approvals_CaseReasonCodes_ProposedCaseReasonCodeId",
                        column: x => x.ProposedCaseReasonCodeId,
                        principalSchema: "Service",
                        principalTable: "CaseReasonCodes",
                        principalColumn: "CaseReasonCodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Approvals_CaseStatuses_ProposedStatusId",
                        column: x => x.ProposedStatusId,
                        principalSchema: "Service",
                        principalTable: "CaseStatuses",
                        principalColumn: "CaseStatusId");
                    table.ForeignKey(
                        name: "FK_Approvals_Cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Service",
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Approvals_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Approvals_RuleDeployments_RuleDeploymentId",
                        column: x => x.RuleDeploymentId,
                        principalSchema: "Service",
                        principalTable: "RuleDeployments",
                        principalColumn: "RuleDeploymentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Approvals_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Approvals_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "ApprovalStatuses",
                columns: new[] { "ApprovalStatusId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Pending", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approved", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Rejected", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Withdrawn", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "ApprovalSubjects",
                columns: new[] { "ApprovalSubjectId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "RuleDeployment", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CaseResolution", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 21, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Approval", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "RuleDeploymentStatuses",
                columns: new[] { "RuleDeploymentStatusId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 5, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "PendingApproval", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalPolicies_CaseDecisionMinimumSeverityId",
                schema: "Service",
                table: "ApprovalPolicies",
                column: "CaseDecisionMinimumSeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_CaseId",
                schema: "Service",
                table: "Approvals",
                column: "CaseId",
                unique: true,
                filter: "\"StatusId\" = 1 AND \"CaseId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_OrganizationId_StatusId_CreatedAt",
                schema: "Service",
                table: "Approvals",
                columns: new[] { "OrganizationId", "StatusId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_ProposedCaseReasonCodeId",
                schema: "Service",
                table: "Approvals",
                column: "ProposedCaseReasonCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_ProposedDispositionId",
                schema: "Service",
                table: "Approvals",
                column: "ProposedDispositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_ProposedStatusId",
                schema: "Service",
                table: "Approvals",
                column: "ProposedStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_RequestedByUserId",
                schema: "Service",
                table: "Approvals",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_ReviewedByUserId",
                schema: "Service",
                table: "Approvals",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_RuleDeploymentId",
                schema: "Service",
                table: "Approvals",
                column: "RuleDeploymentId",
                unique: true,
                filter: "\"StatusId\" = 1 AND \"RuleDeploymentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_StatusId",
                schema: "Service",
                table: "Approvals",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_SubjectId",
                schema: "Service",
                table: "Approvals",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalPolicies",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "Approvals",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "ApprovalStatuses",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "ApprovalSubjects",
                schema: "Service");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 21);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "RuleDeploymentStatuses",
                keyColumn: "RuleDeploymentStatusId",
                keyValue: 5);
        }
    }
}
