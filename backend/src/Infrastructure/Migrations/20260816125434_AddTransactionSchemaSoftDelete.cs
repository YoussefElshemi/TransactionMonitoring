using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionSchemaSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionSchemas_OrganizationId",
                schema: "Service",
                table: "TransactionSchemas");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                schema: "Service",
                table: "TransactionSchemas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "Service",
                table: "TransactionSchemas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSchemas_OrganizationId",
                schema: "Service",
                table: "TransactionSchemas",
                column: "OrganizationId",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionSchemas_OrganizationId",
                schema: "Service",
                table: "TransactionSchemas");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "Service",
                table: "TransactionSchemas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "Service",
                table: "TransactionSchemas");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSchemas_OrganizationId",
                schema: "Service",
                table: "TransactionSchemas",
                column: "OrganizationId",
                unique: true);
        }
    }
}
