using Core.Configs;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Billing;
using Core.Models.CreditNotes;
using Core.Models.Invoices;
using Core.Models.Organizations;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using QuestPDF;
using QuestPDF.Infrastructure;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Billing;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invoices;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Localization;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class CreditNoteServiceTests : BaseTestClass
{
    private readonly Mock<ICreditNoteRepository> _mockCreditNoteRepository = new();
    private readonly Mock<IInvoiceService> _mockInvoiceService = new();
    private readonly Mock<IBillingEntityService> _mockBillingEntityService = new();
    private readonly Mock<IDocumentNumberRepository> _mockDocumentNumberRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IDisplayPreferenceService> _mockDisplayPreferenceService = new();
    private readonly Mock<IOptions<AppConfig>> _mockConfig = new();
    private readonly BillingEntity _billingEntity;
    private readonly CreditNoteService _sut;

    public CreditNoteServiceTests()
    {
        Settings.License = LicenseType.Community;

        _billingEntity = FakeBillingEntity.CreateValid(Fixture);

        _mockConfig
            .SetupGet(x => x.Value)
            .Returns(FakeAppConfig.CreateValid(Fixture));

        _mockBillingEntityService
            .Setup(x => x.GetAsync(It.IsAny<BillingEntityId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_billingEntity);

        _mockDocumentNumberRepository
            .Setup(x => x.NextCreditNotePositionAsync(It.IsAny<BillingEntityId>(), It.IsAny<Year>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockCreditNoteRepository
            .Setup(x => x.GetCreditedNetAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NetAmount(0m));

        _mockUnitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<CreditNote>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<CreditNote>> operation, CancellationToken _) => operation());

        _mockDisplayPreferenceService
            .Setup(x => x.Resolve(It.IsAny<Locale?>(), It.IsAny<DisplayTimeZone?>(), It.IsAny<Organization?>()))
            .Returns(FakeDisplayPreferences.CreateValid());

        _mockDisplayPreferenceService
            .Setup(x => x.ForStatedAsync(It.IsAny<Locale?>(), It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeDisplayPreferences.CreateValid());

        _sut = new CreditNoteService(
            _mockCreditNoteRepository.Object,
            _mockInvoiceService.Object,
            _mockBillingEntityService.Object,
            _mockDocumentNumberRepository.Object,
            _mockDisplayPreferenceService.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext,
            _mockConfig.Object);
    }

    private Invoice GivenInvoice(InvoiceStatus invoiceStatus, decimal net, decimal taxRate = 0m, int lineCount = 2)
    {
        var invoiceId = FakeInvoiceId.CreateValid(Fixture);

        var lines = Enumerable.Range(0, lineCount)
            .Select(index => FakeInvoiceLine.CreateValid(Fixture) with
            {
                InvoiceId = invoiceId,
                InvoiceLineType = InvoiceLineType.Usage,
                Amount = new LineAmount(net / lineCount),
                SortOrder = new SortOrder(index)
            })
            .ToList();

        var invoice = FakeInvoice.CreateValid(Fixture) with
        {
            InvoiceId = invoiceId,
            InvoiceStatus = invoiceStatus,
            BillingEntityId = _billingEntity.BillingEntityId,
            NetAmount = new NetAmount(net),
            TaxRate = new TaxRate(taxRate),
            TaxTreatment = taxRate > 0 ? TaxTreatment.Standard : TaxTreatment.OutOfScope,
            Lines = lines
        };

        _mockInvoiceService
            .Setup(x => x.GetAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);

        return invoice;
    }

    private CreateCreditNoteRequest Request(Invoice invoice, decimal? netAmount = null)
    {
        return new CreateCreditNoteRequest
        {
            OrganizationId = invoice.OrganizationId,
            InvoiceId = invoice.InvoiceId,
            Reason = FakeCreditNoteReason.CreateValid(Fixture),
            NetAmount = netAmount.HasValue ? new NetAmount(netAmount.Value) : null,
            CreatedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };
    }

    private void CaptureWrittenCreditNote()
    {
        CreditNote? written = null;

        _mockCreditNoteRepository
            .Setup(x => x.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()))
            .Callback((CreditNote creditNote, CancellationToken _) => written = creditNote);

        _mockCreditNoteRepository
            .Setup(x => x.GetAsync(It.IsAny<CreditNoteId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => written!);
    }

    [Fact]
    public async Task CreateAsync_GivenNoAmount_CreditsTheWholeInvoiceLineForLine()
    {
        // Arrange
        var invoice = GivenInvoice(InvoiceStatus.Issued, 100m);
        CaptureWrittenCreditNote();

        // Act
        var creditNote = await _sut.CreateAsync(Request(invoice), CancellationToken.None);

        // Assert
        creditNote.NetAmount.Should().Be(new NetAmount(100m));
        creditNote.Lines.Should().HaveCount(invoice.Lines.Count);
        creditNote.Lines.Sum(x => x.Amount).Should().Be(100m);
    }

    [Fact]
    public async Task CreateAsync_GivenAnAmount_CreditsThatMuchOnOneLine()
    {
        // Arrange
        var invoice = GivenInvoice(InvoiceStatus.Issued, 100m);
        CaptureWrittenCreditNote();

        // Act
        var creditNote = await _sut.CreateAsync(Request(invoice, 40m), CancellationToken.None);

        // Assert
        creditNote.NetAmount.Should().Be(new NetAmount(40m));
        creditNote.Lines.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsync_GivenAnInvoiceThatChargedTax_GivesBackTaxAtTheRateItCharged()
    {
        // Arrange
        var invoice = GivenInvoice(InvoiceStatus.Issued, 100m, taxRate: 20m);
        CaptureWrittenCreditNote();

        // Act
        var creditNote = await _sut.CreateAsync(Request(invoice, 50m), CancellationToken.None);

        // Assert
        creditNote.TaxRate.Should().Be(new TaxRate(20m));
        creditNote.TaxAmount.Should().Be(new TaxAmount(10m));
        creditNote.Amount.Should().Be(new InvoiceAmount(60m));
    }

    [Fact]
    public async Task CreateAsync_GivenAnIssuedInvoice_TakesItsNumberFromTheCreditNoteSeries()
    {
        // Arrange
        var invoice = GivenInvoice(InvoiceStatus.Issued, 100m);
        CaptureWrittenCreditNote();

        // Act
        var creditNote = await _sut.CreateAsync(Request(invoice), CancellationToken.None);

        // Assert
        ((string)creditNote.CreditNoteNumber).Should().StartWith((string)_billingEntity.CreditNoteNumberPrefix);

        _mockDocumentNumberRepository.Verify(
            x => x.NextCreditNotePositionAsync(It.IsAny<BillingEntityId>(), It.IsAny<Year>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockDocumentNumberRepository.Verify(
            x => x.NextInvoicePositionAsync(It.IsAny<BillingEntityId>(), It.IsAny<Year>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_GivenADraftInvoice_Throws()
    {
        // Arrange
        var invoice = GivenInvoice(InvoiceStatus.Draft, 100m);

        // Act
        var act = async () => await _sut.CreateAsync(Request(invoice), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvoiceNotIssuedException>();

        _mockCreditNoteRepository.Verify(x => x.AddAsync(It.IsAny<CreditNote>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_GivenMoreThanTheInvoiceCharged_Throws()
    {
        // Arrange
        var invoice = GivenInvoice(InvoiceStatus.Issued, 100m);

        // Act
        var act = async () => await _sut.CreateAsync(Request(invoice, 101m), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CreditExceedsInvoiceException>();
    }

    [Fact]
    public async Task CreateAsync_GivenAnInvoiceAlreadyFullyCredited_Throws()
    {
        // Arrange
        var invoice = GivenInvoice(InvoiceStatus.Issued, 100m);

        _mockCreditNoteRepository
            .Setup(x => x.GetCreditedNetAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NetAmount(100m));

        // Act
        var act = async () => await _sut.CreateAsync(Request(invoice), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CreditExceedsInvoiceException>();
    }

    [Fact]
    public async Task CreateAsync_GivenSomethingAlreadyCredited_CreditsTheRemainderOnOneLine()
    {
        // Arrange
        var invoice = GivenInvoice(InvoiceStatus.Issued, 100m);
        CaptureWrittenCreditNote();

        _mockCreditNoteRepository
            .Setup(x => x.GetCreditedNetAsync(It.IsAny<InvoiceId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NetAmount(60m));

        // Act
        var creditNote = await _sut.CreateAsync(Request(invoice), CancellationToken.None);

        // Assert
        creditNote.NetAmount.Should().Be(new NetAmount(40m));
        creditNote.Lines.Should().ContainSingle();
    }

    [Fact]
    public async Task ExportAsync_GivenACreditNote_RendersTheDocument()
    {
        // Arrange
        var invoice = GivenInvoice(InvoiceStatus.Issued, 100m);
        CaptureWrittenCreditNote();

        var creditNote = await _sut.CreateAsync(Request(invoice), CancellationToken.None);

        // Act
        var exportedCreditNote = await _sut.ExportAsync(creditNote.CreditNoteId, CancellationToken.None);

        // Assert
        exportedCreditNote.Content.Should().NotBeEmpty();
    }
}
