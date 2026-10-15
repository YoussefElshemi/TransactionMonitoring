using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                schema: "Service",
                table: "PlanVersions",
                type: "integer",
                nullable: false,
                defaultValue: 826);

            migrationBuilder.AddColumn<Guid>(
                name: "BillingEntityId",
                schema: "Service",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConversionRate",
                schema: "Service",
                table: "Invoices",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                schema: "Service",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 826);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddress",
                schema: "Service",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCountryCode",
                schema: "Service",
                table: "Invoices",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                schema: "Service",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                schema: "Service",
                table: "Invoices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerTaxIdentifier",
                schema: "Service",
                table: "Invoices",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueAt",
                schema: "Service",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                schema: "Service",
                table: "Invoices",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceStatusId",
                schema: "Service",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IssuedAt",
                schema: "Service",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                schema: "Service",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTerms",
                schema: "Service",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 28);

            migrationBuilder.AddColumn<int>(
                name: "PlanCurrencyId",
                schema: "Service",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 826);

            migrationBuilder.AddColumn<string>(
                name: "SupplierAddress",
                schema: "Service",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierCountryCode",
                schema: "Service",
                table: "Invoices",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                schema: "Service",
                table: "Invoices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierTaxIdentifier",
                schema: "Service",
                table: "Invoices",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "Service",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                schema: "Service",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TaxTreatmentId",
                schema: "Service",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateTable(
                name: "Currencies",
                schema: "Service",
                columns: table => new
                {
                    CurrencyId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.CurrencyId);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceStatuses",
                schema: "Service",
                columns: table => new
                {
                    InvoiceStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceStatuses", x => x.InvoiceStatusId);
                });

            migrationBuilder.CreateTable(
                name: "TaxTreatments",
                schema: "Service",
                columns: table => new
                {
                    TaxTreatmentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxTreatments", x => x.TaxTreatmentId);
                });

            migrationBuilder.CreateTable(
                name: "BillingEntities",
                schema: "Service",
                columns: table => new
                {
                    BillingEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    TaxIdentifier = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    InvoiceNumberPrefix = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CreditNoteNumberPrefix = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CurrencyId = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingEntities", x => x.BillingEntityId);
                    table.ForeignKey(
                        name: "FK_BillingEntities_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "Service",
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingEntities_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                schema: "Service",
                columns: table => new
                {
                    ExchangeRateId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCurrencyId = table.Column<int>(type: "integer", nullable: false),
                    ToCurrencyId = table.Column<int>(type: "integer", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.ExchangeRateId);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_FromCurrencyId",
                        column: x => x.FromCurrencyId,
                        principalSchema: "Service",
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_ToCurrencyId",
                        column: x => x.ToCurrencyId,
                        principalSchema: "Service",
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BillingProfiles",
                schema: "Service",
                columns: table => new
                {
                    BillingProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    TaxIdentifier = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    TaxIdentifierVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrencyId = table.Column<int>(type: "integer", nullable: false),
                    PaymentTerms = table.Column<int>(type: "integer", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingProfiles", x => x.BillingProfileId);
                    table.ForeignKey(
                        name: "FK_BillingProfiles_BillingEntities_BillingEntityId",
                        column: x => x.BillingEntityId,
                        principalSchema: "Service",
                        principalTable: "BillingEntities",
                        principalColumn: "BillingEntityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingProfiles_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "Service",
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingProfiles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingProfiles_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                schema: "Service",
                columns: table => new
                {
                    CreditNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CurrencyId = table.Column<int>(type: "integer", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxTreatmentId = table.Column<int>(type: "integer", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.CreditNoteId);
                    table.ForeignKey(
                        name: "FK_CreditNotes_BillingEntities_BillingEntityId",
                        column: x => x.BillingEntityId,
                        principalSchema: "Service",
                        principalTable: "BillingEntities",
                        principalColumn: "BillingEntityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "Service",
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "Service",
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNotes_TaxTreatments_TaxTreatmentId",
                        column: x => x.TaxTreatmentId,
                        principalSchema: "Service",
                        principalTable: "TaxTreatments",
                        principalColumn: "TaxTreatmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvoiceNumberSequences",
                schema: "Service",
                columns: table => new
                {
                    BillingEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastInvoiceNumber = table.Column<int>(type: "integer", nullable: false),
                    LastCreditNoteNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceNumberSequences", x => new { x.BillingEntityId, x.Year });
                    table.ForeignKey(
                        name: "FK_InvoiceNumberSequences_BillingEntities_BillingEntityId",
                        column: x => x.BillingEntityId,
                        principalSchema: "Service",
                        principalTable: "BillingEntities",
                        principalColumn: "BillingEntityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditNoteLines",
                schema: "Service",
                columns: table => new
                {
                    CreditNoteLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNoteLines", x => x.CreditNoteLineId);
                    table.ForeignKey(
                        name: "FK_CreditNoteLines_CreditNotes_CreditNoteId",
                        column: x => x.CreditNoteId,
                        principalSchema: "Service",
                        principalTable: "CreditNotes",
                        principalColumn: "CreditNoteId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 35, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "BillingEntity", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 36, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "BillingProfile", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 37, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CreditNote", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "Currencies",
                columns: new[] { "CurrencyId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 826, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "GBP", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 840, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 978, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "EUR", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "InvoiceLineTypes",
                columns: new[] { "InvoiceLineTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 7, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tax", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "InvoiceStatuses",
                columns: new[] { "InvoiceStatusId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Draft", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Issued", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 88, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "billing-entities:read", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 89, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "billing-entities:write", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 90, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "billing-profiles:read", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 91, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "billing-profiles:write", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 92, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "exchange-rates:read", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 93, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "exchange-rates:write", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 94, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "credit-notes:read", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 95, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "credit-notes:write", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "TaxTreatments",
                columns: new[] { "TaxTreatmentId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Standard", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ReverseCharge", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "OutOfScope", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Identity",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { 90, new Guid("00000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 94, new Guid("00000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 88, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 89, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 90, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 91, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 92, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 93, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 94, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 95, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 90, new Guid("00000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 94, new Guid("00000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            // Invoices issued before the document had a shape. They keep no number, no issuing entity and
            // no parties: the address and registration were never on them, and inventing any of it today
            // would claim they said something they did not.
            //
            // What is filled in is what those documents genuinely stated. Their exported PDF printed a
            // due date of the issue date plus the configured twenty-eight days and priced everything in
            // sterling; they charged no tax, so the whole of the amount is the net and the treatment is
            // out of scope. They are Issued because they were sent, not drafts somebody forgot.
            migrationBuilder.Sql(
                """
                UPDATE "Service"."Invoices"
                SET "NetAmount" = "Amount",
                    "IssuedAt" = "CreatedAt",
                    "DueAt" = "CreatedAt" + interval '28 days'
                WHERE "IssuedAt" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PlanVersions_CurrencyId",
                schema: "Service",
                table: "PlanVersions",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BillingEntityId_InvoiceNumber",
                schema: "Service",
                table: "Invoices",
                columns: new[] { "BillingEntityId", "InvoiceNumber" },
                unique: true,
                filter: "\"InvoiceNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CurrencyId",
                schema: "Service",
                table: "Invoices",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DueAt",
                schema: "Service",
                table: "Invoices",
                column: "DueAt",
                filter: "\"DueAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceStatusId",
                schema: "Service",
                table: "Invoices",
                column: "InvoiceStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PlanCurrencyId",
                schema: "Service",
                table: "Invoices",
                column: "PlanCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TaxTreatmentId",
                schema: "Service",
                table: "Invoices",
                column: "TaxTreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingEntities_CreatedByUserId",
                schema: "Service",
                table: "BillingEntities",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingEntities_CreditNoteNumberPrefix",
                schema: "Service",
                table: "BillingEntities",
                column: "CreditNoteNumberPrefix",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BillingEntities_CurrencyId",
                schema: "Service",
                table: "BillingEntities",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingEntities_InvoiceNumberPrefix",
                schema: "Service",
                table: "BillingEntities",
                column: "InvoiceNumberPrefix",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BillingProfiles_BillingEntityId",
                schema: "Service",
                table: "BillingProfiles",
                column: "BillingEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingProfiles_CurrencyId",
                schema: "Service",
                table: "BillingProfiles",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingProfiles_OrganizationId",
                schema: "Service",
                table: "BillingProfiles",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingProfiles_UpdatedByUserId",
                schema: "Service",
                table: "BillingProfiles",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNoteLines_CreditNoteId_SortOrder",
                schema: "Service",
                table: "CreditNoteLines",
                columns: new[] { "CreditNoteId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_BillingEntityId_CreditNoteNumber",
                schema: "Service",
                table: "CreditNotes",
                columns: new[] { "BillingEntityId", "CreditNoteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_CreatedByUserId",
                schema: "Service",
                table: "CreditNotes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_CurrencyId",
                schema: "Service",
                table: "CreditNotes",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_InvoiceId",
                schema: "Service",
                table: "CreditNotes",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_OrganizationId",
                schema: "Service",
                table: "CreditNotes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TaxTreatmentId",
                schema: "Service",
                table: "CreditNotes",
                column: "TaxTreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_CreatedByUserId",
                schema: "Service",
                table: "ExchangeRates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_FromCurrencyId_ToCurrencyId_EffectiveFrom",
                schema: "Service",
                table: "ExchangeRates",
                columns: new[] { "FromCurrencyId", "ToCurrencyId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_ToCurrencyId",
                schema: "Service",
                table: "ExchangeRates",
                column: "ToCurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_BillingEntities_BillingEntityId",
                schema: "Service",
                table: "Invoices",
                column: "BillingEntityId",
                principalSchema: "Service",
                principalTable: "BillingEntities",
                principalColumn: "BillingEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Currencies_CurrencyId",
                schema: "Service",
                table: "Invoices",
                column: "CurrencyId",
                principalSchema: "Service",
                principalTable: "Currencies",
                principalColumn: "CurrencyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Currencies_PlanCurrencyId",
                schema: "Service",
                table: "Invoices",
                column: "PlanCurrencyId",
                principalSchema: "Service",
                principalTable: "Currencies",
                principalColumn: "CurrencyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_InvoiceStatuses_InvoiceStatusId",
                schema: "Service",
                table: "Invoices",
                column: "InvoiceStatusId",
                principalSchema: "Service",
                principalTable: "InvoiceStatuses",
                principalColumn: "InvoiceStatusId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TaxTreatments_TaxTreatmentId",
                schema: "Service",
                table: "Invoices",
                column: "TaxTreatmentId",
                principalSchema: "Service",
                principalTable: "TaxTreatments",
                principalColumn: "TaxTreatmentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanVersions_Currencies_CurrencyId",
                schema: "Service",
                table: "PlanVersions",
                column: "CurrencyId",
                principalSchema: "Service",
                principalTable: "Currencies",
                principalColumn: "CurrencyId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_BillingEntities_BillingEntityId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Currencies_CurrencyId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Currencies_PlanCurrencyId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_InvoiceStatuses_InvoiceStatusId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TaxTreatments_TaxTreatmentId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanVersions_Currencies_CurrencyId",
                schema: "Service",
                table: "PlanVersions");

            migrationBuilder.DropTable(
                name: "BillingProfiles",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "CreditNoteLines",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "ExchangeRates",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "InvoiceNumberSequences",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "InvoiceStatuses",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "CreditNotes",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "BillingEntities",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "TaxTreatments",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "Currencies",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_PlanVersions_CurrencyId",
                schema: "Service",
                table: "PlanVersions");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_BillingEntityId_InvoiceNumber",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CurrencyId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_DueAt",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceStatusId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PlanCurrencyId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TaxTreatmentId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 35);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 36);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 37);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "InvoiceLineTypes",
                keyColumn: "InvoiceLineTypeId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 90, new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 94, new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 88, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 89, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 90, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 91, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 92, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 93, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 94, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 95, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 90, new Guid("00000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 94, new Guid("00000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 88);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 89);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 90);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 91);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 92);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 93);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 94);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 95);

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                schema: "Service",
                table: "PlanVersions");

            migrationBuilder.DropColumn(
                name: "BillingEntityId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ConversionRate",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerAddress",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerCountryCode",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerTaxIdentifier",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DueAt",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceStatusId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PlanCurrencyId",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SupplierAddress",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SupplierCountryCode",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SupplierTaxIdentifier",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                schema: "Service",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxTreatmentId",
                schema: "Service",
                table: "Invoices");
        }
    }
}
