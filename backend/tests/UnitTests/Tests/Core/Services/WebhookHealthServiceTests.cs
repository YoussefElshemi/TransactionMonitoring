using Core.Configs;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.NotificationSubscriptions;
using Core.Models.OutboxMessages;
using Core.Models.WebhookConfigurations;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Localization;
using UnitTests.TestHelpers.FakeObjects.Core.Models.OutboxMessages;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class WebhookHealthServiceTests : BaseTestClass
{
    private const int MaxAttempts = 5;
    private const int DisableThreshold = 3;

    private readonly Mock<IWebhookConfigurationRepository> _mockWebhookConfigurationRepository = new();
    private readonly Mock<IOutboxMessageRepository> _mockOutboxMessageRepository = new();
    private readonly Mock<IDisplayPreferenceService> _mockDisplayPreferenceService = new();
    private readonly Mock<INotificationSubscriptionRepository> _mockNotificationSubscriptionRepository = new();
    private readonly WebhookHealthService _sut;

    public WebhookHealthServiceTests()
    {
        _mockDisplayPreferenceService
            .Setup(x => x.ForOrganizationAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeDisplayPreferences.CreateValid());

        _sut = new WebhookHealthService(
            _mockWebhookConfigurationRepository.Object,
            _mockOutboxMessageRepository.Object,
            _mockDisplayPreferenceService.Object,
            _mockNotificationSubscriptionRepository.Object,
            Options.Create(new AppConfig
            {
                BackgroundServiceConfig = new BackgroundServiceConfig { OutboxMaxAttempts = MaxAttempts },
                WebhookConfig = new WebhookConfig { AutoDisableAfterConsecutiveFailures = DisableThreshold }
            }),
            MockTimeProvider);
    }

    [Fact]
    public async Task RecordOutcomeAsync_GivenASuccess_ResetsTheFailureCount()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);

        // Act
        await _sut.RecordOutcomeAsync(outboxMessage, new Result(true), new Attempts(1), CancellationToken.None);

        // Assert
        _mockWebhookConfigurationRepository.Verify(x => x.RecordSuccessAsync(
            outboxMessage.WebhookConfigurationId!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _mockWebhookConfigurationRepository.Verify(x => x.RecordFailureAsync(
            It.IsAny<WebhookConfigurationId>(), It.IsAny<ConsecutiveFailures>(), It.IsAny<DisabledReason>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordOutcomeAsync_GivenAFailureWithRetriesLeft_CountsNothing()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);

        // Act
        await _sut.RecordOutcomeAsync(outboxMessage, new Result(false), new Attempts(MaxAttempts - 1), CancellationToken.None);

        // Assert
        _mockWebhookConfigurationRepository.Verify(x => x.RecordFailureAsync(
            It.IsAny<WebhookConfigurationId>(), It.IsAny<ConsecutiveFailures>(), It.IsAny<DisabledReason>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordOutcomeAsync_GivenAFailureThatUsedItsWholeBudget_CountsOne()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);

        _mockWebhookConfigurationRepository
            .Setup(x => x.RecordFailureAsync(
                It.IsAny<WebhookConfigurationId>(), It.IsAny<ConsecutiveFailures>(), It.IsAny<DisabledReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Outcome(1, wasDisabled: false));

        // Act
        await _sut.RecordOutcomeAsync(outboxMessage, new Result(false), new Attempts(MaxAttempts), CancellationToken.None);

        // Assert
        _mockWebhookConfigurationRepository.Verify(x => x.RecordFailureAsync(
            outboxMessage.WebhookConfigurationId!.Value,
            It.Is<ConsecutiveFailures>(t => t == DisableThreshold),
            It.IsAny<DisabledReason>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockOutboxMessageRepository.Verify(x => x.AddRangeAsync(
            It.IsAny<List<OutboxMessage>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordOutcomeAsync_GivenTheThresholdIsReached_PassesTheThresholdAndAReasonNamingTheEndpoint()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);

        DisabledReason? reason = null;
        _mockWebhookConfigurationRepository
            .Setup(x => x.RecordFailureAsync(
                It.IsAny<WebhookConfigurationId>(), It.IsAny<ConsecutiveFailures>(), It.IsAny<DisabledReason>(), It.IsAny<CancellationToken>()))
            .Callback<WebhookConfigurationId, ConsecutiveFailures, DisabledReason, CancellationToken>((_, _, x, _) => reason = x)
            .ReturnsAsync(Outcome(DisableThreshold, wasDisabled: true));

        _mockNotificationSubscriptionRepository
            .Setup(x => x.ListEnabledAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _sut.RecordOutcomeAsync(outboxMessage, new Result(false), new Attempts(MaxAttempts), CancellationToken.None);

        // Assert
        _mockWebhookConfigurationRepository.Verify(x => x.RecordFailureAsync(
            outboxMessage.WebhookConfigurationId!.Value,
            It.Is<ConsecutiveFailures>(t => t == DisableThreshold),
            It.IsAny<DisabledReason>(),
            It.IsAny<CancellationToken>()), Times.Once);

        reason!.Value.ToString().Should().Contain(outboxMessage.Destination.ToString());
        reason.Value.ToString().Should().Contain(DisableThreshold.ToString());
    }

    [Fact]
    public async Task RecordOutcomeAsync_GivenAnEndpointAlreadyDisabled_DoesNotAnnounceItAgain()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);

        _mockWebhookConfigurationRepository
            .Setup(x => x.RecordFailureAsync(
                It.IsAny<WebhookConfigurationId>(), It.IsAny<ConsecutiveFailures>(), It.IsAny<DisabledReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Outcome(DisableThreshold + 4, wasDisabled: false));

        // Act
        await _sut.RecordOutcomeAsync(outboxMessage, new Result(false), new Attempts(MaxAttempts), CancellationToken.None);

        // Assert
        _mockNotificationSubscriptionRepository.Verify(x => x.ListEnabledAsync(
            It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordOutcomeAsync_GivenTheEndpointIsDisabled_NotifiesEverySubscriberExceptTheWebhookOnes()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture);

        _mockWebhookConfigurationRepository
            .Setup(x => x.RecordFailureAsync(
                It.IsAny<WebhookConfigurationId>(), It.IsAny<ConsecutiveFailures>(), It.IsAny<DisabledReason>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Outcome(DisableThreshold, wasDisabled: true));

        _mockNotificationSubscriptionRepository
            .Setup(x => x.ListEnabledAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Subscription(NotificationChannel.Email),
                Subscription(NotificationChannel.Chat),
                Subscription(NotificationChannel.Webhook)
            ]);

        List<OutboxMessage>? staged = null;
        _mockOutboxMessageRepository
            .Setup(x => x.AddRangeAsync(It.IsAny<List<OutboxMessage>>(), It.IsAny<CancellationToken>()))
            .Callback<List<OutboxMessage>, CancellationToken>((x, _) => staged = x);

        // Act
        await _sut.RecordOutcomeAsync(outboxMessage, new Result(false), new Attempts(MaxAttempts), CancellationToken.None);

        // Assert
        staged.Should().NotBeNull();
        staged!.Should().HaveCount(2);
        staged.Should().NotContain(x => x.Channel == NotificationChannel.Webhook);
    }

    [Fact]
    public async Task RecordOutcomeAsync_GivenARowWithNoConfiguration_DoesNothing()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture) with { WebhookConfigurationId = null };

        // Act
        await _sut.RecordOutcomeAsync(outboxMessage, new Result(false), new Attempts(MaxAttempts), CancellationToken.None);

        // Assert
        _mockWebhookConfigurationRepository.Verify(x => x.RecordFailureAsync(
            It.IsAny<WebhookConfigurationId>(), It.IsAny<ConsecutiveFailures>(), It.IsAny<DisabledReason>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordOutcomeAsync_GivenANonWebhookRow_DoesNothing()
    {
        // Arrange
        var outboxMessage = FakeOutboxMessage.CreateValid(Fixture) with { Channel = NotificationChannel.Email };

        // Act
        await _sut.RecordOutcomeAsync(outboxMessage, new Result(true), new Attempts(1), CancellationToken.None);

        // Assert
        _mockWebhookConfigurationRepository.Verify(x => x.RecordSuccessAsync(
            It.IsAny<WebhookConfigurationId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static WebhookFailureOutcome Outcome(int consecutiveFailures, bool wasDisabled)
    {
        return new WebhookFailureOutcome
        {
            ConsecutiveFailures = new ConsecutiveFailures(consecutiveFailures),
            WasDisabled = new WasDisabled(wasDisabled)
        };
    }

    private NotificationSubscription Subscription(NotificationChannel channel)
    {
        return new NotificationSubscription
        {
            Mode = Mode.Live,
            NotificationSubscriptionId = new NotificationSubscriptionId(Guid.NewGuid()),
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            Channel = channel,
            Destination = FakeDestination.CreateValid(Fixture),
            MinimumSeverity = null,
            NotifyOnBreach = new NotifyOnBreach(true),
            IsEnabled = new IsEnabled(true),
            Locale = null,
            CreatedAt = FakeCreatedAt.CreateValid(Fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(Fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
