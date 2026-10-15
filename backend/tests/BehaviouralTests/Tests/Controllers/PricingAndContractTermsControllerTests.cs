using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Presentation.Models.Adjustments;
using Presentation.Models.Commitments;
using Presentation.Models.Common;
using Presentation.Models.Discounts;
using Presentation.Models.Invoices;
using Presentation.Models.OrganizationInvoices;
using Presentation.Models.Plans;
using Presentation.Models.Subscriptions;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class PricingAndContractTermsControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string AdminEmail = "platform@mail.com";
    private const string ManagerEmail = "desk@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("2f6c1e0b-9a45-4d18-8f31-5c7e0a2b4d69");
    private static readonly DateTimeOffset BilledMonth = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        await testFixture.ResetDatabaseAsync();

        await DatabaseHelper.CreateOrganizations(testFixture.Services,
        [
            OrganizationConfigurations.DefaultOrganization,
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            }
        ]);

        await AuthenticationHelper.CreateUserAsync(testFixture.Services, AdminEmail, Password, [UserRole.Admin], OrganizationId);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, ManagerEmail, Password, [UserRole.Manager], OrganizationId);

        await AuthenticationHelper.SetTokenAsync(_httpClient, AdminEmail, Password);

        await DatabaseHelper.CreateBillingProfile(testFixture.Services, OrganizationId, customerCountryCode: "US");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GenerateInvoice_GivenGraduatedBands_PricesEachUnitInTheBandItFallsIn()
    {
        // Arrange
        await GivenPlanInForce(PlanBody("Graduated", prices:
        [
            Price(null, PricingModel.Graduated, 0, Band(0, 1000, 0.10m), Band(1000, null, 0.01m))
        ]));

        await GivenMeteredUsage(BillableResource.RuleEvaluation, 10000);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.Amount.Should().Be(190m);
        invoice.Lines.Where(x => x.InvoiceLineType == nameof(InvoiceLineType.Usage)).Should().HaveCount(2);
        invoice.Lines.Sum(x => x.Amount).Should().Be(invoice.Amount);
    }

    [Fact]
    public async Task GenerateInvoice_GivenVolumeBands_PricesEveryUnitInTheBandTheTotalReaches()
    {
        // Arrange
        await GivenPlanInForce(PlanBody("Volume", prices:
        [
            Price(null, PricingModel.Volume, 0, Band(0, 1000, 0.10m), Band(1000, null, 0.01m))
        ]));

        await GivenMeteredUsage(BillableResource.RuleEvaluation, 10000);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.Amount.Should().Be(100m);
        invoice.Lines.Where(x => x.InvoiceLineType == nameof(InvoiceLineType.Usage)).Should().ContainSingle();
    }

    [Fact]
    public async Task GenerateInvoice_GivenAFreeAllowance_ChargesOnlyWhatItDidNotCover()
    {
        // Arrange
        await GivenPlanInForce(PlanBody("Allowance", prices:
        [
            Price(null, PricingModel.Flat, 9000, Band(0, null, 0.10m))
        ]));

        await GivenMeteredUsage(BillableResource.RuleEvaluation, 10000);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.Amount.Should().Be(100m);
        invoice.Lines.Should().Contain(x => x.Quantity == 9000 && x.Amount == 0m);
    }

    [Fact]
    public async Task GenerateInvoice_GivenPerResourcePrices_ChargesEachKindOfWorkAtItsOwnRate()
    {
        // Arrange
        await GivenPlanInForce(PlanBody("Per resource", prices:
        [
            Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)),
            Price(BillableResource.Backtest, PricingModel.Flat, 0, Band(0, null, 0.01m))
        ]));

        await GivenMeteredUsage(BillableResource.RuleEvaluation, 100);
        await GivenMeteredUsage(BillableResource.Backtest, 100);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.Amount.Should().Be(11m);
        invoice.Lines.Single(x => x.BillableResource == nameof(BillableResource.RuleEvaluation)).Amount.Should().Be(10m);
        invoice.Lines.Single(x => x.BillableResource == nameof(BillableResource.Backtest)).Amount.Should().Be(1m);
    }

    [Fact]
    public async Task GenerateInvoice_GivenAPlatformFeeAndAMinimum_SumsTheFeeThenTopsUpToTheFloor()
    {
        // Arrange
        await GivenPlanInForce(PlanBody("Platform", platformFee: 200m, minimumCharge: 1000m, prices:
        [
            Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m))
        ]));

        await GivenMeteredUsage(BillableResource.RuleEvaluation, 1000);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.Amount.Should().Be(1000m);
        invoice.Lines.Should().Contain(x => x.InvoiceLineType == nameof(InvoiceLineType.PlatformFee) && x.Amount == 200m);
        invoice.Lines.Should().Contain(x => x.InvoiceLineType == nameof(InvoiceLineType.MinimumTopUp) && x.Amount == 700m);
    }

    [Fact]
    public async Task PublishPlanVersion_GivenBandsWithAGap_ReturnsBadRequest()
    {
        // Arrange
        var planId = await CreatePlanAsync(PlanBody("Gappy"));

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/plans/{planId}/versions",
            new PublishPlanVersionRequestBodyDto
            {
                CostPerRequest = 0.10m,
                Currency = Currency.Gbp,
                Prices = [Price(null, PricingModel.Graduated, 0, Band(0, 1000, 0.10m), Band(5000, null, 0.01m))]
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PublishPlanVersion_GivenABoundedTopBand_ReturnsBadRequest()
    {
        // Arrange
        var planId = await CreatePlanAsync(PlanBody("Capped"));

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/plans/{planId}/versions",
            new PublishPlanVersionRequestBodyDto
            {
                CostPerRequest = 0.10m,
                Currency = Currency.Gbp,
                Prices = [Price(null, PricingModel.Flat, 0, Band(0, 1000, 0.10m))]
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateInvoice_GivenAContractDiscount_TakesItOffTheMonth()
    {
        // Arrange
        await GivenPlanInForce(PlanBody("Discounted", prices:
        [
            Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m))
        ]));

        await GivenMeteredUsage(BillableResource.RuleEvaluation, 10000);
        await CreateDiscountAsync(DiscountType.Percentage, 10m);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.Amount.Should().Be(900m);
        invoice.Lines.Should().Contain(x => x.InvoiceLineType == nameof(InvoiceLineType.Discount) && x.Amount == -100m);
    }

    [Fact]
    public async Task GenerateInvoice_GivenADiscountThatHadAlreadyEnded_LeavesTheMonthUndiscounted()
    {
        // Arrange
        await GivenPlanInForce(PlanBody("Expired discount", prices:
        [
            Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m))
        ]));

        await GivenMeteredUsage(BillableResource.RuleEvaluation, 1000);

        var discountId = await CreateDiscountAsync(DiscountType.Percentage, 10m);
        await DatabaseHelper.BackdateContractDiscount(
            testFixture.Services, discountId, BilledMonth.AddYears(-1), BilledMonth.AddMonths(-1));

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.Amount.Should().Be(100m);
        invoice.Lines.Should().NotContain(x => x.InvoiceLineType == nameof(InvoiceLineType.Discount));
    }

    [Fact]
    public async Task GenerateInvoice_GivenAnUnappliedAdjustment_CarriesItAsALineAndStampsIt()
    {
        // Arrange
        await GivenPlanInForce(PlanBody("Adjusted", prices:
        [
            Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m))
        ]));

        await GivenMeteredUsage(BillableResource.RuleEvaluation, 1000);
        var adjustmentId = await CreateAdjustmentAsync(-25m);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.Amount.Should().Be(75m);
        invoice.Lines.Should().Contain(x => x.InvoiceLineType == nameof(InvoiceLineType.Adjustment) && x.Amount == -25m);

        var adjustment = await GetAdjustmentAsync(adjustmentId);
        adjustment.AppliedToInvoiceId.Should().Be(invoice.InvoiceId);
    }

    [Fact]
    public async Task DeleteAdjustment_GivenOneAlreadyCarriedOntoAnInvoice_ReturnsConflict()
    {
        // Arrange
        await GivenPlanInForce(PlanBody("Adjusted", prices:
        [
            Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m))
        ]));

        await GivenMeteredUsage(BillableResource.RuleEvaluation, 1000);
        var adjustmentId = await CreateAdjustmentAsync(-25m);
        await GenerateInvoiceAsync();

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/invoice-adjustments/{adjustmentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GenerateInvoice_GivenACommitment_DrawsTheMonthDownAndBillsOnlyTheOverage()
    {
        // Arrange
        await GivenPlanInForce(PlanBody("Committed", prices:
        [
            Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m))
        ]));

        await GivenMeteredUsage(BillableResource.RuleEvaluation, 10000);
        var commitmentId = await CreateCommitmentAsync(600m);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.Amount.Should().Be(400m);
        invoice.Lines.Should().Contain(x => x.InvoiceLineType == nameof(InvoiceLineType.CommitmentDrawdown) && x.Amount == -600m);

        var commitment = await GetCommitmentAsync(commitmentId);
        commitment.Drawn.Should().Be(600m);
        commitment.Remaining.Should().Be(0m);
    }

    [Fact]
    public async Task DeleteInvoice_GivenACommitmentItDrewFrom_ReturnsTheBalance()
    {
        // Arrange
        await GivenPlanInForce(PlanBody("Committed", prices:
        [
            Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m))
        ]));

        await GivenMeteredUsage(BillableResource.RuleEvaluation, 1000);
        var commitmentId = await CreateCommitmentAsync(600m);
        var invoice = await GenerateInvoiceAsync();

        // Act
        await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/invoices/{invoice.InvoiceId}");

        // Assert
        var commitment = await GetCommitmentAsync(commitmentId);
        commitment.Drawn.Should().Be(0m);
        commitment.Remaining.Should().Be(600m);
    }

    [Fact]
    public async Task CreateCommitment_GivenAnOverlappingTerm_ReturnsConflict()
    {
        // Arrange
        await CreateCommitmentAsync(600m);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/commitments",
            new CreateCommitmentRequestBodyDto
            {
                Amount = 100m,
                EffectiveFrom = null,
                EffectiveTo = DateTimeOffset.UtcNow.AddYears(2)
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateContractDiscount_AsManager_ReturnsForbidden()
    {
        // Arrange
        await AuthenticationHelper.SetTokenAsync(_httpClient, ManagerEmail, Password);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/contract-discounts",
            new CreateContractDiscountRequestBodyDto
            {
                DiscountType = DiscountType.Percentage,
                Value = 10m,
                Description = "Negotiated",
                EffectiveFrom = null,
                EffectiveTo = null
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListContractDiscounts_AsManager_ReturnsOk()
    {
        // Arrange
        await CreateDiscountAsync(DiscountType.Percentage, 10m);
        await AuthenticationHelper.SetTokenAsync(_httpClient, ManagerEmail, Password);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/contract-discounts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var discounts = await response.Content.ReadFromJsonAsync<PagedResponseDto<ContractDiscountResponseDto>>();
        discounts!.Data.Should().ContainSingle();
    }

    private static PlanPriceTierRequestDto Band(int from, int? to, decimal unit, decimal flat = 0)
    {
        return new PlanPriceTierRequestDto
        {
            FromQuantity = from,
            ToQuantity = to,
            UnitAmount = unit,
            FlatAmount = flat
        };
    }

    private static PlanPriceRequestDto Price(
        BillableResource? billableResource,
        PricingModel pricingModel,
        int freeAllowance,
        params PlanPriceTierRequestDto[] tiers)
    {
        return new PlanPriceRequestDto
        {
            BillableResource = billableResource,
            PricingModel = pricingModel,
            FreeAllowance = freeAllowance,
            Tiers = [..tiers]
        };
    }

    private static CreatePlanRequestBodyDto PlanBody(
        string name,
        decimal platformFee = 0,
        decimal minimumCharge = 0,
        List<PlanPriceRequestDto>? prices = null)
    {
        return new CreatePlanRequestBodyDto
        {
            Name = name,
            Currency = Currency.Gbp,
            Description = null,
            OrganizationId = null,
            CostPerRequest = 0.10m,
            PlatformFee = platformFee,
            MinimumCharge = minimumCharge,
            Prices = prices ?? []
        };
    }

    private async Task<Guid> CreatePlanAsync(CreatePlanRequestBodyDto body)
    {
        var response = await _httpClient.PostAsJsonAsync("/plans", body);
        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<PlanResponseDto>>();

        return created!.Data.PlanId;
    }

    private async Task GivenPlanInForce(CreatePlanRequestBodyDto body)
    {
        var response = await _httpClient.GetAsync($"/plans/{await CreatePlanAsync(body)}");
        var plan = await response.Content.ReadFromJsonAsync<ResponseDataDto<PlanResponseDto>>();

        await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/subscriptions",
            new CreateSubscriptionRequestBodyDto
            {
                PlanVersionId = plan!.Data.CurrentVersion!.PlanVersionId,
                EffectiveFrom = null
            });

        await DatabaseHelper.BackdateSubscriptions(testFixture.Services, OrganizationId, BilledMonth.AddYears(-1));
    }

    private Task GivenMeteredUsage(BillableResource billableResource, int count)
    {
        return DatabaseHelper.CreateUsages(testFixture.Services,
        [
            FakeUsageEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                BillableResourceId = (int)billableResource,
                Count = count,
                CreatedAt = BilledMonth
            }
        ]);
    }

    private async Task<InvoiceResponseDto> GenerateInvoiceAsync()
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices",
            new GenerateOrganizationInvoiceRequestBodyDto { Month = BilledMonth.Month, Year = BilledMonth.Year });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var invoice = await response.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();

        return invoice!.Invoice;
    }

    private async Task<Guid> CreateDiscountAsync(DiscountType discountType, decimal value)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/contract-discounts",
            new CreateContractDiscountRequestBodyDto
            {
                DiscountType = discountType,
                Value = value,
                Description = "Negotiated",
                EffectiveFrom = BilledMonth.AddYears(-1),
                EffectiveTo = null
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<ContractDiscountResponseDto>>();

        return created!.Data.ContractDiscountId;
    }

    private async Task<Guid> CreateAdjustmentAsync(decimal amount)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoice-adjustments",
            new CreateInvoiceAdjustmentRequestBodyDto
            {
                Month = BilledMonth.Month,
                Year = BilledMonth.Year,
                Description = "Goodwill credit",
                Amount = amount
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<InvoiceAdjustmentResponseDto>>();

        return created!.Data.InvoiceAdjustmentId;
    }

    private async Task<InvoiceAdjustmentResponseDto> GetAdjustmentAsync(Guid invoiceAdjustmentId)
    {
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invoice-adjustments/{invoiceAdjustmentId}");
        var adjustment = await response.Content.ReadFromJsonAsync<ResponseDataDto<InvoiceAdjustmentResponseDto>>();

        return adjustment!.Data;
    }

    private async Task<Guid> CreateCommitmentAsync(decimal amount)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/commitments",
            new CreateCommitmentRequestBodyDto
            {
                Amount = amount,
                EffectiveFrom = BilledMonth.AddYears(-1),
                EffectiveTo = DateTimeOffset.UtcNow.AddYears(1)
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<ResponseDataDto<CommitmentResponseDto>>();

        return created!.Data.CommitmentId;
    }

    private async Task<CommitmentResponseDto> GetCommitmentAsync(Guid commitmentId)
    {
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/commitments/{commitmentId}");
        var commitment = await response.Content.ReadFromJsonAsync<ResponseDataDto<CommitmentResponseDto>>();

        return commitment!.Data;
    }
}
