using Core.Constants;
using Core.Enums;
using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Infrastructure.Repositories;

public class WebhookConfigurationRepositoryTests
{
    private static readonly DisabledReason Reason = new("Disabled automatically after 3 consecutive failed deliveries.");

    [Fact]
    public async Task RecordFailureAsync_GivenTheThresholdIsReached_SwitchesTheEndpointOff()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = await SeedAsync(context, consecutiveFailures: 2);
        var repository = new WebhookConfigurationRepository(context, new MockTimeProvider());

        // Act
        var outcome = await repository.RecordFailureAsync(
            new WebhookConfigurationId(entity.WebhookConfigurationId), new ConsecutiveFailures(3), Reason, CancellationToken.None);

        await context.SaveChangesAsync();

        // Assert
        ((bool)outcome.WasDisabled).Should().BeTrue();
        ((int)outcome.ConsecutiveFailures).Should().Be(3);

        var reloaded = await context.WebhookConfigurations.FirstAsync(x => x.WebhookConfigurationId == entity.WebhookConfigurationId);
        reloaded.IsEnabled.Should().BeFalse();
        reloaded.DisabledAt.Should().NotBeNull();
        reloaded.DisabledReason.Should().Be(Reason.ToString());
    }

    [Fact]
    public async Task RecordFailureAsync_GivenAnEndpointAlreadyDisabled_KeepsCountingButDoesNotDisableAgain()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = await SeedAsync(context, consecutiveFailures: 3, isEnabled: false, disabledAt: TestTimeConstants.Today);
        var repository = new WebhookConfigurationRepository(context, new MockTimeProvider());

        // Act
        var outcome = await repository.RecordFailureAsync(
            new WebhookConfigurationId(entity.WebhookConfigurationId), new ConsecutiveFailures(3), Reason, CancellationToken.None);

        // Assert
        ((bool)outcome.WasDisabled).Should().BeFalse();
        ((int)outcome.ConsecutiveFailures).Should().Be(4);
    }

    [Fact]
    public async Task RecordFailureAsync_GivenADeletedConfiguration_RecordsNothingRatherThanThrowing()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = await SeedAsync(context, consecutiveFailures: 2, isDeleted: true);
        var repository = new WebhookConfigurationRepository(context, new MockTimeProvider());

        // Act
        var outcome = await repository.RecordFailureAsync(
            new WebhookConfigurationId(entity.WebhookConfigurationId), new ConsecutiveFailures(3), Reason, CancellationToken.None);

        await context.SaveChangesAsync();

        // Assert
        ((bool)outcome.WasDisabled).Should().BeFalse();
        ((int)outcome.ConsecutiveFailures).Should().Be(0);

        var reloaded = await context.WebhookConfigurations
            .IgnoreQueryFilters()
            .FirstAsync(x => x.WebhookConfigurationId == entity.WebhookConfigurationId);

        reloaded.ConsecutiveFailures.Should().Be(2);
        reloaded.DisabledAt.Should().BeNull();
    }

    [Fact]
    public async Task RecordSuccessAsync_GivenADeletedConfiguration_DoesNothingRatherThanThrowing()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = await SeedAsync(context, consecutiveFailures: 2, isDeleted: true);
        var repository = new WebhookConfigurationRepository(context, new MockTimeProvider());

        // Act
        var recordSuccess = async () => await repository.RecordSuccessAsync(
            new WebhookConfigurationId(entity.WebhookConfigurationId), CancellationToken.None);

        // Assert
        await recordSuccess.Should().NotThrowAsync();

        var reloaded = await context.WebhookConfigurations
            .IgnoreQueryFilters()
            .FirstAsync(x => x.WebhookConfigurationId == entity.WebhookConfigurationId);

        reloaded.ConsecutiveFailures.Should().Be(2);
        reloaded.LastSucceededAt.Should().BeNull();
    }

    [Fact]
    public async Task RecordSuccessAsync_GivenAFailingEndpoint_ResetsTheCount()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = await SeedAsync(context, consecutiveFailures: 2);
        var repository = new WebhookConfigurationRepository(context, new MockTimeProvider());

        // Act
        await repository.RecordSuccessAsync(new WebhookConfigurationId(entity.WebhookConfigurationId), CancellationToken.None);
        await context.SaveChangesAsync();

        // Assert
        var reloaded = await context.WebhookConfigurations.FirstAsync(x => x.WebhookConfigurationId == entity.WebhookConfigurationId);
        reloaded.ConsecutiveFailures.Should().Be(0);
        reloaded.LastSucceededAt.Should().Be(TestTimeConstants.Today);
    }

    [Fact]
    public async Task UpdateAsync_GivenAReEnable_ClearsTheAutomaticDisableAndItsCounter()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = await SeedAsync(context, consecutiveFailures: 20, isEnabled: false, disabledAt: TestTimeConstants.Today);
        var repository = new WebhookConfigurationRepository(context, new MockTimeProvider());

        // Act
        await repository.UpdateAsync(new UpdateWebhookConfigurationRequest
        {
            WebhookConfigurationId = new WebhookConfigurationId(entity.WebhookConfigurationId),
            EndpointUrl = null,
            IsEnabled = new IsEnabled(true),
            SubscribedEvents = null,
            PayloadVersion = null
        }, CancellationToken.None);

        await context.SaveChangesAsync();

        // Assert
        var reloaded = await context.WebhookConfigurations.FirstAsync(x => x.WebhookConfigurationId == entity.WebhookConfigurationId);
        reloaded.IsEnabled.Should().BeTrue();
        reloaded.ConsecutiveFailures.Should().Be(0);
        reloaded.DisabledAt.Should().BeNull();
        reloaded.DisabledReason.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_GivenANewSubscription_ReplacesTheOldOneRatherThanMergingIt()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = await SeedAsync(context, subscribedEvents: [WebhookEvents.CaseCreated, WebhookEvents.CaseResolved]);
        var repository = new WebhookConfigurationRepository(context, new MockTimeProvider());

        // Act
        await repository.UpdateAsync(new UpdateWebhookConfigurationRequest
        {
            WebhookConfigurationId = new WebhookConfigurationId(entity.WebhookConfigurationId),
            EndpointUrl = null,
            IsEnabled = null,
            SubscribedEvents = [new WebhookEvent(WebhookEvents.CaseResolved), new WebhookEvent(WebhookEvents.AlertRaised)],
            PayloadVersion = null
        }, CancellationToken.None);

        await context.SaveChangesAsync();

        // Assert
        var reloaded = await repository.GetAsync(new WebhookConfigurationId(entity.WebhookConfigurationId), CancellationToken.None);

        reloaded.SubscribedEvents.Select(x => x.ToString())
            .Should().BeEquivalentTo(WebhookEvents.AlertRaised, WebhookEvents.CaseResolved);
    }

    [Fact]
    public async Task UpdateAsync_GivenAnEmptySubscription_WidensTheEndpointBackOutToEveryEvent()
    {
        // Arrange
        await using var context = CreateContext();
        var entity = await SeedAsync(context, subscribedEvents: [WebhookEvents.CaseCreated]);
        var repository = new WebhookConfigurationRepository(context, new MockTimeProvider());

        // Act
        await repository.UpdateAsync(new UpdateWebhookConfigurationRequest
        {
            WebhookConfigurationId = new WebhookConfigurationId(entity.WebhookConfigurationId),
            EndpointUrl = null,
            IsEnabled = null,
            SubscribedEvents = [],
            PayloadVersion = null
        }, CancellationToken.None);

        await context.SaveChangesAsync();

        // Assert
        var reloaded = await repository.GetAsync(new WebhookConfigurationId(entity.WebhookConfigurationId), CancellationToken.None);

        reloaded.SubscribedEvents.Should().BeEmpty();
    }

    private static TransactionMonitoringDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TransactionMonitoringDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
            .Options;

        return new TransactionMonitoringDbContext(options, new ModeContext { Current = Mode.Live });
    }

    private static async Task<WebhookConfigurationEntity> SeedAsync(
        TransactionMonitoringDbContext context,
        int consecutiveFailures = 0,
        bool isEnabled = true,
        bool isDeleted = false,
        DateTimeOffset? disabledAt = null,
        string[]? subscribedEvents = null)
    {
        var webhookConfigurationId = Guid.NewGuid();

        var entity = new WebhookConfigurationEntity
        {
            ModeId = (int)Mode.Live,
            WebhookConfigurationId = webhookConfigurationId,
            OrganizationId = Guid.NewGuid(),
            EndpointUrl = "https://example.com/hooks",
            SecretKey = "secret",
            IsEnabled = isEnabled,
            PayloadVersion = WebhookPayloadVersions.Current,
            ConsecutiveFailures = consecutiveFailures,
            DisabledAt = disabledAt,
            DisabledReason = disabledAt is null ? null : Reason.ToString(),
            SubscribedEvents = (subscribedEvents ?? []).Select(x => new WebhookConfigurationEventEntity
            {
                WebhookConfigurationId = webhookConfigurationId,
                Event = x,
                CreatedAt = TestTimeConstants.Today,
                UpdatedAt = TestTimeConstants.Today
            }).ToList(),
            CreatedAt = TestTimeConstants.Today,
            UpdatedAt = TestTimeConstants.Today,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? TestTimeConstants.Today : null
        };

        context.WebhookConfigurations.Add(entity);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return entity;
    }
}
