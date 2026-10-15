using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Presentation.Models.Invoices;
using Presentation.Models.OrganizationInvoices;
using Presentation.Models.Revenue;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class RevenueControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string AdminEmail = "platform@mail.com";
    private const string ManagerEmail = "desk@mail.com";
    private const string Password = "P@$$w0rd";

    private static readonly Guid AcmeId = new("4d2a8f16-3b07-4e59-9c81-7f0e5a3d6b24");
    private static readonly Guid BordenId = new("9e5b1c74-6a20-48d3-b7f5-2c8d0e4a9f61");

    private static readonly DateTimeOffset January = new(2020, 1, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset February = new(2020, 2, 15, 12, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        await testFixture.ResetDatabaseAsync();

        await DatabaseHelper.CreateOrganizations(testFixture.Services,
        [
            OrganizationConfigurations.DefaultOrganization,
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = AcmeId,
                FriendlyName = "Acme Payments",
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            },
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = BordenId,
                FriendlyName = "Borden Cards",
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            }
        ]);

        await AuthenticationHelper.CreateUserAsync(testFixture.Services, AdminEmail, Password, [UserRole.Admin], AcmeId);
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, ManagerEmail, Password, [UserRole.Manager], AcmeId);

        await AuthenticationHelper.SetTokenAsync(_httpClient, AdminEmail, Password);

        await DatabaseHelper.CreateBillingProfile(testFixture.Services, AcmeId, customerCountryCode: "US");
        await DatabaseHelper.CreateBillingProfile(
            testFixture.Services, BordenId, customerCountryCode: "US",
            invoiceNumberPrefix: "INB", creditNoteNumberPrefix: "CRB");

        await DatabaseHelper.CreatePlanWithSubscription(testFixture.Services, AcmeId, 0.10m, January.AddYears(-1));
        await DatabaseHelper.CreatePlanWithSubscription(testFixture.Services, BordenId, 0.10m, January.AddYears(-1));
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListRecurringRevenue_SeparatesExpansionAndChurnRatherThanNettingThem()
    {
        // Arrange
        await GivenIssuedInvoice(AcmeId, January, 1000);
        await GivenIssuedInvoice(BordenId, January, 1000);
        await GivenIssuedInvoice(AcmeId, February, 2000);

        // Act
        var months = await GetAsync<List<RecurringRevenueMonthResponseDto>>(
            "/revenue/recurring?fromMonth=2&fromYear=2020&toMonth=2&toYear=2020");

        // Assert
        var february = months.Should().ContainSingle().Subject;

        february.OpeningRevenue.Should().Be(200m);
        february.ExpansionRevenue.Should().Be(100m);
        february.ChurnedRevenue.Should().Be(100m);
        february.ClosingRevenue.Should().Be(200m);
        february.ChurnedCustomers.Should().Be(1);
    }

    [Fact]
    public async Task ListRecurringRevenue_ReconcilesEveryMonthToItsClosingFigure()
    {
        // Arrange
        await GivenIssuedInvoice(AcmeId, January, 1000);
        await GivenIssuedInvoice(AcmeId, February, 1500);
        await GivenIssuedInvoice(BordenId, February, 500);

        // Act
        var months = await GetAsync<List<RecurringRevenueMonthResponseDto>>(
            "/revenue/recurring?fromMonth=1&fromYear=2020&toMonth=2&toYear=2020");

        // Assert
        months.Should().HaveCount(2);

        foreach (var month in months)
        {
            var reconciled = month.OpeningRevenue
                             + month.NewRevenue
                             + month.ReactivationRevenue
                             + month.ExpansionRevenue
                             - month.ContractionRevenue
                             - month.ChurnedRevenue;

            month.ClosingRevenue.Should().Be(reconciled);
        }

        months[1].OpeningRevenue.Should().Be(months[0].ClosingRevenue);
    }

    [Fact]
    public async Task ListRecurringRevenue_IgnoresADraftInvoiceUntilItIsIssued()
    {
        // Arrange
        await GivenGeneratedInvoice(AcmeId, January, 1000);

        // Act
        var months = await GetAsync<List<RecurringRevenueMonthResponseDto>>(
            "/revenue/recurring?fromMonth=1&fromYear=2020&toMonth=1&toYear=2020");

        // Assert
        months[0].ClosingRevenue.Should().Be(0m);
        months[0].ClosingCustomers.Should().Be(0);
    }

    [Fact]
    public async Task GetConcentration_RanksCustomersAndSaysHowExposedTheBookIsToTheLargest()
    {
        // Arrange
        await GivenIssuedInvoice(AcmeId, January, 3000);
        await GivenIssuedInvoice(BordenId, January, 1000);

        // Act
        var concentration = await GetAsync<RevenueConcentrationResponseDto>(
            "/revenue/concentration?month=1&year=2020");

        // Assert
        concentration.TotalRevenue.Should().Be(400m);
        concentration.CustomerCount.Should().Be(2);
        concentration.Customers[0].OrganizationName.Should().Be("Acme Payments");
        concentration.Customers[0].Rank.Should().Be(1);
        concentration.LargestCustomerShare.Should().Be(75m);
        concentration.ConcentrationIndex.Should().BeApproximately(0.625m, 0.0001m);
    }

    [Fact]
    public async Task GetConcentration_GivenAMonthThatBilledNothing_ReportsNoSharesRatherThanZero()
    {
        // Act
        var concentration = await GetAsync<RevenueConcentrationResponseDto>(
            "/revenue/concentration?month=1&year=2020");

        // Assert
        concentration.LargestCustomerShare.Should().BeNull();
        concentration.ConcentrationIndex.Should().BeNull();
    }

    [Fact]
    public async Task ListMargins_TakesTheCostOfTheWorkOffWhatWasBilled()
    {
        // Arrange
        await GivenIssuedInvoice(AcmeId, January, 10_000);
        await GivenCostRate(BillableResource.RuleEvaluation, 0.02m);

        // Act
        var margins = await GetAsync<List<CustomerMarginResponseDto>>("/revenue/margin?month=1&year=2020");

        // Assert
        var margin = margins.Should().ContainSingle().Subject;

        margin.Revenue.Should().Be(1000m);
        margin.Cost.Should().Be(200m);
        margin.Margin.Should().Be(800m);
        margin.MarginRate.Should().Be(80m);
        margin.CostsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task ListMargins_GivenWorkNobodyHasCosted_SaysTheCostsAreIncomplete()
    {
        // Arrange
        await GivenIssuedInvoice(AcmeId, January, 10_000);

        // Act
        var margins = await GetAsync<List<CustomerMarginResponseDto>>("/revenue/margin?month=1&year=2020");

        // Assert
        margins[0].CostsComplete.Should().BeFalse();
        margins[0].Cost.Should().Be(0m);
        margins[0].Lines.Should().ContainSingle(x => x.UnitCost == null);
    }

    [Fact]
    public async Task ListMargins_GivenACostRateRecordedAfterTheMonth_LeavesThatMonthMeasuredAtTheOldOne()
    {
        // Arrange
        await GivenIssuedInvoice(AcmeId, January, 10_000);
        await GivenCostRate(BillableResource.RuleEvaluation, 0.02m, effectiveFrom: January.AddMonths(-6));
        await GivenCostRate(BillableResource.RuleEvaluation, 0.09m, effectiveFrom: February);

        // Act
        var margins = await GetAsync<List<CustomerMarginResponseDto>>("/revenue/margin?month=1&year=2020");

        // Assert
        margins[0].Cost.Should().Be(200m);
    }

    [Fact]
    public async Task GetRecovery_GivenAWindowThatOpenedNoLadders_ReportsNoRateRatherThanZero()
    {
        // Act
        var recovery = await GetAsync<RecoveryStatisticResponseDto>(
            "/revenue/recovery?periodStart=2020-01-01T00:00:00Z&periodEnd=2020-03-01T00:00:00Z");

        // Assert
        recovery.LaddersOpened.Should().Be(0);
        recovery.RecoveryRate.Should().BeNull();
        recovery.MeanDaysToRecover.Should().BeNull();
    }

    [Fact]
    public async Task ListHealth_ScoresEveryCustomerAndSaysWhatTookThePointsOff()
    {
        // Act
        var health = await GetAsync<List<CustomerHealthResponseDto>>("/revenue/health?windowDays=30");

        // Assert
        health.Should().HaveCountGreaterThanOrEqualTo(2);
        health.Should().Contain(x => x.OrganizationName == "Acme Payments");

        health.Should().OnlyContain(x => x.Signals.Exists(signal => signal.Kind == nameof(HealthSignalKind.NoBaseline)));
    }

    [Fact]
    public async Task ExportRecognition_RecognisesEachDocumentInTheMonthTheServiceWasDelivered()
    {
        // Arrange
        await GivenIssuedInvoice(AcmeId, January, 1000);

        // Act
        var response = await _httpClient.GetAsync(
            "/revenue/recognition?fromMonth=1&fromYear=2020&toMonth=1&toYear=2020");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var csv = await response.Content.ReadAsStringAsync();
        var lines = csv.TrimStart('﻿').Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        lines[0].Should().StartWith("DocumentType,DocumentNumber,IssuedAt,RecognitionPeriod");
        lines.Should().HaveCount(2);
        lines[1].Should().Contain("\"Invoice\"");
        lines[1].Should().Contain("\"2020-01\"");
        lines[1].Should().Contain("\"100.00\"");

        lines[1].Should().Contain("\"Customer Ltd\"");
        lines[1].Should().NotContain("\"Acme Payments\"");

        lines[1].Should().Contain("\"Transaction Monitoring Ltd\"");
    }

    [Theory]
    [InlineData("/revenue/recurring?fromMonth=1&fromYear=2020&toMonth=1&toYear=2020")]
    [InlineData("/revenue/concentration?month=1&year=2020")]
    [InlineData("/revenue/margin?month=1&year=2020")]
    [InlineData("/revenue/recovery?periodStart=2020-01-01T00:00:00Z&periodEnd=2020-03-01T00:00:00Z")]
    [InlineData("/revenue/health")]
    [InlineData("/revenue/recognition?fromMonth=1&fromYear=2020&toMonth=1&toYear=2020")]
    [InlineData("/service-cost-rates")]
    public async Task RevenueEndpoints_GivenASupervisorRatherThanTheInstallationOperator_AreRefused(string route)
    {
        // Arrange
        var httpClient = testFixture.CreateClient();
        await AuthenticationHelper.SetTokenAsync(httpClient, ManagerEmail, Password);

        // Act
        var response = await httpClient.GetAsync(route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<T> GetAsync<T>(string route)
    {
        var response = await _httpClient.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await response.Content.ReadDataFromJsonAsync<T>();

        return data!;
    }

    private async Task GivenCostRate(BillableResource billableResource, decimal unitCost, DateTimeOffset? effectiveFrom = null)
    {
        var response = await _httpClient.PostAsJsonAsync("/service-cost-rates", new CreateServiceCostRateRequestBodyDto
        {
            BillableResource = billableResource,
            UnitCost = unitCost,
            Currency = Currency.Gbp,
            EffectiveFrom = effectiveFrom ?? new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<InvoiceResponseDto> GivenGeneratedInvoice(Guid organizationId, DateTimeOffset month, int evaluations)
    {
        await DatabaseHelper.CreateUsages(testFixture.Services,
        [
            FakeUsageEntity.CreateValid(_fixture) with
            {
                OrganizationId = organizationId,
                Organization = null!,
                BillableResourceId = (int)BillableResource.RuleEvaluation,
                Count = evaluations,
                CreatedAt = month
            }
        ]);

        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{organizationId}/invoices",
            new GenerateOrganizationInvoiceRequestBodyDto { Month = month.Month, Year = month.Year });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var invoice = await response.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();

        return invoice!.Invoice;
    }

    private async Task GivenIssuedInvoice(Guid organizationId, DateTimeOffset month, int evaluations)
    {
        var draft = await GivenGeneratedInvoice(organizationId, month, evaluations);

        var response = await _httpClient.PostAsync(
            $"/organizations/{organizationId}/invoices/{draft.InvoiceId}/issue", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
