using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeDecisionCaseUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Decisions_CaseId",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_CaseId",
                schema: "Service",
                table: "Decisions",
                column: "CaseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Decisions_CaseId",
                schema: "Service",
                table: "Decisions");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_CaseId",
                schema: "Service",
                table: "Decisions",
                column: "CaseId");
        }
    }
}
