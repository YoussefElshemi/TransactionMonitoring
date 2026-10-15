using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionSchemas",
                schema: "Service",
                columns: table => new
                {
                    TransactionSchemaId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fields = table.Column<string>(type: "text", nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentVersionNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionSchemas", x => x.TransactionSchemaId);
                    table.ForeignKey(
                        name: "FK_TransactionSchemas_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionSchemas_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionSchemaVersions",
                schema: "Service",
                columns: table => new
                {
                    TransactionSchemaVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionSchemaId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Fields = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionSchemaVersions", x => x.TransactionSchemaVersionId);
                    table.ForeignKey(
                        name: "FK_TransactionSchemaVersions_TransactionSchemas_TransactionSch~",
                        column: x => x.TransactionSchemaId,
                        principalSchema: "Service",
                        principalTable: "TransactionSchemas",
                        principalColumn: "TransactionSchemaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 13, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "TransactionSchema", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSchemas_CreatedByUserId",
                schema: "Service",
                table: "TransactionSchemas",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSchemas_OrganizationId",
                schema: "Service",
                table: "TransactionSchemas",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSchemaVersions_TransactionSchemaId_VersionNumber",
                schema: "Service",
                table: "TransactionSchemaVersions",
                columns: new[] { "TransactionSchemaId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionSchemaVersions",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "TransactionSchemas",
                schema: "Service");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 13);
        }
    }
}
