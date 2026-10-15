using Core.Configs;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Accounts;
using Core.Models.Invoices;
using Core.Models.Payments;
using Core.Models.Providers;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Billing;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;

namespace UnitTests.Tests.Core.Services;

public class PaymentServiceTests : BaseTestClass
{
    private readonly Mock<IPaymentRepository> _mockPaymentRepository = new();
    private readonly Mock<IAccountEntryRepository> _mockAccountEntryRepository = new();
    private readonly Mock<ICreditNoteRepository> _mockCreditNoteRepository = new();
    private readonly Mock<IInvoiceRepository> _mockInvoiceRepository = new();
    private readonly Mock<IBillingProfileService> _mockBillingProfileService = new();
    private readonly Mock<IBillingEntityService> _mockBillingEntityService = new();
    private readonly Mock<IDocumentNumberRepository> _mockDocumentNumberRepository = new();
    private readonly Mock<IDisplayPreferenceService> _mockDisplayPreferenceService = new();
    private readonly Mock<IPaymentProvider> _mockPaymentProvider = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IOptions<AppConfig>> _mockConfig = new();
    private readonly Invoice _invoice;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _invoice = FakeInvoice.CreateValid(Fixture) with
        {
            InvoiceStatus = InvoiceStatus.Issued,
            Currency = Currency.Gbp,
            Amount = new InvoiceAmount(100m),
            IsDeleted = new IsDeleted(false)
        };

        _mockConfig.SetupGet(x => x.Value).Returns(FakeAppConfig.CreateValid(Fixture));

        _mockInvoiceRepository
            .Setup(x => x.GetAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _invoice);

        _mockBillingProfileService
            .Setup(x => x.GetAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeBillingProfile.CreateValid(Fixture));

        _mockBillingProfileService
            .Setup(x => x.EnsureProviderCustomerAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderCustomerReference("cus_test"));

