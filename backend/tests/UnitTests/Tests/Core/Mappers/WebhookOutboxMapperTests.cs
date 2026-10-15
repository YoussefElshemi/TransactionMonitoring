using Core.Constants;
using Core.Extensions;
using Core.Mappers.Webhooks;
using Core.Models.WebhookConfigurations;
using Core.Models.Webhooks;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;

namespace UnitTests.Tests.Core.Mappers;

public class WebhookOutboxMapperTests : BaseTestClass
{
    [Fact]
    public void ToOutboxMessageRequests_GivenAnEmptySubscription_DeliversEveryEvent()
    {
        // Arrange
        var webhookConfiguration = FakeWebhookConfiguration.CreateValid(Fixture) with { SubscribedEvents = [] };

        // Act
        var requests = new List<WebhookConfiguration> { webhookConfiguration }
            .ToOutboxMessageRequests(WebhookEvents.AlertRaised, new { }, MockTimeProvider.GetUtcNow());

        // Assert
        requests.Should().HaveCount(1);
        requests[0].Event!.Value.ToString().Should().Be(WebhookEvents.AlertRaised);
        requests[0].WebhookConfigurationId.Should().Be(webhookConfiguration.WebhookConfigurationId);
    }

    [Fact]
    public void ToOutboxMessageRequests_GivenAnUnsubscribedEvent_DeliversNothing()
    {
        // Arrange
        var webhookConfiguration = FakeWebhookConfiguration.CreateValid(Fixture) with
        {
            SubscribedEvents = [new WebhookEvent(WebhookEvents.CaseCreated)]
        };

        // Act
        var requests = new List<WebhookConfiguration> { webhookConfiguration }
            .ToOutboxMessageRequests(WebhookEvents.AlertRaised, new { }, MockTimeProvider.GetUtcNow());

        // Assert
        requests.Should().BeEmpty();
    }

    [Fact]
    public void ToOutboxMessageRequests_GivenAMixedSetOfEndpoints_DeliversOnlyToTheOnesThatAskedForIt()
    {
        // Arrange
        var subscribed = FakeWebhookConfiguration.CreateValid(Fixture) with
        {
            SubscribedEvents = [new WebhookEvent(WebhookEvents.CaseResolved), new WebhookEvent(WebhookEvents.AlertRaised)]
        };

        var narrowed = FakeWebhookConfiguration.CreateValid(Fixture) with
        {
            SubscribedEvents = [new WebhookEvent(WebhookEvents.CaseResolved)]
        };

        var everything = FakeWebhookConfiguration.CreateValid(Fixture) with { SubscribedEvents = [] };

        // Act
        var requests = new List<WebhookConfiguration> { subscribed, narrowed, everything }
            .ToOutboxMessageRequests(WebhookEvents.AlertRaised, new { }, MockTimeProvider.GetUtcNow());

        // Assert
        requests.Should().HaveCount(2);
        requests.Select(x => x.WebhookConfigurationId!.Value.ToString()).Should()
            .BeEquivalentTo(subscribed.WebhookConfigurationId.ToString(), everything.WebhookConfigurationId.ToString());
    }

    [Fact]
    public void ToOutboxMessageRequests_GivenAPinnedEndpoint_StampsTheEnvelopeWithItsVersion()
    {
        // Arrange
        var webhookConfiguration = FakeWebhookConfiguration.CreateValid(Fixture) with
        {
            SubscribedEvents = [],
            PayloadVersion = new WebhookPayloadVersion(WebhookPayloadVersions.V1)
        };

        // Act
        var requests = new List<WebhookConfiguration> { webhookConfiguration }
            .ToOutboxMessageRequests(WebhookEvents.CaseCreated, new { Anything = 1 }, MockTimeProvider.GetUtcNow());

        // Assert
        ((int)requests[0].PayloadVersion!.Value).Should().Be(WebhookPayloadVersions.V1);

        var envelope = requests[0].Payload.ToString()
            .FromBase64String()
            .TryDeserialize<WebhookMessage<object>>();

        envelope.Version.Should().Be(WebhookPayloadVersions.V1);
        envelope.Event.Should().Be(WebhookEvents.CaseCreated);
    }

    [Fact]
    public void ToOutboxMessageRequests_GivenTwoEndpointsOnTheSameVersion_RendersOnePayloadForBoth()
    {
        // Arrange
        var first = FakeWebhookConfiguration.CreateValid(Fixture) with { SubscribedEvents = [] };
        var second = FakeWebhookConfiguration.CreateValid(Fixture) with { SubscribedEvents = [] };

        // Act
        var requests = new List<WebhookConfiguration> { first, second }
            .ToOutboxMessageRequests(WebhookEvents.CaseCreated, new { Anything = 1 }, MockTimeProvider.GetUtcNow());

        // Assert
        requests[0].Payload.ToString().Should().Be(requests[1].Payload.ToString());
    }

    [Fact]
    public void IsSubscribedTo_GivenAnEventNotInTheSubscription_IsFalse()
    {
        // Arrange
        var webhookConfiguration = FakeWebhookConfiguration.CreateValid(Fixture) with
        {
            SubscribedEvents = [new WebhookEvent(WebhookEvents.CaseCreated)]
        };

        // Act
        var subscribed = webhookConfiguration.IsSubscribedTo(new WebhookEvent(WebhookEvents.CaseBreached));

        // Assert
        subscribed.Should().BeFalse();
    }
}
