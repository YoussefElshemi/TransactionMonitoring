using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using BehaviouralTests.TestHelpers;
using Core.Configs;
using Core.Constants;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Configurations;
using Infrastructure.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Models.Common;
using Presentation.Models.WebhookConfigurations;
using Presentation.Models.Webhooks;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace BehaviouralTests.Tests.Controllers;

public class WebhookDeliveryControllerTests(TestFixture testFixture) : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly Fixture _fixture = AutoFixtureHelper.Create();
    private readonly HttpClient _httpClient = testFixture.CreateClient();

    private const string Email = "deliveries@mail.com";
    private const string Password = "P@$$w0rd";
    private static readonly Guid OrganizationId = new("2c2ba0f0-6a1e-4d55-9a09-0f5f9b6f5a11");
    private static readonly Guid OtherOrganizationId = new("9f4f4a05-7e11-4c62-8a52-2c6f1b7a3d02");

    [Fact]
    public async Task GetWebhookCatalogue_ShouldPublishEverySubscribableEvent()
    {
        // Arrange
        await ArrangeAdminAsync();

        // Act
        var response = await _httpClient.GetAsync("/webhooks/catalogue");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var catalogue = await response.Content.ReadDataFromJsonAsync<WebhookCatalogueResponseDto>();
        catalogue.Should().NotBeNull();
        catalogue!.Events.Where(x => x.IsSubscribable).Select(x => x.Event)
            .Should().BeEquivalentTo(WebhookEvents.Subscribable);
        catalogue.CurrentPayloadVersion.Should().Be(WebhookPayloadVersions.Current);
        catalogue.SupportedPayloadVersions.Should().Contain(WebhookPayloadVersions.Current);
    }

    [Fact]
    public async Task CreateWebhookConfiguration_GivenEvents_ShouldPersistTheSubscription()
    {
        // Arrange
        await ArrangeAdminAsync();

        var body = new CreateWebhookConfigurationRequestBodyDto
        {
            EndpointUrl = "https://example.test/hooks",
            IsEnabled = true,
            Events = [WebhookEvents.CaseCreated, WebhookEvents.CaseResolved]
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadDataFromJsonAsync<WebhookConfigurationSecretResponseDto>();
        created!.Events.Should().BeEquivalentTo(WebhookEvents.CaseCreated, WebhookEvents.CaseResolved);
        created.PayloadVersion.Should().Be(WebhookPayloadVersions.Current);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        context.WebhookConfigurationEvents
            .Where(x => x.WebhookConfigurationId == created.WebhookConfigurationId)
            .Select(x => x.Event)
            .Should().BeEquivalentTo(WebhookEvents.CaseCreated, WebhookEvents.CaseResolved);
    }

    [Fact]
    public async Task CreateWebhookConfiguration_GivenAnUnpublishedEvent_ShouldReturnBadRequest()
    {
        // Arrange
        await ArrangeAdminAsync();

        var body = new CreateWebhookConfigurationRequestBodyDto
        {
            EndpointUrl = "https://example.test/hooks",
            IsEnabled = true,
            Events = ["case.create"]
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateWebhookConfiguration_GivenNoEvents_ShouldSubscribeToEverything()
    {
        // Arrange
        await ArrangeAdminAsync();

        var body = new CreateWebhookConfigurationRequestBodyDto
        {
            EndpointUrl = "https://example.test/hooks",
            IsEnabled = true
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-configurations", body);

        // Assert
        var created = await response.Content.ReadDataFromJsonAsync<WebhookConfigurationSecretResponseDto>();
        created!.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateWebhookConfiguration_GivenAnEmptyEventArray_ShouldWidenBackOutToEverything()
    {
        // Arrange
        await ArrangeAdminAsync();

        var webhookConfigurationId = await CreateConfigurationAsync([WebhookEvents.CaseCreated]);

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{webhookConfigurationId}",
            new UpdateWebhookConfigurationRequestBodyDto { Events = [] });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var configuration = await GetConfigurationAsync(webhookConfigurationId);
        configuration.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateWebhookConfiguration_GivenNoEventArray_ShouldLeaveTheSubscriptionAlone()
    {
        // Arrange
        await ArrangeAdminAsync();

        var webhookConfigurationId = await CreateConfigurationAsync([WebhookEvents.CaseCreated]);

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{webhookConfigurationId}",
            new UpdateWebhookConfigurationRequestBodyDto { EndpointUrl = "https://example.test/moved" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var configuration = await GetConfigurationAsync(webhookConfigurationId);
        configuration.Events.Should().BeEquivalentTo(WebhookEvents.CaseCreated);
        configuration.EndpointUrl.Should().Be("https://example.test/moved");
    }

    [Fact]
    public async Task ListWebhookDeliveries_ShouldReturnTheOrganizationsDeliveriesWithTheirResponses()
    {
        // Arrange
        await ArrangeAdminAsync();

        var webhookConfigurationId = await CreateConfigurationAsync([]);
        await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.CaseCreated, statusCode: 200, isProcessed: true);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/webhook-deliveries");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deliveries = await response.Content.ReadFromJsonAsync<PagedResponseDto<WebhookDeliveryResourceDto>>();
        deliveries!.Data.Should().HaveCount(1);

        var delivery = deliveries.Data.Single().WebhookDelivery;
        delivery.Event.Should().Be(WebhookEvents.CaseCreated);
        delivery.ResponseStatusCode.Should().Be(200);
        delivery.IsDelivered.Should().BeTrue();
        delivery.PayloadVersion.Should().Be(WebhookPayloadVersions.Current);
    }

    [Fact]
    public async Task ListWebhookDeliveries_GivenAnEventFilter_ShouldReturnOnlyThatEvent()
    {
        // Arrange
        await ArrangeAdminAsync();

        var webhookConfigurationId = await CreateConfigurationAsync([]);
        await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.CaseCreated, statusCode: 200, isProcessed: true);
        await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.AlertRaised, statusCode: 500, isProcessed: false);

        // Act
        var response = await _httpClient.GetAsync(
            $"/organizations/{OrganizationId}/webhook-deliveries?Event={WebhookEvents.AlertRaised}");

        // Assert
        var deliveries = await response.Content.ReadFromJsonAsync<PagedResponseDto<WebhookDeliveryResourceDto>>();
        deliveries!.Data.Should().HaveCount(1);
        deliveries.Data.Single().WebhookDelivery.Event.Should().Be(WebhookEvents.AlertRaised);
    }

    [Fact]
    public async Task ListWebhookDeliveries_GivenADeliveredFilter_ShouldSeparateTheFailures()
    {
        // Arrange
        await ArrangeAdminAsync();

        var webhookConfigurationId = await CreateConfigurationAsync([]);
        await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.CaseCreated, statusCode: 200, isProcessed: true);
        await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.AlertRaised, statusCode: 500, isProcessed: false);

        // Act
        var response = await _httpClient.GetAsync(
            $"/organizations/{OrganizationId}/webhook-deliveries?IsDelivered=false");

        // Assert
        var deliveries = await response.Content.ReadFromJsonAsync<PagedResponseDto<WebhookDeliveryResourceDto>>();
        deliveries!.Data.Should().HaveCount(1);
        deliveries.Data.Single().WebhookDelivery.ResponseStatusCode.Should().Be(500);
    }

    [Fact]
    public async Task ListWebhookDeliveries_ShouldNotReturnAnotherOrganizationsDeliveries()
    {
        // Arrange
        await ArrangeAdminAsync();

        var mine = await CreateConfigurationAsync([]);
        await ArrangeDeliveryAsync(mine, WebhookEvents.CaseCreated, statusCode: 200, isProcessed: true);
        await ArrangeDeliveryAsync(null, WebhookEvents.CaseCreated, statusCode: 200, isProcessed: true, organizationId: OtherOrganizationId);

        // Act
        var response = await _httpClient.GetAsync($"/organizations/{OrganizationId}/webhook-deliveries");

        // Assert
        var deliveries = await response.Content.ReadFromJsonAsync<PagedResponseDto<WebhookDeliveryResourceDto>>();
        deliveries!.Data.Should().HaveCount(1);
        deliveries.Data.Single().WebhookDelivery.WebhookConfigurationId.Should().Be(mine);
    }

    [Fact]
    public async Task ReplayWebhookDelivery_ShouldQueueANewRowAndLeaveTheOriginalAlone()
    {
        // Arrange
        await ArrangeAdminAsync();

        var webhookConfigurationId = await CreateConfigurationAsync([]);
        var original = await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.CaseCreated, statusCode: 500, isProcessed: false);

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/{OrganizationId}/webhook-deliveries/{original}/replay", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var replay = await response.Content.ReadDataFromJsonAsync<WebhookDeliveryResourceDto>();
        replay!.WebhookDelivery.ReplayOfOutboxMessageId.Should().Be(original);
        replay.WebhookDelivery.Attempts.Should().Be(0);
        replay.WebhookDelivery.IsDelivered.Should().BeFalse();

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var untouched = context.OutboxMessages.First(x => x.OutboxMessageId == original);
        untouched.ResponseStatusCode.Should().Be(500);
        untouched.ReplayOfOutboxMessageId.Should().BeNull();
    }

    [Fact]
    public async Task ReplayWebhookDelivery_GivenAnotherOrganizationsDelivery_ShouldReturnNotFound()
    {
        // Arrange
        await ArrangeAdminAsync();

        var theirs = await ArrangeDeliveryAsync(
            null, WebhookEvents.CaseCreated, statusCode: 500, isProcessed: false, organizationId: OtherOrganizationId);

        // Act
        var response = await _httpClient.PostAsync(
            $"/organizations/{OrganizationId}/webhook-deliveries/{theirs}/replay", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReplayWebhookDeliveryRange_ShouldQueueTheFailuresInTheWindow()
    {
        // Arrange
        await ArrangeAdminAsync();

        var webhookConfigurationId = await CreateConfigurationAsync([]);
        await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.CaseCreated, statusCode: 500, isProcessed: false);
        await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.AlertRaised, statusCode: 500, isProcessed: false);
        await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.CaseResolved, statusCode: 200, isProcessed: true);

        var body = new ReplayWebhookDeliveryRangeRequestBodyDto
        {
            From = DateTimeOffset.UtcNow.AddDays(-1),
            To = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-deliveries/replay", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var batch = await response.Content.ReadDataFromJsonAsync<WebhookReplayBatchResponseDto>();
        batch!.Matched.Should().Be(2);
        batch.Queued.Should().Be(2);
        batch.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ReplayWebhookDeliveryRange_GivenAWindowThatEndsBeforeItStarts_ShouldReturnBadRequest()
    {
        // Arrange
        await ArrangeAdminAsync();

        var body = new ReplayWebhookDeliveryRangeRequestBodyDto
        {
            From = DateTimeOffset.UtcNow,
            To = DateTimeOffset.UtcNow.AddDays(-1)
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/organizations/{OrganizationId}/webhook-deliveries/replay", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWebhookConfigurationHealth_ShouldTallyTheWindowAndReportTheDisableReason()
    {
        // Arrange
        await ArrangeAdminAsync();

        var webhookConfigurationId = await CreateConfigurationAsync([]);
        await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.CaseCreated, statusCode: 200, isProcessed: true);
        await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.AlertRaised, statusCode: 500, isProcessed: false);

        // Act
        var response = await _httpClient.GetAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{webhookConfigurationId}/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var health = await response.Content.ReadDataFromJsonAsync<WebhookEndpointHealthResponseDto>();
        health!.WebhookConfigurationId.Should().Be(webhookConfigurationId);
        health.Delivered.Should().Be(1);

        health.Pending.Should().Be(1);
        health.Failed.Should().Be(0);
        health.DisabledAt.Should().BeNull();
    }

    [Fact]
    public async Task RecordOutcomeAsync_GivenEnoughExhaustedDeliveries_ShouldDisableTheEndpointAndStopDispatchingToIt()
    {
        // Arrange
        await ArrangeAdminAsync();

        var webhookConfigurationId = await CreateConfigurationAsync([]);
        var outboxMessageId = await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.CaseCreated, statusCode: 500, isProcessed: false);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var appConfig = scope.ServiceProvider.GetRequiredService<IOptions<AppConfig>>().Value;
        var webhookHealthService = scope.ServiceProvider.GetRequiredService<IWebhookHealthService>();
        var outboxMessageRepository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var outboxMessage = await outboxMessageRepository.GetAsync(new OutboxMessageId(outboxMessageId), CancellationToken.None);
        var exhausted = new Attempts(appConfig.BackgroundServiceConfig.OutboxMaxAttempts);

        // Act
        for (var i = 0; i < appConfig.WebhookConfig.AutoDisableAfterConsecutiveFailures; i++)
        {
            await webhookHealthService.RecordOutcomeAsync(outboxMessage, new Result(false), exhausted, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }

        // Assert
        var disabled = context.WebhookConfigurations.First(x => x.WebhookConfigurationId == webhookConfigurationId);
        disabled.IsEnabled.Should().BeFalse();
        disabled.DisabledAt.Should().NotBeNull();
        disabled.DisabledReason.Should().NotBeNull();
        disabled.ConsecutiveFailures.Should().Be(appConfig.WebhookConfig.AutoDisableAfterConsecutiveFailures);

        var webhookConfigurationRepository = scope.ServiceProvider.GetRequiredService<IWebhookConfigurationRepository>();
        var dispatchable = await webhookConfigurationRepository.GetAllAsync(new OrganizationId(OrganizationId), CancellationToken.None);
        dispatchable.Should().NotContain(x => x.WebhookConfigurationId == new WebhookConfigurationId(webhookConfigurationId));
    }

    [Fact]
    public async Task RecordOutcomeAsync_GivenASuccessAfterFailures_ShouldClearTheCount()
    {
        // Arrange
        await ArrangeAdminAsync();

        var webhookConfigurationId = await CreateConfigurationAsync([]);
        var outboxMessageId = await ArrangeDeliveryAsync(webhookConfigurationId, WebhookEvents.CaseCreated, statusCode: 500, isProcessed: false);

        using var scope = testFixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        var appConfig = scope.ServiceProvider.GetRequiredService<IOptions<AppConfig>>().Value;
        var webhookHealthService = scope.ServiceProvider.GetRequiredService<IWebhookHealthService>();
        var outboxMessageRepository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var outboxMessage = await outboxMessageRepository.GetAsync(new OutboxMessageId(outboxMessageId), CancellationToken.None);
        var exhausted = new Attempts(appConfig.BackgroundServiceConfig.OutboxMaxAttempts);

        await webhookHealthService.RecordOutcomeAsync(outboxMessage, new Result(false), exhausted, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        await webhookHealthService.RecordOutcomeAsync(outboxMessage, new Result(true), new Attempts(1), CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        var recovered = context.WebhookConfigurations.First(x => x.WebhookConfigurationId == webhookConfigurationId);
        recovered.ConsecutiveFailures.Should().Be(0);
        recovered.LastSucceededAt.Should().NotBeNull();
        recovered.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateWebhookConfiguration_GivenAReEnable_ShouldClearTheAutomaticDisable()
    {
        // Arrange
        await ArrangeAdminAsync();

        var webhookConfigurationId = await CreateConfigurationAsync([]);

        using (var scope = testFixture.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
            var entity = context.WebhookConfigurations.First(x => x.WebhookConfigurationId == webhookConfigurationId);
            entity.IsEnabled = false;
            entity.ConsecutiveFailures = 20;
            entity.DisabledAt = DateTimeOffset.UtcNow;
            entity.DisabledReason = "broke";
            context.WebhookConfigurations.Update(entity);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _httpClient.PatchAsJsonAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{webhookConfigurationId}",
            new UpdateWebhookConfigurationRequestBodyDto { IsEnabled = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var configuration = await GetConfigurationAsync(webhookConfigurationId);
        configuration.IsEnabled.Should().BeTrue();
        configuration.DisabledAt.Should().BeNull();
        configuration.DisabledReason.Should().BeNull();

        using var verifyScope = testFixture.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<TransactionMonitoringDbContext>();
        verifyContext.WebhookConfigurations
            .First(x => x.WebhookConfigurationId == webhookConfigurationId)
            .ConsecutiveFailures.Should().Be(0);
    }

    private async Task ArrangeAdminAsync()
    {
        await AuthenticationHelper.CreateUserAsync(testFixture.Services, Email, Password, [UserRole.User, UserRole.Admin], OrganizationId);
        await AuthenticationHelper.SetTokenAsync(_httpClient, Email, Password);
    }

    private async Task<Guid> CreateConfigurationAsync(string[] events)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/organizations/{OrganizationId}/webhook-configurations",
            new CreateWebhookConfigurationRequestBodyDto
            {
                EndpointUrl = "https://example.test/hooks",
                IsEnabled = true,
                Events = events
            });

        var created = await response.Content.ReadDataFromJsonAsync<WebhookConfigurationSecretResponseDto>();

        return created!.WebhookConfigurationId;
    }

    private async Task<WebhookConfigurationResponseDto> GetConfigurationAsync(Guid webhookConfigurationId)
    {
        var response = await _httpClient.GetAsync(
            $"/organizations/{OrganizationId}/webhook-configurations/{webhookConfigurationId}");

        var resource = await response.Content.ReadDataFromJsonAsync<WebhookConfigurationResourceDto>();

        return resource!.WebhookConfiguration;
    }

    private async Task<Guid> ArrangeDeliveryAsync(
        Guid? webhookConfigurationId,
        string @event,
        int statusCode,
        bool isProcessed,
        Guid? organizationId = null)
    {
        var outboxMessageEntity = FakeOutboxMessageEntity.CreateValid(_fixture) with
        {
            OrganizationId = organizationId ?? OrganizationId,
            Organization = null,
            WebhookConfigurationId = webhookConfigurationId,
            WebhookConfiguration = null,
            Event = @event,
            PayloadVersion = WebhookPayloadVersions.Current,
            ChannelId = (int)NotificationChannel.Webhook,
            Payload = "{}".ToBase64String(),
            Attempts = 1,
            ResponseStatusCode = statusCode,
            IsProcessed = isProcessed,
            ProcessedAt = isProcessed ? DateTimeOffset.UtcNow : null,
            SuppressedAt = null,
            IsDeleted = false,
            DeletedAt = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await DatabaseHelper.CreateOutboxMessages(testFixture.Services, [outboxMessageEntity]);

        return outboxMessageEntity.OutboxMessageId;
    }

    public async Task InitializeAsync()
    {
        await testFixture.ResetDatabaseAsync();

        var organizationEntities = new List<OrganizationEntity>
        {
            OrganizationConfigurations.DefaultOrganization,
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            },
            FakeOrganizationEntity.CreateValid(_fixture) with
            {
                OrganizationId = OtherOrganizationId,
                StatusId = (int)OrganizationStatus.Enabled,
                IsDeleted = false
            }
        };

        await DatabaseHelper.CreateOrganizations(testFixture.Services, organizationEntities);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
