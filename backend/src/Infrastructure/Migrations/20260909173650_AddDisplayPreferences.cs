using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Locale",
                schema: "Identity",
                table: "Users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                schema: "Identity",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultLocale",
                schema: "Service",
                table: "Organizations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultTimeZone",
                schema: "Service",
                table: "Organizations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                schema: "Service",
                table: "NotificationSubscriptions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                schema: "Service",
                table: "Invoices",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                schema: "Service",
                table: "CreditNotes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                schema: "Service",
                table: "BillingProfiles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "Service",
                table: "Organizations",
                keyColumn: "OrganizationId",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DefaultLocale", "DefaultTimeZone" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Locale",
                schema: "Identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                schema: "Identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultLocale",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DefaultTimeZone",
                schema: "Service",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Locale",
                schema: "Service",
                table: "NotificationSubscriptions");

            migrationBuilder.DropColumn(
                name: "Locale",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Locale",
                schema: "Service",
                table: "CreditNotes");

            migrationBuilder.DropColumn(
                name: "Locale",
                schema: "Service",
                table: "BillingProfiles");
        }
    }
}
