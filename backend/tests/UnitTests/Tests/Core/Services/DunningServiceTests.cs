using Core.Configs;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Billing;
using Core.Models.Dunning;
using Core.Models.Invoices;
using Core.Models.Payments;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;

namespace UnitTests.Tests.Core.Services;

public class DunningServiceTests : BaseTestClass
{
    private readonly Mock<IDunningRepository> _mockDunningRepository = new();
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepository = new();
    private readonly Mock<IPaymentService> _mockPaymentService = new();
    private readonly Mock<IBillingProfileService> _mockBillingProfileService = new();
    private readonly Mock<IDisplayPreferenceService> _mockDisplayPreferenceService = new();
    private readonly Mock<IEmailSender> _mockEmailSender = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IOptions<AppConfig>> _mockConfig = new();
    private readonly Invoice _invoice;
    private readonly DunningService _sut;

    public DunningServiceTests()
    {
        _invoice = FakeInvoice.CreateValid(Fixture) with
        {
            InvoiceStatus = InvoiceStatus.Issued,
            Amount = new InvoiceAmount(100m),
            DueAt = new DueAt(MockTimeProvider.GetUtcNow().AddDays(-10))
        };

        _mockConfig.SetupGet(x => x.Value).Returns(FakeAppConfig.CreateValid(Fixture));

        _mockInvoiceRepository
            .Setup(x => x.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_invoice]);

        _mockInvoiceRepository
            .Setup(x => x.GetAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_invoice);

