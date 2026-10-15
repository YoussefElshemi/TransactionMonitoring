using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Enums;
using FluentAssertions;
using Infrastructure.Configurations;
using Presentation.Models.Common;
using Presentation.Models.Status;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class StatusPageControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();
    private readonly HttpClient _anonymousClient = testFixture.CreateClient();

    private const string AdminEmail = "platform@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("6b1d4f92-0c73-4a58-9e10-3d8f2a5c7b41");

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
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetStatus_GivenNoCredentialsAtAll_IsAnswered()
    {
        // Act
        var response = await _anonymousClient.GetAsync("status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await response.Content.ReadDataFromJsonAsync<ServiceStatusResponseDto>();
        status.Should().NotBeNull();
        status!.Components.Should().HaveCount(Enum.GetValues<ServiceComponent>().Length);
    }

    [Fact]
    public async Task GetStatus_GivenNothingProbedAndNothingDeclared_IsOperational()
    {
        // Act
        var status = await _anonymousClient.GetFromJsonAsync<ResponseDataDto<ServiceStatusResponseDto>>("status");

        // Assert
        status!.Data.Status.Should().Be(nameof(ComponentStatus.Operational));
        status.Data.Components.Should().OnlyContain(x => x.Status == nameof(ComponentStatus.Operational));
    }

    [Fact]
    public async Task CreateIncident_GivenACriticalIncident_ShowsTheComponentAsDownOnThePublicPage()
    {
        // Arrange
        var created = await CreateIncidentAsync(IncidentImpact.Critical, ServiceComponent.RuleEvaluation);

        // Act
        var status = await _anonymousClient.GetFromJsonAsync<ResponseDataDto<ServiceStatusResponseDto>>("status");

        // Assert
        created.IncidentNumber.Should().StartWith("INC");
        status!.Data.Status.Should().Be(nameof(ComponentStatus.MajorOutage));
        status.Data.Components.Single(x => x.Component == nameof(ServiceComponent.RuleEvaluation))
            .Status.Should().Be(nameof(ComponentStatus.MajorOutage));
        status.Data.Components.Single(x => x.Component == nameof(ServiceComponent.Api))
            .Status.Should().Be(nameof(ComponentStatus.Operational));
        status.Data.ActiveIncidents.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateIncident_GivenANewIncident_OpensItsTimelineWithTheSummary()
    {
        // Act
        var created = await CreateIncidentAsync(IncidentImpact.Major, ServiceComponent.Api);

        // Assert
        created.Status.Should().Be(nameof(IncidentStatus.Investigating));
        created.Updates.Should().ContainSingle();
        created.Updates[0].Status.Should().Be(nameof(IncidentStatus.Investigating));
    }

    [Fact]
    public async Task PostIncidentUpdate_GivenResolved_ClosesTheIncidentAndClearsThePublicPage()
    {
        // Arrange
        var created = await CreateIncidentAsync(IncidentImpact.Critical, ServiceComponent.Api);

        // Act
        var response = await _httpClient.PostAsJsonAsync($"incidents/{created.IncidentId}/updates", new
        {
            Status = IncidentStatus.Resolved,
            Body = "The failing node has been replaced and error rates are back to normal."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var resolved = await response.Content.ReadDataFromJsonAsync<IncidentResponseDto>();
        resolved!.Status.Should().Be(nameof(IncidentStatus.Resolved));
        resolved.ResolvedAt.Should().NotBeNull();
        resolved.Updates.Should().HaveCount(2);

        var status = await _anonymousClient.GetFromJsonAsync<ResponseDataDto<ServiceStatusResponseDto>>("status");
        status!.Data.ActiveIncidents.Should().BeEmpty();
        status.Data.Status.Should().Be(nameof(ComponentStatus.Operational));
    }

    [Fact]
    public async Task PostIncidentUpdate_GivenTheIncidentIsAlreadyResolved_IsRefused()
    {
        // Arrange
        var created = await CreateIncidentAsync(IncidentImpact.Major, ServiceComponent.Api);

        await _httpClient.PostAsJsonAsync($"incidents/{created.IncidentId}/updates", new
        {
            Status = IncidentStatus.Resolved,
            Body = "Resolved."
        });

        // Act
        var response = await _httpClient.PostAsJsonAsync($"incidents/{created.IncidentId}/updates", new
        {
            Status = IncidentStatus.Monitoring,
            Body = "It has come back."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PublishIncidentReview_GivenAnUnresolvedIncident_IsRefused()
    {
        // Arrange
        var created = await CreateIncidentAsync(IncidentImpact.Major, ServiceComponent.Api);

        // Act
        var response = await _httpClient.PutAsJsonAsync($"incidents/{created.IncidentId}/review", new
        {
            RootCause = "A connection pool exhausted under retry pressure.",
            Remediation = "The pool is now bounded per caller and the retry policy backs off."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PublishIncidentReview_GivenAResolvedIncident_IsPublished()
    {
        // Arrange
        var created = await CreateIncidentAsync(IncidentImpact.Major, ServiceComponent.Api);

        await _httpClient.PostAsJsonAsync($"incidents/{created.IncidentId}/updates", new
        {
            Status = IncidentStatus.Resolved,
            Body = "Resolved."
        });

        // Act
        var response = await _httpClient.PutAsJsonAsync($"incidents/{created.IncidentId}/review", new
        {
            RootCause = "A connection pool exhausted under retry pressure.",
            Remediation = "The pool is now bounded per caller and the retry policy backs off."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var reviewed = await response.Content.ReadDataFromJsonAsync<IncidentResponseDto>();
        reviewed!.RootCause.Should().NotBeNull();
        reviewed.ReviewPublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMaintenanceWindow_GivenAFutureWindow_AppearsOnThePublicPage()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddDays(3);

        // Act
        var response = await _httpClient.PostAsJsonAsync("maintenance-windows", new
        {
            Title = "Database upgrade",
            Description = "The primary will be restarted in a rolling fashion.",
            Components = new[] { ServiceComponent.Api },
            ScheduledStart = start,
            ScheduledEnd = start.AddHours(2)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var window = await response.Content.ReadDataFromJsonAsync<MaintenanceWindowResponseDto>();
        window!.Status.Should().Be(nameof(MaintenanceStatus.Scheduled));
        window.AnnouncedAt.Should().NotBeNull();

        var status = await _anonymousClient.GetFromJsonAsync<ResponseDataDto<ServiceStatusResponseDto>>("status");
        status!.Data.UpcomingMaintenance.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateMaintenanceWindow_GivenAWindowInThePast_IsRefused()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var response = await _httpClient.PostAsJsonAsync("maintenance-windows", new
        {
            Title = "Retrospective maintenance",
            Description = "Announced after the fact.",
            Components = new[] { ServiceComponent.Api },
            ScheduledStart = start,
            ScheduledEnd = start.AddHours(2)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Subscribe_GivenTheSameAddressTwice_AnswersTheSameWayBothTimes()
    {
        // Arrange
        var body = new { Email = "ops@example.com" };

        // Act
        var first = await _anonymousClient.PostAsJsonAsync("status/subscriptions", body);
        var second = await _anonymousClient.PostAsJsonAsync("status/subscriptions", body);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        second.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var firstBody = await first.Content.ReadDataFromJsonAsync<StatusSubscriptionResponseDto>();
        var secondBody = await second.Content.ReadDataFromJsonAsync<StatusSubscriptionResponseDto>();

        secondBody!.StatusSubscriptionId.Should().Be(firstBody!.StatusSubscriptionId);
        secondBody.Message.Should().Be(firstBody.Message);
    }

    [Fact]
    public async Task ConfirmSubscription_GivenATokenThatWasNeverIssued_IsRefused()
    {
        // Arrange
        var response = await _anonymousClient.PostAsJsonAsync("status/subscriptions", new { Email = "ops@example.com" });
        var subscription = await response.Content.ReadDataFromJsonAsync<StatusSubscriptionResponseDto>();

        // Act
        var confirmation = await _anonymousClient.GetAsync(
            $"status/subscriptions/{subscription!.StatusSubscriptionId}/confirm?token=not-the-token-that-was-sent");

        // Assert
        confirmation.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<IncidentResponseDto> CreateIncidentAsync(IncidentImpact impact, params ServiceComponent[] components)
    {
        var response = await _httpClient.PostAsJsonAsync("incidents", new
        {
            Title = "Elevated error rates",
            Summary = "A proportion of requests are being refused while we investigate.",
            Impact = impact,
            Components = components
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var incident = await response.Content.ReadDataFromJsonAsync<IncidentResponseDto>();

        return incident!;
    }

    [Fact]
    public async Task GetHeartbeat_GivenTheSweepHasNeverRun_ReportsStale()
    {
        // Act
        var response = await _anonymousClient.GetFromJsonAsync<ResponseDataDto<ServiceHeartbeatResponseDto>>("status/heartbeat");

        // Assert
        response!.Data.IsFresh.Should().BeFalse();
        response.Data.ExpectedIntervalSeconds.Should().Be(60);
        response.Data.Components.Should().OnlyContain(x => !x.IsFresh && x.LastProbedAt == null);
    }

    [Fact]
    public async Task RecordProbe_GivenAnExternalObservation_IsRecordedAgainstTheComponent()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync("status/probes", new
        {
            Component = ServiceComponent.Api,
            IsHealthy = false,
            LatencyMilliseconds = 0,
            Detail = "Connection refused from eu-west-1."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var probe = await response.Content.ReadDataFromJsonAsync<ComponentProbeResponseDto>();
        probe!.Source.Should().Be(nameof(ProbeSource.External));
        probe.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task RecordProbe_GivenAnObservationInTheFuture_IsRefused()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync("status/probes", new
        {
            Component = ServiceComponent.Api,
            IsHealthy = false,
            LatencyMilliseconds = 0,
            ObservedAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RecordProbe_GivenNoCredentials_IsRefused()
    {
        // Act
        var response = await _anonymousClient.PostAsJsonAsync("status/probes", new
        {
            Component = ServiceComponent.Api,
            IsHealthy = false,
            LatencyMilliseconds = 0
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecordProbe_GivenABackdatedOutage_RewritesThatDaysPublishedUptime()
    {
        // Arrange
        var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
        var yesterdaysDate = DateOnly.FromDateTime(yesterday.UtcDateTime);

        var before = await _anonymousClient.GetFromJsonAsync<ResponseDataDto<ServiceStatusResponseDto>>("status");
        before!.Data.Components.Single(x => x.Component == nameof(ServiceComponent.Api))
            .History.Should().NotContain(x => x.Date == yesterdaysDate);

        // Act
        var response = await _httpClient.PostAsJsonAsync("status/probes", new
        {
            Component = ServiceComponent.Api,
            IsHealthy = false,
            LatencyMilliseconds = 0,
            ObservedAt = yesterday,
            Detail = "Unreachable from eu-west-1."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var after = await _anonymousClient.GetFromJsonAsync<ResponseDataDto<ServiceStatusResponseDto>>("status");
        var day = after!.Data.Components
            .Single(x => x.Component == nameof(ServiceComponent.Api))
            .History.Single(x => x.Date == yesterdaysDate);

        day.TotalProbes.Should().Be(1);
        day.HealthyProbes.Should().Be(0);
        day.UptimePercentage.Should().Be(0m);
    }
}
