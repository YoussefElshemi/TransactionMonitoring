using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "TransactionNumberSequence",
                schema: "Service");

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "Service",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FriendlyName = table.Column<string>(type: "text", nullable: false),
                    ExternalReference = table.Column<string>(type: "text", nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_Transactions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 11, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Transaction", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                schema: "Service",
                table: "RuleEvaluations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransactionId",
                schema: "Service",
                table: "Cases",
                type: "uuid",
                nullable: true);

            // One transaction per existing evaluation, carrying its payload across before the column is dropped.
            // xmin is a system column, so it is not written here.
            migrationBuilder.Sql(
                """
                INSERT INTO "Service"."Transactions"
                    ("TransactionId", "OrganizationId", "FriendlyName", "ExternalReference", "Payload",
                     "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt")
                SELECT re."RuleEvaluationId",
                       re."OrganizationId",
                       'TX' || lpad(nextval('"Service"."TransactionNumberSequence"')::text, 10, '0'),
                       NULL,
                       re."Payload",
                       re."CreatedAt",
                       re."UpdatedAt",
                       re."IsDeleted",
                       re."DeletedAt"
                FROM "Service"."RuleEvaluations" re;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Service"."RuleEvaluations" SET "TransactionId" = "RuleEvaluationId";
                """);

            // A historic case points at the payment its first linked evaluation was about. Cases with no link have
            // nothing to point at, which is why the column stays nullable.
            migrationBuilder.Sql(
                """
                UPDATE "Service"."Cases" c
                SET "TransactionId" = (
                    SELECT l."RuleEvaluationId"
                    FROM "Service"."CaseRuleEvaluationLinks" l
                    WHERE l."CaseId" = c."CaseId"
                    ORDER BY l."CreatedAt", l."RuleEvaluationId"
                    LIMIT 1);
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TransactionId",
                schema: "Service",
                table: "RuleEvaluations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Payload",
                schema: "Service",
                table: "RuleEvaluations");

            migrationBuilder.CreateIndex(
                name: "IX_RuleEvaluations_TransactionId",
                schema: "Service",
                table: "RuleEvaluations",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_TransactionId",
                schema: "Service",
                table: "Cases",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FriendlyName",
                schema: "Service",
                table: "Transactions",
                column: "FriendlyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrganizationId_ExternalReference",
                schema: "Service",
                table: "Transactions",
                columns: new[] { "OrganizationId", "ExternalReference" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Transactions_TransactionId",
                schema: "Service",
                table: "Cases",
                column: "TransactionId",
                principalSchema: "Service",
                principalTable: "Transactions",
                principalColumn: "TransactionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RuleEvaluations_Transactions_TransactionId",
                schema: "Service",
                table: "RuleEvaluations",
                column: "TransactionId",
                principalSchema: "Service",
                principalTable: "Transactions",
                principalColumn: "TransactionId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Transactions_TransactionId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleEvaluations_Transactions_TransactionId",
                schema: "Service",
                table: "RuleEvaluations");

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                schema: "Service",
                table: "RuleEvaluations",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Service"."RuleEvaluations" re
                SET "Payload" = t."Payload"
                FROM "Service"."Transactions" t
                WHERE t."TransactionId" = re."TransactionId";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Payload",
                schema: "Service",
                table: "RuleEvaluations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_RuleEvaluations_TransactionId",
                schema: "Service",
                table: "RuleEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_Cases_TransactionId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                schema: "Service",
                table: "RuleEvaluations");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                schema: "Service",
                table: "Cases");

            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "Service");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 11);

            migrationBuilder.DropSequence(
                name: "TransactionNumberSequence",
                schema: "Service");
        }
    }
}