        _mockDocumentNumberRepository
            .Setup(x => x.NextReceiptPositionAsync(It.IsAny<BillingEntityId>(), It.IsAny<Year>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        SetUpSettlement(allocated: 0m, credited: 0m);

        _sut = new PaymentService(
            _mockPaymentRepository.Object,
            _mockAccountEntryRepository.Object,
            _mockCreditNoteRepository.Object,
            _mockInvoiceRepository.Object,
            _mockBillingProfileService.Object,
            _mockBillingEntityService.Object,
            _mockDocumentNumberRepository.Object,
            _mockDisplayPreferenceService.Object,
            _mockPaymentProvider.Object,
            _mockUnitOfWork.Object,
            TestModeContext,
            MockTimeProvider,
            _mockConfig.Object);
    }

    private void SetUpSettlement(decimal allocated, decimal credited)
    {
        _mockAccountEntryRepository
            .Setup(x => x.GetAllocatedAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceAmount(allocated));

        _mockCreditNoteRepository
            .Setup(x => x.GetCreditedAmountAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceAmount(credited));
    }

    [Fact]
    public async Task GetSettlementAsync_GivenAPartPaymentAndACredit_SubtractsBoth()
    {
        // Arrange
        SetUpSettlement(allocated: 30m, credited: 20m);

        // Act
        var settlement = await _sut.GetSettlementAsync(_invoice.InvoiceId, CancellationToken.None);

        // Assert
        ((decimal)settlement.OutstandingAmount).Should().Be(50m);
        ((bool)settlement.IsSettled).Should().BeFalse();
    }

    [Fact]
    public async Task GetSettlementAsync_GivenAnInvoiceCreditedInFull_IsSettled()
    {
        // Arrange
        SetUpSettlement(allocated: 0m, credited: 100m);

        // Act
        var settlement = await _sut.GetSettlementAsync(_invoice.InvoiceId, CancellationToken.None);

        // Assert
        ((bool)settlement.IsSettled).Should().BeTrue();
        ((decimal)settlement.OutstandingAmount).Should().Be(0m);
    }

    [Fact]
    public async Task GetSettlementAsync_GivenAnOverpayment_FloorsOutstandingAtZero()
    {
        // Arrange
        SetUpSettlement(allocated: 150m, credited: 0m);

        // Act
        var settlement = await _sut.GetSettlementAsync(_invoice.InvoiceId, CancellationToken.None);

        // Assert
        ((decimal)settlement.OutstandingAmount).Should().Be(0m);
        ((bool)settlement.IsSettled).Should().BeTrue();
    }

    [Fact]
    public async Task CollectAsync_GivenAnInvoiceWithNothingOutstanding_ThrowsAndNeverReachesTheProvider()
    {
        // Arrange
        SetUpSettlement(allocated: 100m, credited: 0m);

        // Act
        var act = async () => await _sut.CollectAsync(
            new CollectPaymentRequest { InvoiceId = _invoice.InvoiceId, PaymentMethodReference = null },
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvoiceSettledException>();

        _mockPaymentProvider.Verify(
            x => x.ChargeAsync(It.IsAny<ProviderChargeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CollectAsync_GivenNoPaymentMethodOnFile_Throws()
    {
        // Arrange
        _mockPaymentProvider
            .Setup(x => x.ListPaymentMethodsAsync(It.IsAny<ProviderCustomerReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var act = async () => await _sut.CollectAsync(
            new CollectPaymentRequest { InvoiceId = _invoice.InvoiceId, PaymentMethodReference = null },
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NoPaymentMethodException>();
    }

    [Fact]
    public async Task CollectAsync_GivenAPartlyCreditedInvoice_AsksTheProviderForWhatIsLeft()
    {
        // Arrange
        SetUpSettlement(allocated: 0m, credited: 40m);
        SetUpAProviderThatSettles();

        // Act
        await _sut.CollectAsync(
            new CollectPaymentRequest { InvoiceId = _invoice.InvoiceId, PaymentMethodReference = null },
            CancellationToken.None);

        // Assert
        _mockPaymentProvider.Verify(
            x => x.ChargeAsync(It.Is<ProviderChargeRequest>(y => y.Amount == 60m), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_GivenTheProviderReturnsPending_WritesThePaymentAndReceiptsNothing()
    {
        // Arrange
        SetUpAProvider(PaymentStatus.Pending);

        // Act
        var payment = await _sut.CollectAsync(
            new CollectPaymentRequest { InvoiceId = _invoice.InvoiceId, PaymentMethodReference = null },
            CancellationToken.None);

        // Assert
        _mockPaymentRepository.Verify(
            x => x.AddAsync(It.Is<Payment>(y => y.PaymentStatus == PaymentStatus.Pending), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockPaymentRepository.Verify(
            x => x.SettleAsync(It.IsAny<PaymentId>(), It.IsAny<SettledAt>(), It.IsAny<ReceiptNumber>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CollectAsync_GivenTheProviderSettles_ReceiptsTheCashAndAllocatesIt()
    {
        // Arrange
        SetUpAProviderThatSettles();

        // Act
        await _sut.CollectAsync(
            new CollectPaymentRequest { InvoiceId = _invoice.InvoiceId, PaymentMethodReference = null },
            CancellationToken.None);

        // Assert
        _mockAccountEntryRepository.Verify(
            x => x.AddManyAsync(
                It.Is<List<AccountEntry>>(y =>
                    y.Any(z => z.Reason == AccountEntryReason.Receipt && z.Amount == 100m) &&
                    y.Any(z => z.Reason == AccountEntryReason.Allocation && z.Amount == -100m)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleEventAsync_GivenAnEventWithNoPayment_IsIgnoredWithoutError()
    {
        // Arrange
        var providerEvent = Event(paymentReference: null, PaymentStatus.Settled);

        // Act
        var paymentId = await _sut.HandleEventAsync(providerEvent, CancellationToken.None);

        // Assert
        paymentId.Should().BeNull();
    }

    [Fact]
    public async Task HandleEventAsync_GivenAPendingPaymentThatSettles_NumbersTheReceipt()
    {
        // Arrange
        SetUpExistingPayment(PaymentStatus.Pending);

        // Act
        await _sut.HandleEventAsync(Event("pi_test", PaymentStatus.Settled), CancellationToken.None);

        // Assert
        _mockPaymentRepository.Verify(
            x => x.SettleAsync(It.IsAny<PaymentId>(), It.IsAny<SettledAt>(), It.IsAny<ReceiptNumber>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleEventAsync_GivenASucceededEventForAnAlreadyReturnedPayment_ChangesNothing()
    {
        // Arrange
        SetUpExistingPayment(PaymentStatus.Returned);

        // Act
        await _sut.HandleEventAsync(Event("pi_test", PaymentStatus.Settled), CancellationToken.None);

        // Assert
        _mockPaymentRepository.Verify(
            x => x.SettleAsync(It.IsAny<PaymentId>(), It.IsAny<SettledAt>(), It.IsAny<ReceiptNumber>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleEventAsync_GivenASettledPaymentThatIsReturned_ReversesTheLedgerAndReopensTheInvoice()
    {
        // Arrange
        SetUpExistingPayment(PaymentStatus.Settled);

        // Act
        await _sut.HandleEventAsync(Event("pi_test", PaymentStatus.Returned), CancellationToken.None);

        // Assert
        _mockAccountEntryRepository.Verify(
            x => x.AddAsync(
                It.Is<AccountEntry>(y => y.Reason == AccountEntryReason.Return && y.Amount < 0),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockInvoiceRepository.Verify(
            x => x.UnprocessAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetUpExistingPayment(PaymentStatus paymentStatus)
    {
        _mockPaymentRepository
            .Setup(x => x.FindByProviderReferenceAsync(It.IsAny<ProviderPaymentReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment
            {
                PaymentId = new PaymentId(Guid.NewGuid()),
                OrganizationId = _invoice.OrganizationId,
                Organization = null,
                Amount = new PaymentAmount(100m),
                Currency = Currency.Gbp,
                PaymentStatus = paymentStatus,
                PaymentMethodKind = PaymentMethodKind.DirectDebit,
                ProviderPaymentReference = new ProviderPaymentReference("pi_test"),
                PaymentMethodReference = null,
                PaymentMethodLabel = null,
                RemittanceReference = null,
                ReceiptNumber = null,
                RaisedForInvoiceId = _invoice.InvoiceId,
                ReceivedAt = new ReceivedAt(MockTimeProvider.GetUtcNow()),
                SettledAt = null,
                ReturnedAt = null,
                FailureCode = null,
                FailureReason = null,
                RecordedByUserId = null,
                IsDeleted = new IsDeleted(false),
                DeletedAt = null,
                CreatedAt = new CreatedAt(MockTimeProvider.GetUtcNow()),
                UpdatedAt = new UpdatedAt(MockTimeProvider.GetUtcNow())
            });
    }

    private void SetUpAProviderThatSettles()
    {
        SetUpAProvider(PaymentStatus.Settled);
    }

    private void SetUpAProvider(PaymentStatus paymentStatus)
    {
        _mockPaymentProvider
            .Setup(x => x.ListPaymentMethodsAsync(It.IsAny<ProviderCustomerReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProviderPaymentMethod
                {
                    PaymentMethodReference = new PaymentMethodReference("pm_test"),
                    PaymentMethodKind = PaymentMethodKind.Card,
                    Label = new PaymentMethodLabel("Visa 4242"),
                    MandateReference = null,
                    IsDefault = new IsDefault(true),
                    ExpiresAt = null
                }
            ]);

        _mockPaymentProvider
            .Setup(x => x.ChargeAsync(It.IsAny<ProviderChargeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderChargeResult
            {
                PaymentReference = new ProviderPaymentReference("pi_test"),
                PaymentStatus = paymentStatus,
                PaymentMethodReference = new PaymentMethodReference("pm_test"),
                PaymentMethodLabel = new PaymentMethodLabel("Visa 4242"),
                PaymentMethodKind = PaymentMethodKind.Card,
                FailureCode = null,
                FailureReason = null
            });

        _mockPaymentRepository
            .Setup(x => x.GetAsync(It.IsAny<PaymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Payment
            {
                PaymentId = new PaymentId(Guid.NewGuid()),
                OrganizationId = _invoice.OrganizationId,
                Organization = null,
                Amount = new PaymentAmount(100m),
                Currency = Currency.Gbp,
                PaymentStatus = paymentStatus,
                PaymentMethodKind = PaymentMethodKind.Card,
                ProviderPaymentReference = new ProviderPaymentReference("pi_test"),
                PaymentMethodReference = null,
                PaymentMethodLabel = null,
                RemittanceReference = null,
                ReceiptNumber = null,
                RaisedForInvoiceId = _invoice.InvoiceId,
                ReceivedAt = new ReceivedAt(MockTimeProvider.GetUtcNow()),
                SettledAt = null,
                ReturnedAt = null,
                FailureCode = null,
                FailureReason = null,
                RecordedByUserId = null,
                IsDeleted = new IsDeleted(false),
                DeletedAt = null,
                CreatedAt = new CreatedAt(MockTimeProvider.GetUtcNow()),
                UpdatedAt = new UpdatedAt(MockTimeProvider.GetUtcNow())
            });
    }

    private static ProviderEvent Event(string? paymentReference, PaymentStatus paymentStatus)
    {
        return new ProviderEvent
        {
            EventReference = new ProviderEventReference("evt_test"),
            EventType = new ProviderEventType("payment_intent.succeeded"),
            Payload = new Payload("{}"),
            PaymentReference = paymentReference is null ? null : new ProviderPaymentReference(paymentReference),
            PaymentStatus = paymentReference is null ? null : paymentStatus,
            PaymentMethodReference = null,
            PaymentMethodLabel = null,
            PaymentMethodKind = null,
            FailureCode = null,
            FailureReason = null
        };
    }
}
