using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookSubscriptionsAndDeliveryLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailures",
                schema: "Service",
                table: "WebhookConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisabledAt",
                schema: "Service",
                table: "WebhookConfigurations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisabledReason",
                schema: "Service",
                table: "WebhookConfigurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastFailedAt",
                schema: "Service",
                table: "WebhookConfigurations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSucceededAt",
                schema: "Service",
                table: "WebhookConfigurations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayloadVersion",
                schema: "Service",
                table: "WebhookConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Event",
                schema: "Service",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAttemptedAt",
                schema: "Service",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Latency",
                schema: "Service",
                table: "OutboxMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayloadVersion",
                schema: "Service",
                table: "OutboxMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplayOfOutboxMessageId",
                schema: "Service",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseBody",
                schema: "Service",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseStatusCode",
                schema: "Service",
                table: "OutboxMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WebhookConfigurationId",
                schema: "Service",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WebhookConfigurationEvents",
                schema: "Service",
                columns: table => new
                {
                    WebhookConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Event = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookConfigurationEvents", x => new { x.WebhookConfigurationId, x.Event });
                    table.ForeignKey(
                        name: "FK_WebhookConfigurationEvents_WebhookConfigurations_WebhookCon~",
                        column: x => x.WebhookConfigurationId,
                        principalSchema: "Service",
                        principalTable: "WebhookConfigurations",
                        principalColumn: "WebhookConfigurationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_WebhookConfigurationId_CreatedAt",
                schema: "Service",
                table: "OutboxMessages",
                columns: new[] { "WebhookConfigurationId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_OutboxMessages_WebhookConfigurations_WebhookConfigurationId",
                schema: "Service",
                table: "OutboxMessages",
                column: "WebhookConfigurationId",
                principalSchema: "Service",
                principalTable: "WebhookConfigurations",
                principalColumn: "WebhookConfigurationId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboxMessages_WebhookConfigurations_WebhookConfigurationId",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "WebhookConfigurationEvents",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_WebhookConfigurationId_CreatedAt",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ConsecutiveFailures",
                schema: "Service",
                table: "WebhookConfigurations");

            migrationBuilder.DropColumn(
                name: "DisabledAt",
                schema: "Service",
                table: "WebhookConfigurations");

            migrationBuilder.DropColumn(
                name: "DisabledReason",
                schema: "Service",
                table: "WebhookConfigurations");

            migrationBuilder.DropColumn(
                name: "LastFailedAt",
                schema: "Service",
                table: "WebhookConfigurations");

            migrationBuilder.DropColumn(
                name: "LastSucceededAt",
                schema: "Service",
                table: "WebhookConfigurations");

            migrationBuilder.DropColumn(
                name: "PayloadVersion",
                schema: "Service",
                table: "WebhookConfigurations");

            migrationBuilder.DropColumn(
                name: "Event",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LastAttemptedAt",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Latency",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "PayloadVersion",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ReplayOfOutboxMessageId",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ResponseBody",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ResponseStatusCode",
                schema: "Service",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "WebhookConfigurationId",
                schema: "Service",
                table: "OutboxMessages");
        }
    }
}
