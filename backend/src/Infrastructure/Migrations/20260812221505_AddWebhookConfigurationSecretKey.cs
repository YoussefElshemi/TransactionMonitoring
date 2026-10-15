using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookConfigurationSecretKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecretKey",
                schema: "Service",
                table: "WebhookConfigurations",
                type: "text",
                nullable: false,
                defaultValue: "");

            // An empty string is a valid HMAC key, so blank rows would sign successfully with a guessable secret.
            migrationBuilder.Sql(
                """
                UPDATE "Service"."WebhookConfigurations"
                SET "SecretKey" = 'whsec_' ||
                                  replace(gen_random_uuid()::text, '-', '') ||
                                  replace(gen_random_uuid()::text, '-', '')
                WHERE "SecretKey" = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecretKey",
                schema: "Service",
                table: "WebhookConfigurations");
        }
    }
}
