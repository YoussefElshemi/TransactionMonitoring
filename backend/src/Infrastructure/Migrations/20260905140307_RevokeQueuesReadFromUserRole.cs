using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RevokeQueuesReadFromUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RoleAssignments_ScopeMatchesType",
                schema: "Identity",
                table: "RoleAssignments");

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 36, new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoleAssignments_ScopeMatchesType",
                schema: "Identity",
                table: "RoleAssignments",
                sql: "(\"ScopeTypeId\" = 1 AND \"SubOrganizationId\" IS NULL AND \"RuleSetId\" IS NULL AND \"QueueId\" IS NULL)\r\nOR (\"ScopeTypeId\" = 2 AND \"SubOrganizationId\" IS NOT NULL AND \"RuleSetId\" IS NULL AND \"QueueId\" IS NULL)\r\nOR (\"ScopeTypeId\" = 3 AND \"SubOrganizationId\" IS NULL AND \"RuleSetId\" IS NOT NULL AND \"QueueId\" IS NULL)\r\nOR (\"ScopeTypeId\" = 4 AND \"SubOrganizationId\" IS NULL AND \"RuleSetId\" IS NULL AND \"QueueId\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RoleAssignments_ScopeMatchesType",
                schema: "Identity",
                table: "RoleAssignments");

            migrationBuilder.InsertData(
                schema: "Identity",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "CreatedAt", "UpdatedAt" },
                values: new object[] { 36, new Guid("00000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoleAssignments_ScopeMatchesType",
                schema: "Identity",
                table: "RoleAssignments",
                sql: "(\"ScopeTypeId\" = 1 AND \"SubOrganizationId\" IS NULL AND \"RuleSetId\" IS NULL AND \"QueueId\" IS NULL)\nOR (\"ScopeTypeId\" = 2 AND \"SubOrganizationId\" IS NOT NULL AND \"RuleSetId\" IS NULL AND \"QueueId\" IS NULL)\nOR (\"ScopeTypeId\" = 3 AND \"SubOrganizationId\" IS NULL AND \"RuleSetId\" IS NOT NULL AND \"QueueId\" IS NULL)\nOR (\"ScopeTypeId\" = 4 AND \"SubOrganizationId\" IS NULL AND \"RuleSetId\" IS NULL AND \"QueueId\" IS NOT NULL)");
        }
    }
}
