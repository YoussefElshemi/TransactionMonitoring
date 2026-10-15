using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTieredPricingAndContractTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usages_OrganizationId",
                schema: "Service",
                table: "Usages");

            migrationBuilder.AddColumn<int>(
                name: "BillableResourceId",
                schema: "Service",
                table: "Usages",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumCharge",
                schema: "Service",
                table: "PlanVersions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFee",
                schema: "Service",
                table: "PlanVersions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "BillableResources",
                schema: "Service",
                columns: table => new
                {
                    BillableResourceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillableResources", x => x.BillableResourceId);
                });

            migrationBuilder.CreateTable(
                name: "Commitments",
                schema: "Service",
                columns: table => new
                {
                    CommitmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commitments", x => x.CommitmentId);
                    table.ForeignKey(
                        name: "FK_Commitments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Commitments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DiscountTypes",
                schema: "Service",
                columns: table => new
                {
                    DiscountTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountTypes", x => x.DiscountTypeId);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceAdjustments",
                schema: "Service",
                columns: table => new
                {
                    InvoiceAdjustmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    AppliedToInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceAdjustments", x => x.InvoiceAdjustmentId);
                    table.ForeignKey(
                        name: "FK_InvoiceAdjustments_Invoices_AppliedToInvoiceId",
                        column: x => x.AppliedToInvoiceId,
                        principalSchema: "Service",
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceAdjustments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceAdjustments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineTypes",
                schema: "Service",
                columns: table => new
                {
                    InvoiceLineTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineTypes", x => x.InvoiceLineTypeId);
                });

            migrationBuilder.CreateTable(
                name: "PricingModels",
                schema: "Service",
                columns: table => new
                {
                    PricingModelId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingModels", x => x.PricingModelId);
                });

            migrationBuilder.CreateTable(
                name: "ContractDiscounts",
                schema: "Service",
                columns: table => new
                {
                    ContractDiscountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountTypeId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_ContractDiscounts", x => x.ContractDiscountId);
                    table.ForeignKey(
                        name: "FK_ContractDiscounts_DiscountTypes_DiscountTypeId",
                        column: x => x.DiscountTypeId,
                        principalSchema: "Service",
                        principalTable: "DiscountTypes",
                        principalColumn: "DiscountTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractDiscounts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "Service",
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractDiscounts_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                schema: "Service",
                columns: table => new
                {
                    InvoiceLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceLineTypeId = table.Column<int>(type: "integer", nullable: false),
                    BillableResourceId = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    InvoiceAdjustmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.InvoiceLineId);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_BillableResources_BillableResourceId",
                        column: x => x.BillableResourceId,
                        principalSchema: "Service",
                        principalTable: "BillableResources",
                        principalColumn: "BillableResourceId");
                    table.ForeignKey(
                        name: "FK_InvoiceLines_InvoiceAdjustments_InvoiceAdjustmentId",
                        column: x => x.InvoiceAdjustmentId,
                        principalSchema: "Service",
                        principalTable: "InvoiceAdjustments",
                        principalColumn: "InvoiceAdjustmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_InvoiceLineTypes_InvoiceLineTypeId",
                        column: x => x.InvoiceLineTypeId,
                        principalSchema: "Service",
                        principalTable: "InvoiceLineTypes",
                        principalColumn: "InvoiceLineTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "Service",
                        principalTable: "Invoices",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanPrices",
                schema: "Service",
                columns: table => new
                {
                    PlanPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillableResourceId = table.Column<int>(type: "integer", nullable: true),
                    PricingModelId = table.Column<int>(type: "integer", nullable: false),
                    FreeAllowance = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanPrices", x => x.PlanPriceId);
                    table.ForeignKey(
                        name: "FK_PlanPrices_BillableResources_BillableResourceId",
                        column: x => x.BillableResourceId,
                        principalSchema: "Service",
                        principalTable: "BillableResources",
                        principalColumn: "BillableResourceId");
                    table.ForeignKey(
                        name: "FK_PlanPrices_PlanVersions_PlanVersionId",
                        column: x => x.PlanVersionId,
                        principalSchema: "Service",
                        principalTable: "PlanVersions",
                        principalColumn: "PlanVersionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanPrices_PricingModels_PricingModelId",
                        column: x => x.PricingModelId,
                        principalSchema: "Service",
                        principalTable: "PricingModels",
                        principalColumn: "PricingModelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanPriceTiers",
                schema: "Service",
                columns: table => new
                {
                    PlanPriceTierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromQuantity = table.Column<int>(type: "integer", nullable: false),
                    ToQuantity = table.Column<int>(type: "integer", nullable: true),
                    UnitAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    FlatAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanPriceTiers", x => x.PlanPriceTierId);
                    table.ForeignKey(
                        name: "FK_PlanPriceTiers_PlanPrices_PlanPriceId",
                        column: x => x.PlanPriceId,
                        principalSchema: "Service",
                        principalTable: "PlanPrices",
                        principalColumn: "PlanPriceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "AuditableEntityTypes",
                columns: new[] { "AuditableEntityTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 32, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ContractDiscount", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 33, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "InvoiceAdjustment", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 34, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Commitment", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "BillableResources",
                columns: new[] { "BillableResourceId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Unclassified", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "RuleEvaluation", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "RuleSetEvaluation", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "RuleTest", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Backtest", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SandboxWrite", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "DiscountTypes",
                columns: new[] { "DiscountTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Percentage", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "FixedAmount", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "InvoiceLineTypes",
                columns: new[] { "InvoiceLineTypeId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Usage", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "PlatformFee", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MinimumTopUp", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Discount", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CommitmentDrawdown", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Adjustment", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 86, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "contract-terms:read", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 87, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "contract-terms:write", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Service",
                table: "PricingModels",
                columns: new[] { "PricingModelId", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Flat", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Graduated", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Volume", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "Identity",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { 86, new Guid("00000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 86, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 87, new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 86, new Guid("00000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usages_BillableResourceId",
                schema: "Service",
                table: "Usages",
                column: "BillableResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Usages_OrganizationId_BillableResourceId_CreatedAt",
                schema: "Service",
                table: "Usages",
                columns: new[] { "OrganizationId", "BillableResourceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Commitments_CreatedByUserId",
                schema: "Service",
                table: "Commitments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Commitments_OrganizationId_EffectiveFrom",
                schema: "Service",
                table: "Commitments",
                columns: new[] { "OrganizationId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractDiscounts_CreatedByUserId",
                schema: "Service",
                table: "ContractDiscounts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractDiscounts_DiscountTypeId",
                schema: "Service",
                table: "ContractDiscounts",
                column: "DiscountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractDiscounts_OrganizationId_EffectiveFrom",
                schema: "Service",
                table: "ContractDiscounts",
                columns: new[] { "OrganizationId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAdjustments_AppliedToInvoiceId",
                schema: "Service",
                table: "InvoiceAdjustments",
                column: "AppliedToInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAdjustments_CreatedByUserId",
                schema: "Service",
                table: "InvoiceAdjustments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAdjustments_OrganizationId_Year_Month",
                schema: "Service",
                table: "InvoiceAdjustments",
                columns: new[] { "OrganizationId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_BillableResourceId",
                schema: "Service",
                table: "InvoiceLines",
                column: "BillableResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceAdjustmentId",
                schema: "Service",
                table: "InvoiceLines",
                column: "InvoiceAdjustmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId_SortOrder",
                schema: "Service",
                table: "InvoiceLines",
                columns: new[] { "InvoiceId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceLineTypeId",
                schema: "Service",
                table: "InvoiceLines",
                column: "InvoiceLineTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPrices_BillableResourceId",
                schema: "Service",
                table: "PlanPrices",
                column: "BillableResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPrices_PlanVersionId_BillableResourceId",
                schema: "Service",
                table: "PlanPrices",
                columns: new[] { "PlanVersionId", "BillableResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanPrices_PlanVersionId_CatchAll",
                schema: "Service",
                table: "PlanPrices",
                column: "PlanVersionId",
                unique: true,
                filter: "\"BillableResourceId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPrices_PricingModelId",
                schema: "Service",
                table: "PlanPrices",
                column: "PricingModelId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPriceTiers_PlanPriceId_FromQuantity",
                schema: "Service",
                table: "PlanPriceTiers",
                columns: new[] { "PlanPriceId", "FromQuantity" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Usages_BillableResources_BillableResourceId",
                schema: "Service",
                table: "Usages",
                column: "BillableResourceId",
                principalSchema: "Service",
                principalTable: "BillableResources",
                principalColumn: "BillableResourceId",
                onDelete: ReferentialAction.Cascade);

            // Every version published before this item has one rate and no price rows, and the
            // arithmetic reads price rows and nothing else. Left alone, every existing plan would bill
            // nothing at all. Each becomes what it always was: one catch-all price, flat, one band that
            // never ends, at the rate it was published with.
            migrationBuilder.Sql(
                """
                WITH inserted AS (
                    INSERT INTO "Service"."PlanPrices"
                        ("PlanPriceId", "PlanVersionId", "BillableResourceId", "PricingModelId", "FreeAllowance", "CreatedAt")
                    SELECT gen_random_uuid(), pv."PlanVersionId", NULL, 1, 0, pv."CreatedAt"
                    FROM "Service"."PlanVersions" pv
                    RETURNING "PlanPriceId", "PlanVersionId", "CreatedAt"
                )
                INSERT INTO "Service"."PlanPriceTiers"
                    ("PlanPriceTierId", "PlanPriceId", "FromQuantity", "ToQuantity", "UnitAmount", "FlatAmount", "CreatedAt")
                SELECT gen_random_uuid(), i."PlanPriceId", 0, NULL, pv."CostPerRequest", 0, i."CreatedAt"
                FROM inserted i
                JOIN "Service"."PlanVersions" pv ON pv."PlanVersionId" = i."PlanVersionId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usages_BillableResources_BillableResourceId",
                schema: "Service",
                table: "Usages");

            migrationBuilder.DropTable(
                name: "Commitments",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "ContractDiscounts",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "InvoiceLines",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "PlanPriceTiers",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "DiscountTypes",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "InvoiceAdjustments",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "InvoiceLineTypes",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "PlanPrices",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "BillableResources",
                schema: "Service");

            migrationBuilder.DropTable(
                name: "PricingModels",
                schema: "Service");

            migrationBuilder.DropIndex(
                name: "IX_Usages_BillableResourceId",
                schema: "Service",
                table: "Usages");

            migrationBuilder.DropIndex(
                name: "IX_Usages_OrganizationId_BillableResourceId_CreatedAt",
                schema: "Service",
                table: "Usages");

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 32);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 33);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "AuditableEntityTypes",
                keyColumn: "AuditableEntityTypeId",
                keyValue: 34);

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 86, new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 86, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 87, new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 86, new Guid("00000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 86);

            migrationBuilder.DeleteData(
                schema: "Service",
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 87);

            migrationBuilder.DropColumn(
                name: "BillableResourceId",
                schema: "Service",
                table: "Usages");

            migrationBuilder.DropColumn(
                name: "MinimumCharge",
                schema: "Service",
                table: "PlanVersions");

            migrationBuilder.DropColumn(
                name: "PlatformFee",
                schema: "Service",
                table: "PlanVersions");

            migrationBuilder.CreateIndex(
                name: "IX_Usages_OrganizationId",
                schema: "Service",
                table: "Usages",
                column: "OrganizationId");
        }
    }
}