        _mockBillingProfileService
            .Setup(x => x.FindAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingProfile?)null);

        _sut = new DunningService(
            _mockDunningRepository.Object,
            _mockInvoiceRepository.Object,
            _mockPaymentService.Object,
            _mockBillingProfileService.Object,
            _mockDisplayPreferenceService.Object,
            _mockEmailSender.Object,
            _mockUnitOfWork.Object,
            TestModeContext,
            MockTimeProvider,
            _mockConfig.Object,
            MockLogger.Object);
    }

    private void SetUpSettlement(bool isSettled, decimal outstanding)
    {
        _mockPaymentService
            .Setup(x => x.GetSettlementAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceSettlement
            {
                InvoiceId = _invoice.InvoiceId,
                Amount = new InvoiceAmount(100m),
                AllocatedAmount = new InvoiceAmount(100m - outstanding),
                CreditedAmount = new InvoiceAmount(0m),
                OutstandingAmount = new OutstandingAmount(outstanding),
                IsSettled = new IsSettled(isSettled)
            });
    }

    [Fact]
    public async Task ReconcileAsync_GivenAnOverdueInvoiceWithNoLadder_OpensOneInGraceSayingNothing()
    {
        // Arrange
        SetUpSettlement(isSettled: false, outstanding: 100m);

        // Act
        await _sut.ReconcileAsync(CancellationToken.None);

        // Assert
        _mockDunningRepository.Verify(
            x => x.AddAsync(
                It.Is<DunningState>(y => y.Stage == DunningStage.Grace && y.LastNoticedAt == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_GivenAnInvoiceAlreadyBeingChased_DoesNotOpenASecondLadder()
    {
        // Arrange
        SetUpSettlement(isSettled: false, outstanding: 100m);

        _mockDunningRepository
            .Setup(x => x.FindOpenAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenLadder(DunningStage.FirstNotice));

        // Act
        await _sut.ReconcileAsync(CancellationToken.None);

        // Assert
        _mockDunningRepository.Verify(
            x => x.AddAsync(It.IsAny<DunningState>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReconcileAsync_GivenTheInvoiceIsNowSettled_ClosesTheLadder()
    {
        // Arrange
        SetUpSettlement(isSettled: true, outstanding: 0m);

        _mockDunningRepository
            .Setup(x => x.FindOpenAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OpenLadder(DunningStage.SecondNotice));

        // Act
        await _sut.ReconcileAsync(CancellationToken.None);

        // Assert
        _mockDunningRepository.Verify(
            x => x.ResolveAsync(It.IsAny<DunningStateId>(), It.IsAny<ResolvedAt>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AdvanceAsync_GivenTheInvoiceWasPaidSinceTheLadderOpened_ClosesItWithoutAdvancing()
    {
        // Arrange
        SetUpSettlement(isSettled: true, outstanding: 0m);

        _mockDunningRepository
            .Setup(x => x.ListDueAsync(It.IsAny<NextAttemptAt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([OpenLadder(DunningStage.Grace)]);

        // Act
        await _sut.AdvanceAsync(CancellationToken.None);

        // Assert
        _mockDunningRepository.Verify(
            x => x.ResolveAsync(It.IsAny<DunningStateId>(), It.IsAny<ResolvedAt>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockDunningRepository.Verify(
            x => x.AdvanceAsync(It.IsAny<DunningState>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdvanceAsync_GivenALadderInGrace_MovesItOneRung()
    {
        // Arrange
        SetUpSettlement(isSettled: false, outstanding: 100m);

        _mockDunningRepository
            .Setup(x => x.ListDueAsync(It.IsAny<NextAttemptAt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([OpenLadder(DunningStage.Grace)]);

        // Act
        await _sut.AdvanceAsync(CancellationToken.None);

        // Assert
        _mockDunningRepository.Verify(
            x => x.AdvanceAsync(
                It.Is<DunningState>(y => y.Stage == DunningStage.FirstNotice && y.LastNoticedAt != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AdvanceAsync_GivenALadderAtTheFinalNotice_ProposesRestrictionAndRecordsWhen()
    {
        // Arrange
        SetUpSettlement(isSettled: false, outstanding: 100m);

        _mockDunningRepository
            .Setup(x => x.ListDueAsync(It.IsAny<NextAttemptAt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([OpenLadder(DunningStage.FinalNotice)]);

        // Act
        await _sut.AdvanceAsync(CancellationToken.None);

        // Assert
        _mockDunningRepository.Verify(
            x => x.AdvanceAsync(
                It.Is<DunningState>(y =>
                    y.Stage == DunningStage.RestrictionProposed && y.RestrictionProposedAt != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AdvanceAsync_GivenALadderAtTheTop_LeavesItAloneAndCollectsNothing()
    {
        // Arrange
        SetUpSettlement(isSettled: false, outstanding: 100m);

        _mockDunningRepository
            .Setup(x => x.ListDueAsync(It.IsAny<NextAttemptAt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([OpenLadder(DunningStage.RestrictionProposed)]);

        // Act
        await _sut.AdvanceAsync(CancellationToken.None);

        // Assert
        _mockDunningRepository.Verify(
            x => x.AdvanceAsync(It.IsAny<DunningState>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockPaymentService.Verify(
            x => x.CollectAsync(It.IsAny<CollectPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdvanceAsync_GivenCollectionThrows_StillAdvancesTheRung()
    {
        // Arrange
        SetUpSettlement(isSettled: false, outstanding: 100m);

        _mockConfig
            .SetupGet(x => x.Value)
            .Returns(FakeAppConfig.CreateValid(Fixture) with { PaymentConfig = new PaymentConfig { CollectionEnabled = true } });

        _mockDunningRepository
            .Setup(x => x.ListDueAsync(It.IsAny<NextAttemptAt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([OpenLadder(DunningStage.Grace)]);

        _mockPaymentService
            .Setup(x => x.CollectAsync(It.IsAny<CollectPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("card declined"));

        // Act
        var act = async () => await _sut.AdvanceAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _mockDunningRepository.Verify(
            x => x.AdvanceAsync(
                It.Is<DunningState>(y => y.Stage == DunningStage.FirstNotice),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private DunningState OpenLadder(DunningStage stage)
    {
        var now = MockTimeProvider.GetUtcNow();

        return new DunningState
        {
            DunningStateId = new DunningStateId(Guid.NewGuid()),
            InvoiceId = _invoice.InvoiceId,
            Invoice = null,
            OrganizationId = _invoice.OrganizationId,
            Stage = stage,
            NextAttemptAt = new NextAttemptAt(now),
            LastNoticedAt = null,
            CollectionAttempts = new Attempts(0),
            LastAttemptedAt = null,
            RestrictionProposedAt = null,
            ResolvedAt = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };
    }
}
