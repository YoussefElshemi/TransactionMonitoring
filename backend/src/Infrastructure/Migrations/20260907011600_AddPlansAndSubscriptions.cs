using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlansAndSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlanVersionId",
                schema: "Service",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Plans",
                schema: "Service",
                columns: table => new
                {
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentVersionNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.PlanId);
                    table.ForeignKey(
                        name: "FK_Plans_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId");
                });

            migrationBuilder.CreateTable(
                name: "PlanVersions",
                schema: "Service",
                columns: table => new
                {
                    PlanVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    CostPerRequest = table.Column<decimal>(type: "numeric", nullable: false),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanVersions", x => x.PlanVersionId);
                    table.ForeignKey(
                        name: "FK_PlanVersions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "Service",
                        principalTable: "Plans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanVersions_Users_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                schema: "Service",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_PlanVersions_PlanVersionId",
                        column: x => x.PlanVersionId,
                        principalSchema: "Service",
                        principalTable: "PlanVersions",
                        principalColumn: "PlanVersionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 30, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Plan", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 31, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Subscription", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 82, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "plans:read", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 83, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "plans:write", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 84, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "subscriptions:read", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 85, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "subscriptions:write", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Identity",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { 82, new Guid("00000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 84, new Guid("00000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 82, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 83, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 84, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 85, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 82, new Guid("00000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 84, new Guid("00000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            // Every organisation's rate becomes a bespoke plan of its own, published as version 1 and
            // subscribed from the day the organisation was created. The commercial reality is preserved
            // exactly — nobody's price changes — and what is gained is a record of what it is.
            //
            // Existing invoices are deliberately left with a null PlanVersionId. They were priced off the
            // column rather than off a plan, and pointing them at a version invented today would claim
            // they were derived from an agreement that did not exist when they were issued. The rate they
            // were issued at is already snapshotted on the invoice itself.
            migrationBuilder.Sql("""
                INSERT INTO "Service"."Plans"
                    ("PlanId", "Name", "Description", "OrganizationId", "IsEnabled", "CurrentVersionNumber",
                     "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
                SELECT
                    gen_random_uuid(),
                    organization."FriendlyName",
                    'Bespoke plan migrated from the per-organization rate that preceded the plan catalogue.',
                    organization."OrganizationId",
                    true,
                    1,
                    organization."CreatedAt",
                    now(),
                    false,
                    NULL
                FROM "Service"."Organizations" organization;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "Service"."PlanVersions"
                    ("PlanVersionId", "PlanId", "VersionNumber", "CostPerRequest", "PublishedByUserId", "CreatedAt")
                SELECT
                    gen_random_uuid(),
                    plan."PlanId",
                    1,
                    organization."CostPerRequest",
                    NULL,
                    plan."CreatedAt"
                FROM "Service"."Plans" plan
                JOIN "Service"."Organizations" organization
                    ON organization."OrganizationId" = plan."OrganizationId";
                """);

            // Open-ended and starting from the organisation's own creation, so every month that has ever
            // been invoiced is covered by the timeline and can be re-derived rather than falling into the
            // gap before plans existed.
            migrationBuilder.Sql("""
                INSERT INTO "Service"."Subscriptions"
                    ("SubscriptionId", "OrganizationId", "PlanVersionId", "EffectiveFrom", "EffectiveTo",
                     "CreatedByUserId", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
                SELECT
                    gen_random_uuid(),
                    organization."OrganizationId",
                    planVersion."PlanVersionId",
                    organization."CreatedAt",
                    NULL,
                    NULL,
                    organization."CreatedAt",
                    now(),
                    false,
                    NULL
                FROM "Service"."Organizations" organization
                JOIN "Service"."Plans" plan
                    ON plan."OrganizationId" = organization."OrganizationId"
                JOIN "Service"."PlanVersions" planVersion
                    ON planVersion."PlanId" = plan."PlanId" AND planVersion."VersionNumber" = 1;
                """);

            migrationBuilder.DropColumn(
                name: "CostPerRequest",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PlanVersionId",
                schema: "Service",
                table: "Invoices",
                column: "PlanVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_OrganizationId_Name",
                schema: "Service",
                table: "Plans",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_PlanId_VersionNumber",
                schema: "Service",
                table: "PlanVersions",
                columns: new[] { "PlanId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_PublishedByUserId",
                schema: "Service",
                table: "PlanVersions",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CreatedByUserId",
                schema: "Service",
                table: "Subscriptions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_OrganizationId_EffectiveFrom",
                schema: "Service",
                table: "Subscriptions",
                columns: new[] { "OrganizationId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanVersionId",
                schema: "Service",
                table: "Subscriptions",
                column: "PlanVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_PlanVersions_PlanVersionId",
                schema: "Service",
                table: "Invoices",
                column: "PlanVersionId",
                principalSchema: "Service",
                principalTable: "PlanVersions",
                principalColumn: "PlanVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_PlanVersions_PlanVersionId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.AddColumn<decimal>(
                name: "CostPerRequest",
                schema: "Service",
                table: "Organizations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            // Back to a column. Whatever each organisation is on right now is the rate it had before, so
            // reversing loses the history rather than the price.
            migrationBuilder.Sql("""
                UPDATE "Service"."Organizations" organization
                SET "CostPerRequest" = planVersion."CostPerRequest"
                FROM "Service"."Subscriptions" subscription
                JOIN "Service"."PlanVersions" planVersion
                    ON planVersion."PlanVersionId" = subscription."PlanVersionId"
                WHERE subscription."OrganizationId" = organization."OrganizationId"
                  AND subscription."IsDeleted" = false
                  AND subscription."EffectiveTo" IS NULL;
                """);

            migrationBuilder.DropTable(
                name: "Subscriptions",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "PlanVersions",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "Plans",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PlanVersionId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 30);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 31);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 82, new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 84, new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 82, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 83, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 84, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 85, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 82, new Guid("00000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 84, new Guid("00000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 82);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 83);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 84);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 85);

            migrationBuilder.DropColumn(
                name: "PlanVersionId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.UpdateData(
                schema: "Service",
                table: "Organizations",
                keyColumn: "OrganizationId",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "CostPerRequest",
                value: 0m);
        }
    }
}
