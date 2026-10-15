using Core.Configs;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Dunning;
using Core.Models.Organizations;
using Core.Models.Revenue;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Organizations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class RevenueServiceTests : BaseTestClass
{
    private static readonly Month March = new(3);
    private static readonly Year Year = new(2026);

    private readonly Mock<IInvoiceRepository> _mockInvoiceRepository = new();
    private readonly Mock<ICreditNoteRepository> _mockCreditNoteRepository = new();
    private readonly Mock<IDunningRepository> _mockDunningRepository = new();
    private readonly Mock<IUsageRepository> _mockUsageRepository = new();
    private readonly Mock<IAvailabilityWindowRepository> _mockAvailabilityWindowRepository = new();
    private readonly Mock<IOrganizationRepository> _mockOrganizationRepository = new();
    private readonly Mock<IServiceCostRateService> _mockServiceCostRateService = new();
    private readonly Mock<IExchangeRateService> _mockExchangeRateService = new();
    private readonly Mock<IBillingEntityService> _mockBillingEntityService = new();
    private readonly Mock<IOptions<AppConfig>> _mockConfig = new();
    private readonly RevenueService _sut;

    public RevenueServiceTests()
    {
        _mockConfig.SetupGet(x => x.Value).Returns(FakeAppConfig.CreateValid(Fixture));

        _mockExchangeRateService
            .Setup(x => x.GetRateAsync(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversionRate(1m));

        _mockInvoiceRepository
            .Setup(x => x.GetBilledMonthsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockUsageRepository
            .Setup(x => x.GroupByOrganizationAndResourceAsync(It.IsAny<PeriodStart>(), It.IsAny<PeriodEnd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockUsageRepository
            .Setup(x => x.SumByOrganizationAsync(It.IsAny<PeriodStart>(), It.IsAny<PeriodEnd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockUsageRepository
            .Setup(x => x.GetLastMeteredAtAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockAvailabilityWindowRepository
            .Setup(x => x.SumByOrganizationAsync(It.IsAny<PeriodStart>(), It.IsAny<PeriodEnd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockServiceCostRateService
            .Setup(x => x.GetInEffectAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockDunningRepository
            .Setup(x => x.ListOpenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockDunningRepository
            .Setup(x => x.ListOpenedBetweenAsync(It.IsAny<PeriodStart>(), It.IsAny<PeriodEnd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockOrganizationRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockOrganizationRepository
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<OrganizationId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _sut = new RevenueService(
            _mockInvoiceRepository.Object,
            _mockCreditNoteRepository.Object,
            _mockDunningRepository.Object,
            _mockUsageRepository.Object,
            _mockAvailabilityWindowRepository.Object,
            _mockOrganizationRepository.Object,
            _mockServiceCostRateService.Object,
            _mockExchangeRateService.Object,
            _mockBillingEntityService.Object,
            TestModeContext,
            MockTimeProvider,
            _mockConfig.Object);
    }

    [Fact]
    public async Task GetConcentrationAsync_SubtractsWhatWasCreditedFromWhatWasBilled()
    {
        // Arrange
        BilledMonths(Billed("Acme", 1000m, credited: 250m));

        // Act
        var concentration = await _sut.GetConcentrationAsync(ConcentrationRequest(), CancellationToken.None);

        // Assert
        ((decimal)concentration.TotalRevenue).Should().Be(750m);
    }

    [Fact]
    public async Task GetConcentrationAsync_RanksCustomersLargestFirstAndSharesThemOutOfTheTotal()
    {
        // Arrange
        BilledMonths(Billed("Acme", 250m), Billed("Borden", 750m));

        // Act
        var concentration = await _sut.GetConcentrationAsync(ConcentrationRequest(), CancellationToken.None);

        // Assert
        concentration.Customers.Should().HaveCount(2);
        concentration.Customers[0].OrganizationName.ToString().Should().Be("Borden");
        ((decimal)concentration.Customers[0].Share).Should().Be(75m);
        ((decimal)concentration.LargestCustomerShare!.Value).Should().Be(75m);
    }

    [Fact]
    public async Task GetConcentrationAsync_LeavesOutACustomerWhoseMonthWasNetCredited()
    {
        // Arrange
        BilledMonths(Billed("Acme", 1000m), Billed("Borden", 100m, credited: 400m));

        // Act
        var concentration = await _sut.GetConcentrationAsync(ConcentrationRequest(), CancellationToken.None);

        // Assert
        ((int)concentration.CustomerCount).Should().Be(1);
        ((decimal)concentration.TotalRevenue).Should().Be(1000m);
        ((decimal)concentration.Customers[0].Share).Should().Be(100m);
    }

    [Fact]
    public async Task GetConcentrationAsync_GivenOneCustomer_ReportsAnIndexOfOne()
    {
        // Arrange
        BilledMonths(Billed("Acme", 1000m));

        // Act
        var concentration = await _sut.GetConcentrationAsync(ConcentrationRequest(), CancellationToken.None);

        // Assert
        ((decimal)concentration.ConcentrationIndex!.Value).Should().Be(1m);
    }

    [Fact]
    public async Task GetConcentrationAsync_GivenAMonthThatBilledNothing_ReportsNoSharesRatherThanZero()
    {
        // Act
        var concentration = await _sut.GetConcentrationAsync(ConcentrationRequest(), CancellationToken.None);

        // Assert
        concentration.LargestCustomerShare.Should().BeNull();
        concentration.ConcentrationIndex.Should().BeNull();
        ((int)concentration.CustomerCount).Should().Be(0);
    }

    [Fact]
    public async Task ListMarginsAsync_TakesTheCostOfEveryKindOfWorkOffWhatWasBilled()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        BilledMonths(Billed("Acme", 1000m, organizationId));
        Usage((organizationId, BillableResource.RuleEvaluation, 10_000));
        CostRates((BillableResource.RuleEvaluation, 0.02m));

        // Act
        var margins = await _sut.ListMarginsAsync(MarginRequest(), CancellationToken.None);

        // Assert
        margins.Should().ContainSingle();
        ((decimal)margins[0].Cost).Should().Be(200m);
        ((decimal)margins[0].Margin).Should().Be(800m);
        ((decimal)margins[0].MarginRate!.Value).Should().Be(80m);
        ((bool)margins[0].CostsComplete).Should().BeTrue();
    }

    [Fact]
    public async Task ListMarginsAsync_GivenWorkNobodyHasCosted_KeepsTheLineAndSaysTheCostsAreIncomplete()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        BilledMonths(Billed("Acme", 1000m, organizationId));
        Usage(
            (organizationId, BillableResource.RuleEvaluation, 10_000),
            (organizationId, BillableResource.Backtest, 5));
        CostRates((BillableResource.RuleEvaluation, 0.02m));

        // Act
        var margins = await _sut.ListMarginsAsync(MarginRequest(), CancellationToken.None);

        // Assert
        ((bool)margins[0].CostsComplete).Should().BeFalse();
        margins[0].Lines.Should().HaveCount(2);
        margins[0].Lines.Should().ContainSingle(x => x.BillableResource == BillableResource.Backtest && !x.UnitCost.HasValue);
        ((decimal)margins[0].Cost).Should().Be(200m);
    }

    [Fact]
    public async Task ListMarginsAsync_GivenACustomerWhoDidWorkAndWasNeverBilled_StillGivesThemARow()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        Usage((organizationId, BillableResource.RuleEvaluation, 1_000));
        CostRates((BillableResource.RuleEvaluation, 0.05m));

        _mockOrganizationRepository
            .Setup(x => x.ListByIdsAsync(It.IsAny<IReadOnlyCollection<OrganizationId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Organization("Corvid", organizationId)]);

        // Act
        var margins = await _sut.ListMarginsAsync(MarginRequest(), CancellationToken.None);

        // Assert
        margins.Should().ContainSingle();
        ((decimal)margins[0].Revenue).Should().Be(0m);
        ((decimal)margins[0].Margin).Should().Be(-50m);

        margins[0].MarginRate.Should().BeNull();
        margins[0].OrganizationName.ToString().Should().Be("Corvid");
    }

    [Fact]
    public async Task ListMarginsAsync_ReturnsTheWorstAccountsFirst()
    {
        // Arrange
        var profitable = Guid.NewGuid();
        var lossMaking = Guid.NewGuid();

        BilledMonths(Billed("Acme", 1000m, profitable), Billed("Borden", 10m, lossMaking));
        Usage(
            (profitable, BillableResource.RuleEvaluation, 1_000),
            (lossMaking, BillableResource.RuleEvaluation, 10_000));
        CostRates((BillableResource.RuleEvaluation, 0.02m));

        // Act
        var margins = await _sut.ListMarginsAsync(MarginRequest(), CancellationToken.None);

        // Assert
        margins[0].OrganizationName.ToString().Should().Be("Borden");
        ((decimal)margins[0].Margin).Should().Be(-190m);
    }

    [Fact]
    public async Task GetRecoveryAsync_ReportsTheShareOfTheCohortThatStoppedBeingLate()
    {
        // Arrange
        Ladders(
            Ladder(DunningStage.FirstNotice, resolvedAfterDays: 4, netAmount: 500m),
            Ladder(DunningStage.FinalNotice, resolvedAfterDays: 20, netAmount: 300m),
            Ladder(DunningStage.RestrictionProposed, resolvedAfterDays: null, netAmount: 900m));

        // Act
        var recovery = await _sut.GetRecoveryAsync(RecoveryRequest(), CancellationToken.None);

        // Assert
        ((int)recovery.LaddersOpened).Should().Be(3);
        ((int)recovery.LaddersResolved).Should().Be(2);
        ((decimal)recovery.RecoveryRate!.Value).Should().BeApproximately(66.67m, 0.01m);
        ((decimal)recovery.RecoveredRevenue).Should().Be(800m);
        ((decimal)recovery.OutstandingRevenue).Should().Be(900m);
        ((int)recovery.MeanDaysToRecover!.Value).Should().Be(12);
    }

    [Fact]
    public async Task GetRecoveryAsync_BreaksTheRecoveriesDownByTheRungTheyWereStandingOn()
    {
        // Arrange
        Ladders(
            Ladder(DunningStage.Grace, resolvedAfterDays: 1, netAmount: 100m),
            Ladder(DunningStage.Grace, resolvedAfterDays: 2, netAmount: 100m),
            Ladder(DunningStage.FinalNotice, resolvedAfterDays: 30, netAmount: 100m));

        // Act
        var recovery = await _sut.GetRecoveryAsync(RecoveryRequest(), CancellationToken.None);

        // Assert
        recovery.ResolvedByStage.Should().HaveCount(2);
        recovery.ResolvedByStage.Should().ContainSingle(x => x.Stage == DunningStage.Grace && x.Resolved == 2);
    }

    [Fact]
    public async Task GetRecoveryAsync_GivenAWindowThatOpenedNoLadders_ReportsNoRateRatherThanZero()
    {
        // Act
        var recovery = await _sut.GetRecoveryAsync(RecoveryRequest(), CancellationToken.None);

        // Assert
        ((int)recovery.LaddersOpened).Should().Be(0);
        recovery.RecoveryRate.Should().BeNull();
        recovery.MeanDaysToRecover.Should().BeNull();
    }

    [Fact]
    public async Task ListHealthAsync_ScoresEveryOrganizationAndReturnsTheWorstFirst()
    {
        // Arrange
        var quiet = Guid.NewGuid();
        var busy = Guid.NewGuid();

        _mockOrganizationRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Organization("Quiet", quiet), Organization("Busy", busy)]);

        var now = MockTimeProvider.GetUtcNow();

        _mockUsageRepository
            .Setup(x => x.GetLastMeteredAtAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, DateTimeOffset>
            {
                [quiet] = now.AddDays(-45),
                [busy] = now.AddHours(-1)
            });

        // Act
        var health = await _sut.ListHealthAsync(
            new ListCustomerHealthRequest { WindowDays = new DaysElapsed(30) }, CancellationToken.None);

        // Assert
        health.Should().HaveCount(2);
        health[0].OrganizationName.ToString().Should().Be("Quiet");
        ((int)health[0].DaysSinceLastUsage!.Value).Should().Be(45);
        health[0].Signals.Should().Contain(x => x.Kind == HealthSignalKind.Silence);
        ((int)health[0].HealthScore).Should().BeLessThan(health[1].HealthScore);
    }

    private void BilledMonths(params BilledMonth[] billedMonths)
    {
        _mockInvoiceRepository
            .Setup(x => x.GetBilledMonthsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(billedMonths.ToList());
    }

    private void Usage(params (Guid Organization, BillableResource Resource, int Count)[] usage)
    {
        _mockUsageRepository
            .Setup(x => x.GroupByOrganizationAndResourceAsync(It.IsAny<PeriodStart>(), It.IsAny<PeriodEnd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usage.Select(x => new OrganizationResourceUsage
            {
                OrganizationId = new OrganizationId(x.Organization),
                BillableResource = x.Resource,
                Count = new Count(x.Count)
            }).ToList());
    }

    private void CostRates(params (BillableResource Resource, decimal UnitCost)[] rates)
    {
        _mockServiceCostRateService
            .Setup(x => x.GetInEffectAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates.ToDictionary(x => x.Resource, x => new ServiceCostRate
            {
                ServiceCostRateId = new ServiceCostRateId(Guid.NewGuid()),
                BillableResource = x.Resource,
                UnitCost = new UnitCost(x.UnitCost),
                Currency = Currency.Gbp,
                EffectiveFrom = new EffectiveFrom(MockTimeProvider.GetUtcNow()),
                CreatedByUserId = null,
                CreatedAt = FakeCreatedAt.CreateValid(Fixture),
                UpdatedAt = FakeUpdatedAt.CreateValid(Fixture)
            }));
    }

    private void Ladders(params DunningState[] ladders)
    {
        _mockDunningRepository
            .Setup(x => x.ListOpenedBetweenAsync(It.IsAny<PeriodStart>(), It.IsAny<PeriodEnd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ladders.ToList());
    }

    private DunningState Ladder(DunningStage stage, int? resolvedAfterDays, decimal netAmount)
    {
        var openedAt = MockTimeProvider.GetUtcNow();

        var invoice = FakeInvoice.CreateValid(Fixture) with
        {
            Currency = Currency.Gbp,
            NetAmount = new NetAmount(netAmount),
            Month = March,
            Year = Year
        };

        return new DunningState
        {
            DunningStateId = new DunningStateId(Guid.NewGuid()),
            InvoiceId = invoice.InvoiceId,
            Invoice = invoice,
            OrganizationId = invoice.OrganizationId,
            Stage = stage,
            NextAttemptAt = new NextAttemptAt(openedAt),
            LastNoticedAt = null,
            CollectionAttempts = new Attempts(0),
            LastAttemptedAt = null,
            RestrictionProposedAt = stage == DunningStage.RestrictionProposed ? new ProposedAt(openedAt) : null,
            ResolvedAt = resolvedAfterDays.HasValue ? new ResolvedAt(openedAt.AddDays(resolvedAfterDays.Value)) : null,
            CreatedAt = new CreatedAt(openedAt),
            UpdatedAt = new UpdatedAt(openedAt)
        };
    }

    private Organization Organization(string name, Guid organizationId)
    {
        return FakeOrganization.CreateValid(Fixture) with
        {
            OrganizationId = new OrganizationId(organizationId),
            FriendlyName = new FriendlyName(name)
        };
    }

    private static BilledMonth Billed(string name, decimal netAmount, Guid? organizationId = null, decimal credited = 0m)
    {
        return new BilledMonth
        {
            OrganizationId = new OrganizationId(organizationId ?? Guid.NewGuid()),
            OrganizationName = new FriendlyName(name),
            Month = March,
            Year = Year,
            Currency = Currency.Gbp,
            NetAmount = new NetAmount(netAmount),
            CreditedNetAmount = new NetAmount(credited)
        };
    }

    private static GetRevenueConcentrationRequest ConcentrationRequest()
    {
        return new GetRevenueConcentrationRequest { Month = March, Year = Year, TopCount = new Count(20) };
    }

    private static ListCustomerMarginRequest MarginRequest()
    {
        return new ListCustomerMarginRequest { Month = March, Year = Year };
    }

    private GetRecoveryStatisticRequest RecoveryRequest()
    {
        var now = MockTimeProvider.GetUtcNow();

        return new GetRecoveryStatisticRequest
        {
            PeriodStart = new PeriodStart(now.AddDays(-90)),
            PeriodEnd = new PeriodEnd(now)
        };
    }
}
