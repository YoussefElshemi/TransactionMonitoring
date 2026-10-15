using Core.Configs;
using Core.Enums;
using Core.Exceptions;
using Core.Factories;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Billing;
using Core.Models.Commitments;
using Core.Models.Invoices;
using Core.Models.Organizations;
using Core.Models.Subscriptions;
using Core.Models.Usages;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using QuestPDF;
using QuestPDF.Infrastructure;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Adjustments;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Billing;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Localization;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Subscriptions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Tax;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class InvoiceServiceTests : BaseTestClass
{
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepository = new();
    private readonly Mock<IInvoiceAdjustmentRepository> _mockInvoiceAdjustmentRepository = new();
    private readonly Mock<ISubscriptionService> _mockSubscriptionService = new();
    private readonly Mock<IContractDiscountService> _mockContractDiscountService = new();
    private readonly Mock<ICommitmentService> _mockCommitmentService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IDisplayPreferenceService> _mockDisplayPreferenceService = new();
    private readonly Mock<IUsageService> _mockUsageService = new();
    private readonly Mock<IBillingProfileService> _mockBillingProfileService = new();
    private readonly Mock<IBillingEntityService> _mockBillingEntityService = new();
    private readonly Mock<IExchangeRateService> _mockExchangeRateService = new();
    private readonly Mock<IDocumentNumberRepository> _mockDocumentNumberRepository = new();
    private readonly Mock<IOptions<AppConfig>> _mockConfig = new();
    private readonly BillingProfile _billingProfile;
    private readonly InvoiceService _sut;

    public InvoiceServiceTests()
    {
        Settings.License = LicenseType.Community;

        _mockConfig
            .SetupGet(x => x.Value)
            .Returns(FakeAppConfig.CreateValid(Fixture));

        _mockContractDiscountService
            .Setup(x => x.GetInEffectAsync(It.IsAny<OrganizationId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockInvoiceAdjustmentRepository
            .Setup(x => x.GetUnappliedAsync(It.IsAny<OrganizationId>(), It.IsAny<Month>(), It.IsAny<Year>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockCommitmentService
            .Setup(x => x.GetInEffectAsync(It.IsAny<OrganizationId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Commitment?)null);

        _billingProfile = FakeBillingProfile.CreateValid(Fixture);

        _mockBillingProfileService
            .Setup(x => x.FindAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_billingProfile);

        _mockBillingEntityService
            .Setup(x => x.GetAsync(It.IsAny<BillingEntityId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_billingProfile.BillingEntity!);

        _mockExchangeRateService
            .Setup(x => x.GetRateAsync(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversionRate(1m));

        _mockDocumentNumberRepository
            .Setup(x => x.NextInvoicePositionAsync(It.IsAny<BillingEntityId>(), It.IsAny<Year>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<Invoice>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Invoice>> operation, CancellationToken _) => operation());

        _mockDisplayPreferenceService
            .Setup(x => x.Resolve(It.IsAny<Locale?>(), It.IsAny<DisplayTimeZone?>(), It.IsAny<Organization?>()))
            .Returns(FakeDisplayPreferences.CreateValid());

        _mockDisplayPreferenceService
            .Setup(x => x.ForStatedAsync(It.IsAny<Locale?>(), It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeDisplayPreferences.CreateValid());

        _sut = new InvoiceService(
            _mockInvoiceRepository.Object,
            _mockInvoiceAdjustmentRepository.Object,
            _mockDocumentNumberRepository.Object,
            _mockUsageService.Object,
            _mockSubscriptionService.Object,
            _mockContractDiscountService.Object,
            _mockCommitmentService.Object,
            _mockBillingProfileService.Object,
            _mockBillingEntityService.Object,
            _mockExchangeRateService.Object,
            _mockDisplayPreferenceService.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext,
            _mockConfig.Object);
    }

    private Subscription GivenSubscriptionInEffect()
    {
        var subscription = FakeSubscription.CreateValid(Fixture);

        _mockSubscriptionService
            .Setup(x => x.GetInEffectAsync(It.IsAny<OrganizationId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        return subscription;
    }

    private void GivenMeteredUsage(int count, Action? onRead = null)
    {
        _mockUsageService
            .Setup(x => x.GroupByResourceAsync(It.IsAny<SumUsageByDateRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => onRead?.Invoke())
            .ReturnsAsync(new Dictionary<BillableResource, Count>
            {
                [BillableResource.RuleEvaluation] = new(count)
            });
    }

    private void GivenSavedInvoiceIsReturned(GenerateInvoiceRequest request, Subscription subscription, int count)
    {
        var invoiceId = FakeInvoiceId.CreateValid(Fixture);

        var lines = PricingCalculator.Calculate(
            new PriceInvoiceRequest
            {
                InvoiceId = invoiceId,
                PlanVersion = subscription.PlanVersion!,
                Quantities = new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(count) },
                Discounts = [],
                Adjustments = [],
                Commitment = null,
                Currency = _billingProfile.Currency,
                ConversionRate = new ConversionRate(1m),
                TaxDetermination = FakeTaxDetermination.CreateValid(Fixture),
                Translator = FakeDisplayPreferences.Translator()
            },
            MockTimeProvider);

        _mockInvoiceRepository
            .Setup(x => x.GetAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(InvoiceFactory.Create(
                request, invoiceId, subscription.PlanVersion!, _billingProfile, _billingProfile.BillingEntity!,
                new ConversionRate(1m), FakeTaxDetermination.CreateValid(Fixture), lines,
                FakeDisplayPreferences.CreateValid().Locale, MockTimeProvider));
    }

    [Fact]
    public async Task GenerateAsync_CalledFromTheSandbox_ReadsUsageAsLive()
    {
        // Arrange
        TestModeContext.Current = Mode.Test;

        var modeDuringRead = Mode.Test;

        var subscription = GivenSubscriptionInEffect();
        var generateInvoiceRequest = FakeGenerateInvoiceRequest.CreateValid(Fixture) with
        {
            Month = new Month(1),
            Year = new Year(2020)
        };

        GivenMeteredUsage(10000, () => modeDuringRead = TestModeContext.CurrentOrLive);
        GivenSavedInvoiceIsReturned(generateInvoiceRequest, subscription, 10000);

        // Act
        await _sut.GenerateAsync(generateInvoiceRequest, CancellationToken.None);

        // Assert
        modeDuringRead.Should().Be(Mode.Live);
    }

    [Fact]
    public async Task GenerateAsync_CalledFromTheSandbox_LeavesTheCallerInTheSandbox()
    {
        // Arrange
        TestModeContext.Current = Mode.Test;

        var subscription = GivenSubscriptionInEffect();
        var generateInvoiceRequest = FakeGenerateInvoiceRequest.CreateValid(Fixture) with
        {
            Month = new Month(1),
            Year = new Year(2020)
        };

        GivenMeteredUsage(1);
        GivenSavedInvoiceIsReturned(generateInvoiceRequest, subscription, 1);

        // Act
        await _sut.GenerateAsync(generateInvoiceRequest, CancellationToken.None);

        // Assert
        TestModeContext.Current.Should().Be(Mode.Test);
    }

    [Fact]
    public async Task GenerateAsync_GivenGenerateInvoiceRequest_CreatesInvoiceBasedOnUsage()
    {
        // Arrange
        var subscription = GivenSubscriptionInEffect();
        var costPerRequest = subscription.PlanVersion!.CostPerRequest;

        var generateInvoiceRequest = FakeGenerateInvoiceRequest.CreateValid(Fixture) with
        {
            Month = new Month(1),
            Year = new Year(2020)
        };

        GivenMeteredUsage(10000);
        GivenSavedInvoiceIsReturned(generateInvoiceRequest, subscription, 10000);

        // Act
        var invoice = await _sut.GenerateAsync(generateInvoiceRequest, CancellationToken.None);

        // Assert
        invoice.Amount.Should().Be(new InvoiceAmount(10000 * costPerRequest));
        invoice.InvoiceStatus.Should().Be(InvoiceStatus.Draft);
        invoice.InvoiceNumber.Should().BeNull();
        invoice.IssuedAt.Should().BeNull();
        invoice.DueAt.Should().BeNull();
        invoice.PlanVersionId.Should().Be(subscription.PlanVersionId);
        invoice.CostPerRequest.Should().Be(costPerRequest);
        invoice.OrganizationId.Should().Be(generateInvoiceRequest.OrganizationId);
        invoice.Year.Should().Be(generateInvoiceRequest.Year);
        invoice.Month.Should().Be(generateInvoiceRequest.Month);
        invoice.Organization.Should().BeNull();
        invoice.CreatedByUserId.Should().Be(generateInvoiceRequest.CreatedByUserId);
        invoice.IsDeleted.Should().Be(new IsDeleted(false));
        invoice.DeletedAt.Should().BeNull();
        invoice.IsProcessed.Should().Be(new IsProcessed(false));
        invoice.ProcessedAt.Should().BeNull();

        invoice.Lines.Sum(x => x.Amount).Should().Be(invoice.Amount);

        _mockSubscriptionService.Verify(x => x.GetInEffectAsync(It.IsAny<OrganizationId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUsageService.Verify(x => x.GroupByResourceAsync(It.IsAny<SumUsageByDateRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockInvoiceRepository.Verify(x => x.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockInvoiceRepository.Verify(x => x.GetAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_GivenBilledMonth_ReadsEveryDatedTermAtTheEndOfThatMonth()
    {
        // Arrange
        var subscription = GivenSubscriptionInEffect();
        var generateInvoiceRequest = FakeGenerateInvoiceRequest.CreateValid(Fixture) with
        {
            Month = new Month(2),
            Year = new Year(2024)
        };

        GivenMeteredUsage(1);
        GivenSavedInvoiceIsReturned(generateInvoiceRequest, subscription, 1);

        // Act
        await _sut.GenerateAsync(generateInvoiceRequest, CancellationToken.None);

        // Assert
        _mockSubscriptionService.Verify(
            x => x.GetInEffectAsync(
                It.IsAny<OrganizationId>(),
                It.Is<DateTimeOffset>(asOf => asOf.Year == 2024 && asOf.Month == 2 && asOf.Day == 29),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockContractDiscountService.Verify(
            x => x.GetInEffectAsync(
                It.IsAny<OrganizationId>(),
                It.Is<DateTimeOffset>(asOf => asOf.Year == 2024 && asOf.Month == 2 && asOf.Day == 29),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockCommitmentService.Verify(
            x => x.GetInEffectAsync(
                It.IsAny<OrganizationId>(),
                It.Is<DateTimeOffset>(asOf => asOf.Year == 2024 && asOf.Month == 2 && asOf.Day == 29),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_GivenUnappliedAdjustments_StampsThemWithTheInvoiceThatCarriedThem()
    {
        // Arrange
        var subscription = GivenSubscriptionInEffect();
        var generateInvoiceRequest = FakeGenerateInvoiceRequest.CreateValid(Fixture) with
        {
            Month = new Month(1),
            Year = new Year(2020)
        };

        var adjustment = FakeInvoiceAdjustment.CreateValid(Fixture) with { AppliedToInvoiceId = null };

        _mockInvoiceAdjustmentRepository
            .Setup(x => x.GetUnappliedAsync(It.IsAny<OrganizationId>(), It.IsAny<Month>(), It.IsAny<Year>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([adjustment]);

        GivenMeteredUsage(100);
        GivenSavedInvoiceIsReturned(generateInvoiceRequest, subscription, 100);

        // Act
        await _sut.GenerateAsync(generateInvoiceRequest, CancellationToken.None);

        // Assert
        _mockInvoiceAdjustmentRepository.Verify(
            x => x.ApplyAsync(adjustment.InvoiceAdjustmentId, It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_GivenNoPlanInEffect_Throws()
    {
        // Arrange
        var generateInvoiceRequest = FakeGenerateInvoiceRequest.CreateValid(Fixture) with
        {
            Month = new Month(1),
            Year = new Year(2020)
        };

        _mockSubscriptionService
            .Setup(x => x.GetInEffectAsync(It.IsAny<OrganizationId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        // Act
        var act = async () => await _sut.GenerateAsync(generateInvoiceRequest, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NoPlanInEffectException>();

        _mockInvoiceRepository.Verify(x => x.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_GivenASubscriptionWithoutItsPlanVersion_Throws()
    {
        // Arrange
        var generateInvoiceRequest = FakeGenerateInvoiceRequest.CreateValid(Fixture) with
        {
            Month = new Month(1),
            Year = new Year(2020)
        };

        _mockSubscriptionService
            .Setup(x => x.GetInEffectAsync(It.IsAny<OrganizationId>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeSubscription.CreateValid(Fixture) with { PlanVersion = null });

        // Act
        var act = async () => await _sut.GenerateAsync(generateInvoiceRequest, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NoPlanInEffectException>();

        _mockInvoiceRepository.Verify(x => x.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_GivenInvoice_ReleasesTheAdjustmentsItCarried()
    {
        // Arrange
        var invoiceId = GivenStoredInvoice(InvoiceStatus.Draft).InvoiceId;

        // Act
        await _sut.DeleteAsync(invoiceId, CancellationToken.None);

        // Assert
        _mockInvoiceRepository.Verify(x => x.DeleteAsync(invoiceId, It.IsAny<CancellationToken>()), Times.Once);
        _mockInvoiceAdjustmentRepository.Verify(x => x.ReleaseAsync(invoiceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecoverAsync_GivenInvoice_ReappliesTheAdjustmentsItsLinesName()
    {
        // Arrange
        var invoiceId = FakeInvoiceId.CreateValid(Fixture);

        // Act
        await _sut.RecoverAsync(invoiceId, CancellationToken.None);

        // Assert
        _mockInvoiceRepository.Verify(x => x.RecoverAsync(invoiceId, It.IsAny<CancellationToken>()), Times.Once);
        _mockInvoiceAdjustmentRepository.Verify(x => x.ReapplyAsync(invoiceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportAsync_GivenInvoice_RendersTheDocumentFromItsLines()
    {
        // Arrange
        var invoice = FakeInvoice.CreateValid(Fixture) with
        {
            Month = new Month(1),
            Year = new Year(2020),
            Lines = [FakeInvoiceLine.CreateValid(Fixture)]
        };

        _mockInvoiceRepository
            .Setup(x => x.GetAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        // Act
        var exportedInvoice = await _sut.ExportAsync(invoice.InvoiceId, CancellationToken.None);

        // Assert
        exportedInvoice.Invoice.Should().Be(invoice);
        exportedInvoice.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_GivenNoBillingProfile_Throws()
    {
        // Arrange
        GivenSubscriptionInEffect();

        var generateInvoiceRequest = FakeGenerateInvoiceRequest.CreateValid(Fixture) with
        {
            Month = new Month(1),
            Year = new Year(2020)
        };

        _mockBillingProfileService
            .Setup(x => x.FindAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingProfile?)null);

        // Act
        var act = async () => await _sut.GenerateAsync(generateInvoiceRequest, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NoBillingProfileException>();

        _mockInvoiceRepository.Verify(x => x.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_GivenAPlanInAnotherCurrency_ReadsTheRateAtTheEndOfTheBilledMonth()
    {
        // Arrange
        GivenSubscriptionInEffect();

        var generateInvoiceRequest = FakeGenerateInvoiceRequest.CreateValid(Fixture) with
        {
            Month = new Month(2),
            Year = new Year(2024)
        };

        GivenMeteredUsage(1);
        GivenSavedInvoiceIsReturned(generateInvoiceRequest, GivenSubscriptionInEffect(), 1);

        // Act
        await _sut.GenerateAsync(generateInvoiceRequest, CancellationToken.None);

        // Assert
        _mockExchangeRateService.Verify(
            x => x.GetRateAsync(
                It.IsAny<Currency>(),
                It.IsAny<Currency>(),
                It.Is<DateTimeOffset>(asOf => asOf.Year == 2024 && asOf.Month == 2 && asOf.Day == 29),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IssueAsync_GivenADraft_TakesTheNextPositionAndStampsTheDocument()
    {
        // Arrange
        var invoice = GivenStoredInvoice(InvoiceStatus.Draft);

        // Act
        await _sut.IssueAsync(invoice.InvoiceId, CancellationToken.None);

        // Assert
        _mockDocumentNumberRepository.Verify(
            x => x.NextInvoicePositionAsync(It.IsAny<BillingEntityId>(), It.IsAny<Year>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockInvoiceRepository.Verify(
            x => x.IssueAsync(
                invoice.InvoiceId,
                It.Is<InvoiceNumber>(number => ((string)number).EndsWith("-000001", StringComparison.Ordinal)),
                It.IsAny<IssuedAt>(),
                It.Is<DueAt>(dueAt => dueAt == MockTimeProvider.GetUtcNow().AddDays(invoice.PaymentTerms)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IssueAsync_GivenAnIssuedInvoice_Throws()
    {
        // Arrange
        var invoice = GivenStoredInvoice(InvoiceStatus.Issued);

        // Act
        var act = async () => await _sut.IssueAsync(invoice.InvoiceId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvoiceIssuedException>();

        _mockDocumentNumberRepository.Verify(
            x => x.NextInvoicePositionAsync(It.IsAny<BillingEntityId>(), It.IsAny<Year>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_GivenAnIssuedInvoice_Throws()
    {
        // Arrange
        var invoice = GivenStoredInvoice(InvoiceStatus.Issued);

        // Act
        var act = async () => await _sut.DeleteAsync(invoice.InvoiceId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvoiceIssuedException>();

        _mockInvoiceRepository.Verify(x => x.DeleteAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_GivenADraft_Throws()
    {
        // Arrange
        var invoice = GivenStoredInvoice(InvoiceStatus.Draft);

        // Act
        var act = async () => await _sut.ProcessAsync(invoice.InvoiceId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvoiceNotIssuedException>();

        _mockInvoiceRepository.Verify(x => x.ProcessAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private Invoice GivenStoredInvoice(InvoiceStatus invoiceStatus)
    {
        var invoice = FakeInvoice.CreateValid(Fixture) with
        {
            InvoiceStatus = invoiceStatus,
            InvoiceNumber = invoiceStatus is InvoiceStatus.Draft ? null : FakeInvoiceNumber.CreateValid(Fixture),
            BillingEntityId = _billingProfile.BillingEntityId,
            PaymentTerms = new PaymentTerms(28)
        };

        _mockInvoiceRepository
            .Setup(x => x.GetAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        return invoice;
    }
}
