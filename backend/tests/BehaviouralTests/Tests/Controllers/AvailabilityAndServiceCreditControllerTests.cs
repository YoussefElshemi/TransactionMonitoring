using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Availability;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Models.Availability;
using Presentation.Models.Common;
using Presentation.Models.Invoices;
using Presentation.Models.OrganizationInvoices;
using Presentation.Models.ServiceCredits;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class AvailabilityAndServiceCreditControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string AdminEmail = "platform@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("8d2a7f61-3b94-4c05-a7e8-9f6b1d0c4a35");
    private static readonly DateTimeOffset MeasuredMonth = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

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
        await AuthenticationHelper.SetTokenAsync(_httpClient, AdminEmail, Password);

        await DatabaseHelper.CreatePlanWithSubscription(
            testFixture.Services, OrganizationId, 0.10m, MeasuredMonth.AddYears(-1));

        await DatabaseHelper.CreateBillingProfile(testFixture.Services, OrganizationId, customerCountryCode: "US");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task MeasureAvailability_GivenMeasuredHoursAndNoCommitment_ReportsTheFiguresAndNoVerdict()
    {
        // Arrange
        await GivenMeasuredHour(MeasuredMonth, 1000, 10);

        // Act
        var measurement = await MeasureAsync();

        // Assert
        measurement.TotalRequests.Should().Be(1000);
        measurement.FailedRequests.Should().Be(10);
        measurement.UptimePercentage.Should().Be(99m);
        measurement.AvailabilityTarget.Should().BeNull();
        measurement.MetAvailabilityTarget.Should().BeNull();
    }

    [Fact]
    public async Task MergeAvailability_GivenANewWindow_PersistsTheWindowAndLatencyBuckets()
    {
        // Arrange
        var periodStart = MeasuredMonth.AddHours(1);
        using var scope = testFixture.Services.CreateScope();
        var availabilityService = scope.ServiceProvider.GetRequiredService<IAvailabilityService>();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();

        // Act
        await availabilityService.MergeAsync(
        [
            new MergeAvailabilityWindowRequest
            {
                OrganizationId = new OrganizationId(OrganizationId),
                PeriodStart = new PeriodStart(periodStart),
                TotalRequests = new RequestCount(2),
                FailedRequests = new RequestCount(1),
                TotalLatencyMilliseconds = new TotalLatencyMilliseconds(300),
                MaxLatencyMilliseconds = new LatencyMilliseconds(200),
                LatencyBuckets =
                [
                    new MergeAvailabilityLatencyBucketRequest
                    {
                        UpperBoundMilliseconds = new LatencyBucketBound(100),
                        Count = new RequestCount(1)
                    },
                    new MergeAvailabilityLatencyBucketRequest
                    {
                        UpperBoundMilliseconds = new LatencyBucketBound(250),
                        Count = new RequestCount(1)
                    }
                ]
            }
        ], CancellationToken.None);

        // Assert
        var window = await context.AvailabilityWindows
            .Include(x => x.LatencyBuckets)
            .SingleAsync(x => x.OrganizationId == OrganizationId && x.PeriodStart == periodStart);

        window.TotalRequests.Should().Be(2);
        window.FailedRequests.Should().Be(1);
        window.LatencyBuckets.Should().HaveCount(2);
    }

    [Fact]
    public async Task MeasureAvailability_GivenACommitmentInForce_SaysWhetherTheMonthMetIt()
    {
        // Arrange
        await GivenMeasuredHour(MeasuredMonth, 1000, 10);
        await GivenCommitmentAsync(99.9m);

        // Act
        var measurement = await MeasureAsync();

        // Assert
        measurement.AvailabilityTarget.Should().Be(99.9m);
        measurement.MetAvailabilityTarget.Should().BeFalse();
    }

    [Fact]
    public async Task IssueServiceCredit_GivenAMonthBelowTheTarget_RaisesACreditNoteForTheBand()
    {
        // Arrange
        await GivenMeasuredHour(MeasuredMonth, 1000, 50);
        await GivenCommitmentAsync(99.9m);
        var invoice = await GivenIssuedInvoiceAsync();

        // Act
        var serviceCredit = await IssueServiceCreditAsync();

        // Assert
        serviceCredit.MeasuredUptimePercentage.Should().Be(95m);
        serviceCredit.CreditPercentage.Should().Be(25m);
        serviceCredit.Amount.Should().Be(Math.Round(invoice.NetAmount * 0.25m, 2));
        serviceCredit.InvoiceId.Should().Be(invoice.InvoiceId);
        serviceCredit.CreditNoteId.Should().NotBeNull();
    }

    [Fact]
    public async Task IssueServiceCredit_GivenAMonthThatMetTheTarget_RecordsItAndOwesNothing()
    {
        // Arrange
        await GivenMeasuredHour(MeasuredMonth, 10_000, 1);
        await GivenCommitmentAsync(99.9m);
        await GivenIssuedInvoiceAsync();

        // Act
        var serviceCredit = await IssueServiceCreditAsync();

        // Assert
        serviceCredit.MeasuredUptimePercentage.Should().Be(99.99m);
        serviceCredit.CreditPercentage.Should().Be(0m);
        serviceCredit.Amount.Should().Be(0m);
        serviceCredit.CreditNoteId.Should().BeNull();
    }

    [Fact]
    public async Task IssueServiceCredit_GivenTheMonthHasAlreadyBeenMeasured_IsRefused()
    {
        // Arrange
        await GivenMeasuredHour(MeasuredMonth, 1000, 50);
        await GivenCommitmentAsync(99.9m);
        await GivenIssuedInvoiceAsync();
        await IssueServiceCreditAsync();

        // Act
        var response = await PostServiceCreditAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task IssueServiceCredit_GivenNoCommitmentInForce_IsRefused()
    {
        // Arrange
        await GivenMeasuredHour(MeasuredMonth, 1000, 500);
        await GivenIssuedInvoiceAsync();

        // Act
        var response = await PostServiceCreditAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateAvailabilityCommitment_GivenATermAlreadyInForce_ClosesItRatherThanOverlapping()
    {
        // Arrange
        await GivenCommitmentAsync(99.5m);

        // Act
        await GivenCommitmentAsync(99.9m);

        // Assert
        var response = await _httpClient.GetFromJsonAsync<PagedResponseDto<AvailabilityCommitmentResponseDto>>(
            $"/organizations/{OrganizationId}/availability/commitments?includeSuperseded=true");

        response!.Data.Should().HaveCount(2);
        response.Data.Count(x => x.EffectiveTo == null).Should().Be(1);
        response.Data.Single(x => x.EffectiveTo == null).AvailabilityTarget.Should().Be(99.9m);
    }

    [Fact]
    public async Task CreateAvailabilityCommitment_GivenABandAboveTheTarget_IsRefused()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/availability/commitments",
            new
            {
                AvailabilityTarget = 99.9m,
                LatencyTargetMilliseconds = 500,
                LatencyPercentile = 99,
                CreditBands = new[] { new { BelowUptimePercentage = 99.99m, CreditPercentage = 10m } }
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private Task GivenMeasuredHour(DateTimeOffset periodStart, int totalRequests, int failedRequests)
    {
        return DatabaseHelper.CreateAvailabilityWindow(
            testFixture.Services, OrganizationId, periodStart, totalRequests, failedRequests);
    }

    private async Task GivenCommitmentAsync(decimal availabilityTarget)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/availability/commitments",
            new
            {
                AvailabilityTarget = availabilityTarget,
                LatencyTargetMilliseconds = 500,
                LatencyPercentile = 99,
                EffectiveFrom = MeasuredMonth.AddMonths(-1),
                CreditBands = new[]
                {
                    new { BelowUptimePercentage = availabilityTarget, CreditPercentage = 10m },
                    new { BelowUptimePercentage = 99m, CreditPercentage = 25m },
                    new { BelowUptimePercentage = 95m, CreditPercentage = 50m }
                }
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<AvailabilityMeasurementResponseDto> MeasureAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<ResponseDataDto<AvailabilityMeasurementResponseDto>>(
            $"/organizations/{OrganizationId}/availability" +
            $"?fromDate={MeasuredMonth:yyyy-MM-dd}" +
            $"&toDate={MeasuredMonth.AddMonths(1).AddDays(-1):yyyy-MM-dd}");

        return response!.Data;
    }

    private async Task<InvoiceResponseDto> GivenIssuedInvoiceAsync()
    {
        await DatabaseHelper.CreateUsages(testFixture.Services,
        [
            FakeUsageEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                Organization = null!,
                BillableResourceId = (int)BillableResource.RuleEvaluation,
                Count = 10_000,
                CreatedAt = MeasuredMonth
            }
        ]);

        var generated = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/invoices",
            new GenerateOrganizationInvoiceRequestBodyDto { Month = MeasuredMonth.Month, Year = MeasuredMonth.Year });

        generated.StatusCode.Should().Be(HttpStatusCode.Created);

        var draft = await generated.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();

        var issued = await _httpClient.PostAsync(
            $"/organizations/{OrganizationId}/invoices/{draft!.Invoice.InvoiceId}/issue", null);

        issued.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoice = await issued.Content.ReadDataFromJsonAsync<InvoiceResourceDto>();

        return invoice!.Invoice;
    }

    private async Task<ServiceCreditResponseDto> IssueServiceCreditAsync()
    {
        var response = await PostServiceCreditAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var serviceCredit = await response.Content.ReadDataFromJsonAsync<ServiceCreditResponseDto>();

        return serviceCredit!;
    }

    private Task<HttpResponseMessage> PostServiceCreditAsync()
    {
        return _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/service-credits",
            new {
                MeasuredMonth.Month,
                MeasuredMonth.Year });
    }
}
