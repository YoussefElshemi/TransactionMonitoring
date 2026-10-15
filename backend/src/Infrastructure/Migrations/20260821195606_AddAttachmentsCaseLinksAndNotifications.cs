using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentsCaseLinksAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EndpointUrl",
                schema: "Service",
                table: "OutboxMessages",
                newName: "Destination");

            migrationBuilder.AlterColumn<string>(
                name: "SecretKey",
                schema: "Service",
                table: "OutboxMessages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            // Defaulted to 1 rather than 0. Every row already in the outbox was written before channels
            // existed and is a webhook, and NotificationChannel.Webhook is 1 — a default of 0 is not a
            // channel at all and would fail the foreign key added below on any non-empty queue.
            migrationBuilder.AddColumn<int>(
                name: "ChannelId",
                schema: "Service",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BreachNotifiedAt",
                schema: "Service",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttachmentScanStatuses",
                schema: "Service",
                columns: table => new
                {
                    AttachmentScanStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentScanStatuses", x => x.AttachmentScanStatusId);
                });

            migrationBuilder.CreateTable(
                name: "CaseLinks",
                schema: "Service",
                columns: table => new
                {
                    CaseLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseLinks", x => x.CaseLinkId);
                    table.ForeignKey(
                        name: "FK_CaseLinks_Cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Service",
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseLinks_Cases_LinkedCaseId",
                        column: x => x.LinkedCaseId,
                        principalSchema: "Service",
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseLinks_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseLinks_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationChannels",
                schema: "Service",
                columns: table => new
                {
                    NotificationChannelId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationChannels", x => x.NotificationChannelId);
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                schema: "Service",
                columns: table => new
                {
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    ByteCount = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "text", nullable: false),
                    Checksum = table.Column<string>(type: "text", nullable: false),
                    ScanStatusId = table.Column<int>(type: "integer", nullable: false),
                    ScannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ThreatName = table.Column<string>(type: "text", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.AttachmentId);
                    table.ForeignKey(
                        name: "FK_Attachments_AttachmentScanStatuses_ScanStatusId",
                        column: x => x.ScanStatusId,
                        principalSchema: "Service",
                        principalTable: "AttachmentScanStatuses",
                        principalColumn: "AttachmentScanStatusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_Cases_CaseId",
                        column: x => x.CaseId,
                        principalSchema: "Service",
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSubscriptions",
                schema: "Service",
                columns: table => new
                {
                    NotificationSubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<int>(type: "integer", nullable: false),
                    Destination = table.Column<string>(type: "text", nullable: false),
                    MinimumSeverityId = table.Column<int>(type: "integer", nullable: true),
                    NotifyOnBreach = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSubscriptions", x => x.NotificationSubscriptionId);
                    table.ForeignKey(
                        name: "FK_NotificationSubscriptions_NotificationChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalSchema: "Service",
                        principalTable: "NotificationChannels",
                        principalColumn: "NotificationChannelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationSubscriptions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationSubscriptions_RuleSeverities_MinimumSeverityId",
                        column: x => x.MinimumSeverityId,
                        principalSchema: "Service",
                        principalTable: "RuleSeverities",
                        principalColumn: "RuleSeverityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AttachmentScanStatuses",
                columns: new[] { "AttachmentScanStatusId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Pending", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Clean", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Infected", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Failed", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 22, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Attachment", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 23, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CaseLink", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 24, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "NotificationSubscription", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "NotificationChannels",
                columns: new[] { "NotificationChannelId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Webhook", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Email", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Chat", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ChannelId",
                schema: "Service",
                table: "OutboxMessages",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CaseId_IsDeleted",
                schema: "Service",
                table: "Attachments",
                columns: new[] { "CaseId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_OrganizationId",
                schema: "Service",
                table: "Attachments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_ScanStatusId",
                schema: "Service",
                table: "Attachments",
                column: "ScanStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_UploadedByUserId",
                schema: "Service",
                table: "Attachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseLinks_CaseId_IsDeleted",
                schema: "Service",
                table: "CaseLinks",
                columns: new[] { "CaseId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseLinks_CreatedByUserId",
                schema: "Service",
                table: "CaseLinks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseLinks_LinkedCaseId_IsDeleted",
                schema: "Service",
                table: "CaseLinks",
                columns: new[] { "LinkedCaseId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseLinks_OrganizationId",
                schema: "Service",
                table: "CaseLinks",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSubscriptions_ChannelId",
                schema: "Service",
                table: "NotificationSubscriptions",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSubscriptions_MinimumSeverityId",
                schema: "Service",
                table: "NotificationSubscriptions",
                column: "MinimumSeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSubscriptions_OrganizationId_IsEnabled_IsDeleted",
                schema: "Service",
                table: "NotificationSubscriptions",
                columns: new[] { "OrganizationId", "IsEnabled", "IsDeleted" });

            migrationBuilder.AddForeignKey(
                name: "FK_OutboxMessages_NotificationChannels_ChannelId",
                schema: "Service",
                table: "OutboxMessages",
                column: "ChannelId",
                principalSchema: "Service",
                principalTable: "NotificationChannels",
                principalColumn: "NotificationChannelId",
                onDelete: ReferentialAction.Cascade);

            // Every case already past its deadline is stamped as though it had been notified. The sweep
            // only ever considers cases carrying no stamp, so without this the first tick after deploying
            // would email somebody about every breach in the installation's history at once — which is
            // not news, and is the surest way to have the notifications turned straight back off.
            migrationBuilder.Sql(
                """
                UPDATE "Service"."Cases"
                SET "BreachNotifiedAt" = NOW()
                WHERE "BreachNotifiedAt" IS NULL AND "DueAt" < NOW();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboxMessages_NotificationChannels_ChannelId",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "Attachments",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "CaseLinks",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "NotificationSubscriptions",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "AttachmentScanStatuses",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "NotificationChannels",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ChannelId",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 22);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 23);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 24);

            migrationBuilder.DropColumn(
                name: "ChannelId",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "BreachNotifiedAt",
                schema: "Service",
                table: "Cases");

            migrationBuilder.RenameColumn(
                name: "Destination",
                schema: "Service",
                table: "OutboxMessages",
                newName: "EndpointUrl");

            migrationBuilder.AlterColumn<string>(
                name: "SecretKey",
                schema: "Service",
                table: "OutboxMessages",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
