using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Presentation.Models.BillingEntities;
using Presentation.Models.BillingProfiles;
using Presentation.Models.CreditNotes;
using Presentation.Models.ExchangeRates;
using Presentation.Models.Invoices;
using Presentation.Models.OrganizationInvoices;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class InvoiceDocumentControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string AdminEmail = "platform@mail.com";
    private const string ManagerEmail = "desk@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("6b3f8c21-4d0e-4f7a-9c52-1e8b7d4a3f60");
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
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GenerateInvoice_GivenNoBillingProfile_ReturnsConflict()
    {
        // Arrange
        await GivenMeteredUsage(1000);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices",
            new GenerateOrganizationInvoiceRequestBodyDto { Month = BilledMonth.Month, Year = BilledMonth.Year });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GenerateInvoice_GivenABillingProfile_ProducesAnUnnumberedDraft()
    {
        // Arrange
        await GivenBillingDetails();
        await GivenMeteredUsage(1000);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.InvoiceStatus.Should().Be(nameof(InvoiceStatus.Draft));
        invoice.InvoiceNumber.Should().BeNull();
        invoice.IssuedAt.Should().BeNull();
        invoice.DueAt.Should().BeNull();

        invoice.Supplier!.LegalName.Should().Be("Transaction Monitoring Ltd");
        invoice.Customer!.LegalName.Should().Be("Customer Ltd");
        invoice.Currency.Should().Be("GBP");
    }

    [Fact]
    public async Task IssueInvoice_GivenADraft_NumbersItAndStampsTheDates()
    {
        // Arrange
        await GivenBillingDetails();
        await GivenMeteredUsage(1000);

        var draft = await GenerateInvoiceAsync();

        // Act
        var issued = await IssueInvoiceAsync(draft.InvoiceId);

        // Assert
        issued.InvoiceStatus.Should().Be(nameof(InvoiceStatus.Issued));
        issued.InvoiceNumber.Should().Be($"INV-{DateTimeOffset.UtcNow.Year}-000001");
        issued.IssuedAt.Should().NotBeNull();

        issued.DueAt.Should().Be(issued.IssuedAt!.Value.AddDays(issued.PaymentTerms));
    }

    [Fact]
    public async Task IssueInvoice_GivenSeveralInvoices_NumbersThemInSequenceWithoutGaps()
    {
        // Arrange
        await GivenBillingDetails();
        await GivenMeteredUsage(1000);

        var numbers = new List<string>();

        // Act
        foreach (var month in new[] { 1, 2, 3 })
        {
            var draft = await GenerateInvoiceAsync(month);
            var issued = await IssueInvoiceAsync(draft.InvoiceId);

            numbers.Add(issued.InvoiceNumber!);
        }

        // Assert
        numbers.Should().BeEquivalentTo(
        [
            $"INV-{DateTimeOffset.UtcNow.Year}-000001",
            $"INV-{DateTimeOffset.UtcNow.Year}-000002",
            $"INV-{DateTimeOffset.UtcNow.Year}-000003"
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task DeleteInvoice_GivenADraft_ReturnsNoContent()
    {
        // Arrange
        await GivenBillingDetails();
        await GivenMeteredUsage(1000);

        var draft = await GenerateInvoiceAsync();

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/invoices/{draft.InvoiceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteInvoice_GivenAnIssuedInvoice_ReturnsConflict()
    {
        // Arrange
        await GivenBillingDetails();
        await GivenMeteredUsage(1000);

        var issued = await IssueInvoiceAsync((await GenerateInvoiceAsync()).InvoiceId);

        // Act
        var response = await _httpClient.DeleteAsync($"/organizations/{OrganizationId}/invoices/{issued.InvoiceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task IssueInvoice_GivenAnAlreadyIssuedInvoice_ReturnsConflict()
    {
        // Arrange
        await GivenBillingDetails();
        await GivenMeteredUsage(1000);

        var issued = await IssueInvoiceAsync((await GenerateInvoiceAsync()).InvoiceId);

        // Act
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invoices/{issued.InvoiceId}/issue", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GenerateInvoice_GivenADomesticCustomer_ChargesTaxAsItsOwnLine()
    {
        // Arrange
        await GivenBillingDetails(customerCountryCode: "GB");
        await GivenMeteredUsage(1000);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.TaxTreatment.Should().Be(nameof(TaxTreatment.Standard));
        invoice.TaxRate.Should().Be(20m);
        invoice.NetAmount.Should().Be(100m);
        invoice.TaxAmount.Should().Be(20m);
        invoice.Amount.Should().Be(120m);

        invoice.Lines.Single(x => x.InvoiceLineType == nameof(InvoiceLineType.Tax)).Amount.Should().Be(20m);
        invoice.Lines.Sum(x => x.Amount).Should().Be(invoice.Amount);
    }

    [Fact]
    public async Task GenerateInvoice_GivenAnEuBusinessWithAConfirmedRegistration_ReverseCharges()
    {
        // Arrange
        await GivenBillingDetails(customerCountryCode: "IE", customerTaxIdentifier: "IE1234567FA", taxIdentifierVerified: true);
        await GivenMeteredUsage(1000);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.TaxTreatment.Should().Be(nameof(TaxTreatment.ReverseCharge));
        invoice.TaxAmount.Should().Be(0m);
        invoice.Amount.Should().Be(100m);

        invoice.Lines.Single(x => x.InvoiceLineType == nameof(InvoiceLineType.Tax))
            .Description.Should().Contain("Reverse charge");

        invoice.Customer!.TaxIdentifier.Should().Be("IE1234567FA");
    }

    [Fact]
    public async Task GenerateInvoice_GivenAnUnconfirmedRegistration_DoesNotReverseCharge()
    {
        // Arrange
        await GivenBillingDetails(customerCountryCode: "IE", customerTaxIdentifier: "IE1234567FA");
        await GivenMeteredUsage(1000);

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.TaxTreatment.Should().NotBe(nameof(TaxTreatment.ReverseCharge));
        invoice.Customer!.TaxIdentifier.Should().BeNull();
    }

    [Fact]
    public async Task GenerateInvoice_GivenNoRecordedExchangeRate_ReturnsConflict()
    {
        // Arrange
        await GivenBillingDetails(customerCountryCode: "US", currency: Currency.Eur);
        await GivenMeteredUsage(1000);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices",
            new GenerateOrganizationInvoiceRequestBodyDto { Month = BilledMonth.Month, Year = BilledMonth.Year });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GenerateInvoice_GivenAPlanInAnotherCurrency_ConvertsAndRecordsTheRate()
    {
        // Arrange
        await GivenBillingDetails(customerCountryCode: "US", currency: Currency.Eur);
        await GivenMeteredUsage(1000);

        await DatabaseHelper.CreateExchangeRate(
            testFixture.Services, Currency.Gbp, Currency.Eur, 1.20m, BilledMonth.AddYears(-1));

        // Act
        var invoice = await GenerateInvoiceAsync();

        // Assert
        invoice.Currency.Should().Be("EUR");
        invoice.PlanCurrency.Should().Be("GBP");
        invoice.ConversionRate.Should().Be(1.20m);
        invoice.Amount.Should().Be(120m);
    }

    [Fact]
    public async Task CreateCreditNote_GivenAnIssuedInvoice_CreditsItInItsOwnSeries()
    {
        // Arrange
        await GivenBillingDetails();
        await GivenMeteredUsage(1000);

        var issued = await IssueInvoiceAsync((await GenerateInvoiceAsync()).InvoiceId);

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices/{issued.InvoiceId}/credit-notes",
            new CreateCreditNoteRequestBodyDto { Reason = "Billed in error", NetAmount = null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var creditNote = await response.Content.ReadDataFromJsonAsync<CreditNoteResponseDto>();

        creditNote!.CreditNoteNumber.Should().Be($"CRN-{DateTimeOffset.UtcNow.Year}-000001");
        creditNote.InvoiceId.Should().Be(issued.InvoiceId);
        creditNote.NetAmount.Should().Be(issued.NetAmount);
        creditNote.Lines.Should().NotBeEmpty();
        creditNote.Lines.Sum(x => x.Amount).Should().Be(creditNote.NetAmount);
    }

    [Fact]
    public async Task CreateCreditNote_GivenADraft_ReturnsConflict()
    {
        // Arrange
        await GivenBillingDetails();
        await GivenMeteredUsage(1000);

        var draft = await GenerateInvoiceAsync();

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices/{draft.InvoiceId}/credit-notes",
            new CreateCreditNoteRequestBodyDto { Reason = "Billed in error", NetAmount = null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCreditNote_GivenMoreThanRemainsUncredited_ReturnsConflict()
    {
        // Arrange
        await GivenBillingDetails();
        await GivenMeteredUsage(1000);

        var issued = await IssueInvoiceAsync((await GenerateInvoiceAsync()).InvoiceId);

        await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices/{issued.InvoiceId}/credit-notes",
            new CreateCreditNoteRequestBodyDto { Reason = "Partial", NetAmount = 60m });

        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices/{issued.InvoiceId}/credit-notes",
            new CreateCreditNoteRequestBodyDto { Reason = "Too much", NetAmount = 60m });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ExportCreditNote_GivenACreditNote_ReturnsAPdf()
    {
        // Arrange
        await GivenBillingDetails();
        await GivenMeteredUsage(1000);

        var issued = await IssueInvoiceAsync((await GenerateInvoiceAsync()).InvoiceId);

        var created = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices/{issued.InvoiceId}/credit-notes",
            new CreateCreditNoteRequestBodyDto { Reason = "Billed in error", NetAmount = null });

        var creditNote = await created.Content.ReadDataFromJsonAsync<CreditNoteResponseDto>();

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/{OrganizationId}/credit-notes/{creditNote!.CreditNoteId}/export", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task VerifyTaxIdentifier_GivenAWellFormedRegistration_StampsIt()
    {
        // Arrange
        var billingEntityId = await GivenBillingDetails(customerCountryCode: "IE", customerTaxIdentifier: "IE1234567FA");

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/{OrganizationId}/billing-profile/verify-tax-identifier", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var billingProfile = await response.Content.ReadDataFromJsonAsync<BillingProfileResponseDto>();

        billingProfile!.TaxIdentifierVerifiedAt.Should().NotBeNull();
        billingProfile.BillingEntityId.Should().Be(billingEntityId);
    }

    [Fact]
    public async Task VerifyTaxIdentifier_GivenAMalformedRegistration_ReturnsUnprocessableEntity()
    {
        // Arrange
        await GivenBillingDetails(customerCountryCode: "IE", customerTaxIdentifier: "IE12");

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/{OrganizationId}/billing-profile/verify-tax-identifier", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SaveBillingProfile_GivenAnExistingProfile_ReplacesItRatherThanConflicting()
    {
        // Arrange
        var billingEntityId = await GivenBillingDetails();

        // Act
        var response = await _httpClient.PutAsJsonAsync(
            $"/organizations/{OrganizationId}/billing-profile",
            new SaveBillingProfileRequestBodyDto
            {
                BillingEntityId = billingEntityId,
                LegalName = "Customer Holdings Ltd",
                ContactName = "Accounts Payable",
                ContactEmail = "accounts@customer.test",
                Address = "3 Example Street\nLeeds\nLS1 1AA",
                CountryCode = "GB",
                TaxIdentifier = null,
                Currency = Currency.Gbp,
                PaymentTerms = 14
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var billingProfile = await response.Content.ReadDataFromJsonAsync<BillingProfileResponseDto>();

        billingProfile!.LegalName.Should().Be("Customer Holdings Ltd");
        billingProfile.PaymentTerms.Should().Be(14);
    }

    [Fact]
    public async Task CreateBillingEntity_AsManager_ReturnsForbidden()
    {
        // Arrange
        await AuthenticationHelper.SetTokenAsync(_httpClient, ManagerEmail, Password);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/billing-entities", new CreateBillingEntityRequestBodyDto
        {
            LegalName = "Rogue Ltd",
            Address = "1 Example Street",
            CountryCode = "GB",
            TaxIdentifier = null,
            InvoiceNumberPrefix = "RGE",
            CreditNoteNumberPrefix = "RGC",
            Currency = Currency.Gbp
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateBillingEntity_GivenOnePrefixForBothSeries_ReturnsBadRequest()
    {
        // Arrange
        // Act
        var response = await _httpClient.PostAsJsonAsync("/billing-entities", new CreateBillingEntityRequestBodyDto
        {
            LegalName = "Ambiguous Ltd",
            Address = "1 Example Street",
            CountryCode = "GB",
            TaxIdentifier = null,
            InvoiceNumberPrefix = "AMB",
            CreditNoteNumberPrefix = "AMB",
            Currency = Currency.Gbp
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateExchangeRate_GivenAPair_ReturnsCreated()
    {
        // Arrange
        // Act
        var response = await _httpClient.PostAsJsonAsync("/exchange-rates", new CreateExchangeRateRequestBodyDto
        {
            FromCurrency = Currency.Gbp,
            ToCurrency = Currency.Eur,
            Rate = 1.17m,
            EffectiveFrom = null
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var exchangeRate = await response.Content.ReadDataFromJsonAsync<ExchangeRateResponseDto>();

        exchangeRate!.FromCurrency.Should().Be("GBP");
        exchangeRate.ToCurrency.Should().Be("EUR");
        exchangeRate.Rate.Should().Be(1.17m);
    }

    [Fact]
    public async Task CreateExchangeRate_GivenTheCurrencyCodeAsResponsesRenderIt_ReturnsCreated()
    {
        // Arrange
        var body = new StringContent(
            """{"fromCurrency":"GBP","toCurrency":"USD","rate":1.27}""",
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        // Act
        var response = await _httpClient.PostAsync("/exchange-rates", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var exchangeRate = await response.Content.ReadDataFromJsonAsync<ExchangeRateResponseDto>();

        exchangeRate!.FromCurrency.Should().Be("GBP");
        exchangeRate.ToCurrency.Should().Be("USD");
    }

    [Fact]
    public async Task CreateExchangeRate_GivenTheSameCurrencyBothWays_ReturnsBadRequest()
    {
        // Arrange
        // Act
        var response = await _httpClient.PostAsJsonAsync("/exchange-rates", new CreateExchangeRateRequestBodyDto
        {
            FromCurrency = Currency.Gbp,
            ToCurrency = Currency.Gbp,
            Rate = 1m,
            EffectiveFrom = null
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private Task<Guid> GivenBillingDetails(
        string customerCountryCode = "US",
        Currency currency = Currency.Gbp,
        string? customerTaxIdentifier = null,
        bool taxIdentifierVerified = false)
    {
        return DatabaseHelper.CreateBillingProfile(
            testFixture.Services,
            OrganizationId,
            customerCountryCode: customerCountryCode,
            currency: currency,
            customerTaxIdentifier: customerTaxIdentifier,
            taxIdentifierVerified: taxIdentifierVerified);
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
            },
            FakeUsageEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                BillableResourceId = (int)BillableResource.RuleEvaluation,
                Count = count,
                CreatedAt = BilledMonth.AddMonths(2)
            }
        ]);
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

    private async Task<InvoiceResponseDto> IssueInvoiceAsync(Guid invoiceId)
    {
        var response = await _httpClient.PostAsync($"/organizations/{OrganizationId}/invoices/{invoiceId}/issue", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoice = await response.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();

        return invoice!.Invoice;
    }
}
