using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueuesAndRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "QueueId",
                schema: "Service",
                table: "Cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QueueRoutingPolicies",
                schema: "Service",
                columns: table => new
                {
                    QueueRoutingPolicyId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueRoutingPolicies", x => x.QueueRoutingPolicyId);
                });

            migrationBuilder.CreateTable(
                name: "Queues",
                schema: "Service",
                columns: table => new
                {
                    QueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RoutingPolicyId = table.Column<int>(type: "integer", nullable: false),
                    MinimumSeverityId = table.Column<int>(type: "integer", nullable: true),
                    Matcher = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Queues", x => x.QueueId);
                    table.ForeignKey(
                        name: "FK_Queues_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Queues_QueueRoutingPolicies_RoutingPolicyId",
                        column: x => x.RoutingPolicyId,
                        principalSchema: "Service",
                        principalTable: "QueueRoutingPolicies",
                        principalColumn: "QueueRoutingPolicyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Queues_RuleSeverities_MinimumSeverityId",
                        column: x => x.MinimumSeverityId,
                        principalSchema: "Service",
                        principalTable: "RuleSeverities",
                        principalColumn: "RuleSeverityId");
                });

            migrationBuilder.CreateTable(
                name: "QueueMembers",
                schema: "Service",
                columns: table => new
                {
                    QueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueMembers", x => new { x.QueueId, x.UserId });
                    table.ForeignKey(
                        name: "FK_QueueMembers_Queues_QueueId",
                        column: x => x.QueueId,
                        principalSchema: "Service",
                        principalTable: "Queues",
                        principalColumn: "QueueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueueMembers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QueueRules",
                schema: "Service",
                columns: table => new
                {
                    QueueId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueRules", x => new { x.QueueId, x.RuleId });
                    table.ForeignKey(
                        name: "FK_QueueRules_Queues_QueueId",
                        column: x => x.QueueId,
                        principalSchema: "Service",
                        principalTable: "Queues",
                        principalColumn: "QueueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QueueRules_Rules_RuleId",
                        column: x => x.RuleId,
                        principalSchema: "Service",
                        principalTable: "Rules",
                        principalColumn: "RuleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 19, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Queue", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "QueueRoutingPolicies",
                columns: new[] { "QueueRoutingPolicyId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Manual", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "RoundRobin", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "LeastLoaded", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_QueueId_StatusId_AssignedToUserId",
                schema: "Service",
                table: "Cases",
                columns: new[] { "QueueId", "StatusId", "AssignedToUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_QueueMembers_UserId",
                schema: "Service",
                table: "QueueMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueRules_RuleId",
                schema: "Service",
                table: "QueueRules",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_MinimumSeverityId",
                schema: "Service",
                table: "Queues",
                column: "MinimumSeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_OrganizationId_IsEnabled_Priority",
                schema: "Service",
                table: "Queues",
                columns: new[] { "OrganizationId", "IsEnabled", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Queues_OrganizationId_Name",
                schema: "Service",
                table: "Queues",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Queues_RoutingPolicyId",
                schema: "Service",
                table: "Queues",
                column: "RoutingPolicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Queues_QueueId",
                schema: "Service",
                table: "Cases",
                column: "QueueId",
                principalSchema: "Service",
                principalTable: "Queues",
                principalColumn: "QueueId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Queues_QueueId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropTable(
                name: "QueueMembers",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "QueueRules",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "Queues",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "QueueRoutingPolicies",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_Cases_QueueId_StatusId_AssignedToUserId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 19);

            migrationBuilder.DropColumn(
                name: "QueueId",
                schema: "Service",
                table: "Cases");
        }
    }
}
