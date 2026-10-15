using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiClientApiVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiVersion",
                schema: "Identity",
                table: "ApiClients",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,

                // Every client that already exists was written against the API as it stood before it
                // had versions, which is what the first version is. Pinning them there is what makes
                // the next version opt-in for them rather than something that happens to them.
                defaultValue: "2026-10-14");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiVersion",
                schema: "Identity",
                table: "ApiClients");
        }
    }
}
