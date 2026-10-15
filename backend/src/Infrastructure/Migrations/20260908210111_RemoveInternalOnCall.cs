using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInternalOnCall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Users_AcknowledgedByUserId",
                schema: "Service",
                table: "Incidents");

            migrationBuilder.DropTable(
                name: "OnCallShifts",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_AcknowledgedByUserId",
                schema: "Service",
                table: "Incidents");

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 98, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 106, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 107, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 98);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 106);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 107);

            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                schema: "Service",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "AcknowledgedByUserId",
                schema: "Service",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "EscalatedToLevel",
                schema: "Service",
                table: "Incidents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcknowledgedAt",
                schema: "Service",
                table: "Incidents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcknowledgedByUserId",
                schema: "Service",
                table: "Incidents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscalatedToLevel",
                schema: "Service",
                table: "Incidents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OnCallShifts",
                schema: "Service",
                columns: table => new
                {
                    OnCallShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EscalationLevel = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ShiftEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ShiftStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnCallShifts", x => x.OnCallShiftId);
                    table.ForeignKey(
                        name: "FK_OnCallShifts_ServiceComponents_ComponentId",
                        column: x => x.ComponentId,
                        principalSchema: "Service",
                        principalTable: "ServiceComponents",
                        principalColumn: "ServiceComponentId");
                    table.ForeignKey(
                        name: "FK_OnCallShifts_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnCallShifts_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 98, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "incidents:acknowledge", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 106, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "on-call:read", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 107, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "on-call:write", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Identity",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { 98, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 106, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 107, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_AcknowledgedByUserId",
                schema: "Service",
                table: "Incidents",
                column: "AcknowledgedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OnCallShifts_ComponentId",
                schema: "Service",
                table: "OnCallShifts",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_OnCallShifts_CreatedByUserId",
                schema: "Service",
                table: "OnCallShifts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OnCallShifts_ShiftStart_ShiftEnd_EscalationLevel",
                schema: "Service",
                table: "OnCallShifts",
                columns: new[] { "ShiftStart", "ShiftEnd", "EscalationLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_OnCallShifts_UserId",
                schema: "Service",
                table: "OnCallShifts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Users_AcknowledgedByUserId",
                schema: "Service",
                table: "Incidents",
                column: "AcknowledgedByUserId",
                principalSchema: "Identity",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
