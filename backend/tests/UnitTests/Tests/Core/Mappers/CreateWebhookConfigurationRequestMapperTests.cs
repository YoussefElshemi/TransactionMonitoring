using Core.Constants;
using Core.Enums;
using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;

namespace UnitTests.Tests.Core.Mappers;

public class CreateWebhookConfigurationRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateWebhookConfigurationRequestMapper()
    {
        // Arrange
        var createWebhookConfigurationTest = FakeCreateWebhookConfigurationRequest.CreateValid(Fixture);

        // Act
        var createWebhookConfiguration = createWebhookConfigurationTest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        createWebhookConfiguration.EndpointUrl.Should().Be(createWebhookConfigurationTest.EndpointUrl);
        createWebhookConfiguration.IsEnabled.Should().Be(createWebhookConfigurationTest.IsEnabled);
        createWebhookConfiguration.OrganizationId.Should().Be(createWebhookConfigurationTest.OrganizationId);
    }

    [Fact]
    public void ToModel_GivenTheSameEventTwice_KeepsItOnce()
    {
        // Arrange
        var createWebhookConfigurationRequest = FakeCreateWebhookConfigurationRequest.CreateValid(Fixture) with
        {
            SubscribedEvents =
            [
                new WebhookEvent(WebhookEvents.CaseCreated),
                new WebhookEvent(WebhookEvents.CaseResolved),
                new WebhookEvent(WebhookEvents.CaseCreated)
            ]
        };

        // Act
        var webhookConfiguration = createWebhookConfigurationRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        webhookConfiguration.SubscribedEvents.Select(x => x.ToString())
            .Should().BeEquivalentTo(WebhookEvents.CaseCreated, WebhookEvents.CaseResolved);
    }

    [Fact]
    public void ToModel_GivenNoPinnedVersion_TakesTheCurrentEnvelopeVersion()
    {
        // Arrange
        var createWebhookConfigurationRequest = FakeCreateWebhookConfigurationRequest.CreateValid(Fixture) with
        {
            PayloadVersion = null
        };

        // Act
        var webhookConfiguration = createWebhookConfigurationRequest.ToModel(MockTimeProvider, Mode.Live);

        // Assert
        ((int)webhookConfiguration.PayloadVersion).Should().Be(WebhookPayloadVersions.Current);
    }
}