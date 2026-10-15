using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys",
                schema: "Identity");

            // Per-organization system users existed only so API-key traffic had a principal to borrow. API clients
            // are principals in their own right, so the rows go — but they are still referenced by role
            // assignments and by any audit rows they wrote, which have to be cleared first.
            migrationBuilder.Sql("""
                DELETE FROM "Identity"."UserRoles"
                 WHERE "UserId" IN (SELECT "Id" FROM "Identity"."Users" WHERE "SystemUser");

                DELETE FROM "Identity"."UserClaims"
                 WHERE "UserId" IN (SELECT "Id" FROM "Identity"."Users" WHERE "SystemUser");

                DELETE FROM "Identity"."UserLogins"
                 WHERE "UserId" IN (SELECT "Id" FROM "Identity"."Users" WHERE "SystemUser");

                DELETE FROM "Identity"."UserTokens"
                 WHERE "UserId" IN (SELECT "Id" FROM "Identity"."Users" WHERE "SystemUser");

                UPDATE "Service"."AuditLogs" SET "ChangedByUserId" = NULL
                 WHERE "ChangedByUserId" IN (SELECT "Id" FROM "Identity"."Users" WHERE "SystemUser");

                UPDATE "Service"."Cases" SET "AssignedToUserId" = NULL, "AssignedAt" = NULL
                 WHERE "AssignedToUserId" IN (SELECT "Id" FROM "Identity"."Users" WHERE "SystemUser");

                DELETE FROM "Identity"."Users" WHERE "SystemUser";
                """);

            migrationBuilder.DropColumn(
                name: "SystemUser",
                schema: "Identity",
                table: "Users");

            migrationBuilder.AddColumn<Guid>(
                name: "ChangedByApiClientId",
                schema: "Service",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApiClients",
                schema: "Identity",
                columns: table => new
                {
                    ApiClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    FriendlyName = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiClients", x => x.ApiClientId);
                    table.ForeignKey(
                        name: "FK_ApiClients_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApiClients_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiScopes",
                schema: "Identity",
                columns: table => new
                {
                    ApiScopeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiScopes", x => x.ApiScopeId);
                });

            migrationBuilder.CreateTable(
                name: "ApiClientSecrets",
                schema: "Identity",
                columns: table => new
                {
                    ApiClientSecretId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecretHash = table.Column<string>(type: "text", nullable: false),
                    SecretPrefix = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiClientSecrets", x => x.ApiClientSecretId);
                    table.ForeignKey(
                        name: "FK_ApiClientSecrets_ApiClients_ApiClientId",
                        column: x => x.ApiClientId,
                        principalSchema: "Identity",
                        principalTable: "ApiClients",
                        principalColumn: "ApiClientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApiClientSecrets_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiClientScopes",
                schema: "Identity",
                columns: table => new
                {
                    ApiClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiScopeId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiClientScopes", x => new { x.ApiClientId, x.ApiScopeId });
                    table.ForeignKey(
                        name: "FK_ApiClientScopes_ApiClients_ApiClientId",
                        column: x => x.ApiClientId,
                        principalSchema: "Identity",
                        principalTable: "ApiClients",
                        principalColumn: "ApiClientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApiClientScopes_ApiScopes_ApiScopeId",
                        column: x => x.ApiScopeId,
                        principalSchema: "Identity",
                        principalTable: "ApiScopes",
                        principalColumn: "ApiScopeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Identity",
                table: "ApiScopes",
                columns: new[] { "ApiScopeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "rules:evaluate", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "rulesets:evaluate", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 9, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ApiClient", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 10, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ApiClientSecret", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_ClientId",
                schema: "Identity",
                table: "ApiClients",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_CreatedByUserId",
                schema: "Identity",
                table: "ApiClients",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClients_OrganizationId",
                schema: "Identity",
                table: "ApiClients",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClientScopes_ApiScopeId",
                schema: "Identity",
                table: "ApiClientScopes",
                column: "ApiScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClientSecrets_ApiClientId",
                schema: "Identity",
                table: "ApiClientSecrets",
                column: "ApiClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiClientSecrets_CreatedByUserId",
                schema: "Identity",
                table: "ApiClientSecrets",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiClientScopes",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "ApiClientSecrets",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "ApiScopes",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "ApiClients",
                schema: "Identity");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 10);

            migrationBuilder.DropColumn(
                name: "ChangedByApiClientId",
                schema: "Service",
                table: "AuditLogs");

            migrationBuilder.AddColumn<bool>(
                name: "SystemUser",
                schema: "Identity",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                schema: "Identity",
                columns: table => new
                {
                    ApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FriendlyName = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.ApiKeyId);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_CreatedByUserId",
                schema: "Identity",
                table: "ApiKeys",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_Key",
                schema: "Identity",
                table: "ApiKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_OrganizationId",
                schema: "Identity",
                table: "ApiKeys",
                column: "OrganizationId");
        }
    }
}
