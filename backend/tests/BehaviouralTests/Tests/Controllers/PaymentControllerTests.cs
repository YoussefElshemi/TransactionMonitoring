using System.Net;
using System.Net.Http.Json;
using System.Text;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Presentation.Models.Accounts;
using Presentation.Models.Common;
using Presentation.Models.CreditNotes;
using Presentation.Models.Invoices;
using Presentation.Models.OrganizationInvoices;
using Presentation.Models.Payments;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class PaymentControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string AdminEmail = "platform@mail.com";
    private const string ManagerEmail = "desk@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("9c2f4a13-7b6e-4c81-a3d5-2f8e1b4c7a09");
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

        await DatabaseHelper.CreatePlanWithSubscription(
            testFixture.Services, OrganizationId, 0.10m, new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await DatabaseHelper.CreateBillingProfile(testFixture.Services, OrganizationId, customerCountryCode: "US");
        await GivenMeteredUsage(1000);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RecordPayment_GivenATransferAgainstAnInvoice_SettlesItAndNumbersTheReceipt()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();

        // Act
        var payment = await RecordPaymentAsync(invoice.Amount, invoice.InvoiceId);

        // Assert
        payment.PaymentStatus.Should().Be(nameof(PaymentStatus.Settled));
        payment.PaymentMethodKind.Should().Be(nameof(PaymentMethodKind.BankTransfer));
        payment.ReceiptNumber.Should().NotBeNullOrWhiteSpace();
        payment.SettledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordPayment_GivenATransferAgainstAnInvoice_WritesAReceiptAndAnAllocation()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();

        // Act
        await RecordPaymentAsync(invoice.Amount, invoice.InvoiceId);

        // Assert
        var entries = await GetAccountEntriesAsync();

        entries.Should().Contain(x => x.Reason == nameof(AccountEntryReason.Receipt) && x.Amount == invoice.Amount);
        entries.Should().Contain(x => x.Reason == nameof(AccountEntryReason.Allocation) && x.Amount == -invoice.Amount);
    }

    [Fact]
    public async Task GetBalances_GivenAnInvoicePaidExactly_LeavesNothingOnAccount()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();
        await RecordPaymentAsync(invoice.Amount, invoice.InvoiceId);

        // Act
        var balances = await GetBalancesAsync();

        // Assert
        balances.Should().ContainSingle(x => x.Currency == nameof(Currency.Gbp) && x.Amount == 0m);
    }

    [Fact]
    public async Task GetInvoice_GivenAPartPayment_ReportsWhatIsLeftOutstanding()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();
        await RecordPaymentAsync(invoice.Amount - 10m, invoice.InvoiceId);

        // Act
        var settlement = await GetSettlementAsync(invoice.InvoiceId);

        // Assert
        settlement!.OutstandingAmount.Should().Be(10m);
        settlement.AllocatedAmount.Should().Be(invoice.Amount - 10m);
        settlement.IsSettled.Should().BeFalse();
    }

    [Fact]
    public async Task RecordPayment_GivenMoreThanTheInvoiceAsked_LeavesTheDifferenceOnAccount()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();

        // Act
        await RecordPaymentAsync(invoice.Amount + 25m, invoice.InvoiceId);

        // Assert
        var settlement = await GetSettlementAsync(invoice.InvoiceId);
        var balances = await GetBalancesAsync();

        settlement!.IsSettled.Should().BeTrue();
        settlement.OutstandingAmount.Should().Be(0m);
        balances.Should().ContainSingle(x => x.Currency == nameof(Currency.Gbp) && x.Amount == 25m);
    }

    [Fact]
    public async Task RecordPayment_GivenAFullPayment_MarksTheInvoiceProcessedAndAPartOneDoesNot()
    {
        // Arrange
        var partlyPaid = await GivenAnIssuedInvoice();
        await RecordPaymentAsync(partlyPaid.Amount - 10m, partlyPaid.InvoiceId);

        var fullyPaid = await GivenAnIssuedInvoice(month: BilledMonth.Month + 1);
        await RecordPaymentAsync(fullyPaid.Amount, fullyPaid.InvoiceId);

        // Act
        var partly = await GetInvoiceAsync(partlyPaid.InvoiceId);
        var fully = await GetInvoiceAsync(fullyPaid.InvoiceId);

        // Assert
        partly.IsProcessed.Should().BeFalse();
        fully.IsProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task RecordPayment_GivenNoInvoice_HoldsTheCashAndSurfacesItForReconciliation()
    {
        // Arrange
        await GivenAnIssuedInvoice();

        // Act
        await RecordPaymentAsync(75m, invoiceId: null);

        // Assert
        var response = await _httpClient.GetAsync("/reconciliation/unapplied");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var unapplied = await response.Content.ReadFromJsonAsync<PagedResponseDto<AccountBalanceResponseDto>>();

        unapplied!.Data.Should().ContainSingle(x => x.OrganizationId == OrganizationId && x.Amount == 75m);
    }

    [Fact]
    public async Task AllocatePayment_GivenUnappliedCash_AppliesItToTheInvoice()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();
        await RecordPaymentAsync(invoice.Amount, invoiceId: null);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/account/allocations",
            new AllocatePaymentRequestBodyDto { InvoiceId = invoice.InvoiceId, Amount = null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var settlement = await GetSettlementAsync(invoice.InvoiceId);

        settlement!.IsSettled.Should().BeTrue();
    }

    [Fact]
    public async Task AllocatePayment_GivenNothingOnAccount_ReturnsConflict()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/account/allocations",
            new AllocatePaymentRequestBodyDto { InvoiceId = invoice.InvoiceId, Amount = null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CollectPayment_GivenAnInvoiceCreditedInFull_ReturnsConflict()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();

        var creditNoteResponse = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices/{invoice.InvoiceId}/credit-notes",
            new CreateCreditNoteRequestBodyDto { Reason = "Billed in error", NetAmount = null });

        creditNoteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices/{invoice.InvoiceId}/collect",
            new CollectPaymentRequestBodyDto { PaymentMethodReference = null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AllocatePayment_GivenADraftInvoice_ReturnsConflict()
    {
        // Arrange
        var draft = await GenerateInvoiceAsync();
        await RecordPaymentAsync(50m, invoiceId: null);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/account/allocations",
            new AllocatePaymentRequestBodyDto { InvoiceId = draft.InvoiceId, Amount = null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ExportReceipt_GivenASettledPayment_ReturnsAPdf()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();
        var payment = await RecordPaymentAsync(invoice.Amount, invoice.InvoiceId);

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/{OrganizationId}/payments/{payment.PaymentId}/receipt", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var content = await response.Content.ReadAsByteArrayAsync();

        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RecordPayment_AsAManager_ReturnsForbidden()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();

        await AuthenticationHelper.SetTokenAsync(_httpClient, ManagerEmail, Password);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/payments",
            new RecordPaymentRequestBodyDto
            {
                Amount = invoice.Amount,
                Currency = nameof(Currency.Gbp),
                ReceivedAt = DateTimeOffset.UtcNow,
                RemittanceReference = null,
                InvoiceId = invoice.InvoiceId
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListPayments_AsAManager_ReturnsTheirOwnHistory()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();
        await RecordPaymentAsync(invoice.Amount, invoice.InvoiceId);

        await AuthenticationHelper.SetTokenAsync(_httpClient, ManagerEmail, Password);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/payments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payments = await response.Content.ReadFromJsonAsync<PagedResponseDto<PaymentResponseDto>>();

        payments!.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task RecordPayment_GivenTheWrongCurrencyForTheInvoice_ReturnsConflict()
    {
        // Arrange
        var invoice = await GivenAnIssuedInvoice();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/payments",
            new RecordPaymentRequestBodyDto
            {
                Amount = invoice.Amount,
                Currency = nameof(Currency.Eur),
                ReceivedAt = DateTimeOffset.UtcNow,
                RemittanceReference = null,
                InvoiceId = invoice.InvoiceId
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReceiveProviderEvent_GivenNoSignature_ReturnsBadRequest()
    {
        // Arrange
        var body = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync("/payments/webhooks/provider", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<PaymentResponseDto> RecordPaymentAsync(decimal amount, Guid? invoiceId)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/payments",
            new RecordPaymentRequestBodyDto
            {
                Amount = amount,
                Currency = nameof(Currency.Gbp),
                ReceivedAt = DateTimeOffset.UtcNow,
                RemittanceReference = "REF-001",
                InvoiceId = invoiceId
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payment = await response.Content.ReadDataFromJsonAsync<PaymentResponseDto>();

        return payment!;
    }

    private async Task<AccountEntryResponseDto[]> GetAccountEntriesAsync()
    {
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/account/entries");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var entries = await response.Content.ReadFromJsonAsync<PagedResponseDto<AccountEntryResponseDto>>();

        return entries!.Data;
    }

    private async Task<AccountBalanceResponseDto[]> GetBalancesAsync()
    {
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/account");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var balances = await response.Content.ReadDataFromJsonAsync<AccountBalanceResponseDto[]>();

        return balances!;
    }

    private async Task<InvoiceSettlementResponseDto?> GetSettlementAsync(Guid invoiceId)
    {
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invoices/{invoiceId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoice = await response.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();

        return invoice!.Settlement;
    }

    private async Task<InvoiceResponseDto> GetInvoiceAsync(Guid invoiceId)
    {
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/invoices/{invoiceId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoice = await response.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();

        return invoice!.Invoice;
    }

    private async Task<InvoiceResponseDto> GivenAnIssuedInvoice(int? month = null)
    {
        var draft = await GenerateInvoiceAsync(month);

        var response = await _httpClient.PostAsync(
            $"/organizations/{OrganizationId}/invoices/{draft.InvoiceId}/issue", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoice = await response.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();

        return invoice!.Invoice;
    }

    private async Task<InvoiceResponseDto> GenerateInvoiceAsync(int? month = null)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices",
            new GenerateOrganizationInvoiceRequestBodyDto { Month = month ?? BilledMonth.Month, Year = BilledMonth.Year });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var invoice = await response.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();

        return invoice!.Invoice;
    }

    private Task GivenMeteredUsage(int count)
    {
        return DatabaseHelper.CreateUsages(testFixture.Services,
        [
            FakeUsageEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                BillableResourceId = (int)BillableResource.RuleEvaluation,
                Count = count,
                CreatedAt = BilledMonth
            },
            FakeUsageEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                BillableResourceId = (int)BillableResource.RuleEvaluation,
                Count = count,
                CreatedAt = BilledMonth.AddMonths(1)
            }
        ]);
    }
}
