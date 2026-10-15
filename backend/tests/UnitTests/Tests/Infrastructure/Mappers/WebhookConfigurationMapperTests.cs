using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;
using UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class WebhookConfigurationMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_WebhookConfiguration()
    {
        // Arrange
        var webhookConfigurationEntity = FakeWebhookConfigurationEntity.CreateValid(Fixture);

        // Act
        var webhookConfiguration = webhookConfigurationEntity.ToModel();

        // Assert
        webhookConfiguration.WebhookConfigurationId.Should().Be(new WebhookConfigurationId(webhookConfigurationEntity.WebhookConfigurationId));
        webhookConfiguration.EndpointUrl.Should().Be(new Uri(webhookConfigurationEntity.EndpointUrl));
        webhookConfiguration.IsEnabled.Should().Be(new IsEnabled(webhookConfigurationEntity.IsEnabled));
        webhookConfiguration.OrganizationId.Should().Be(new OrganizationId(webhookConfigurationEntity.OrganizationId));
        webhookConfiguration.CreatedAt.Should().Be(new CreatedAt(webhookConfigurationEntity.CreatedAt));
        webhookConfiguration.UpdatedAt.Should().Be(new UpdatedAt(webhookConfigurationEntity.UpdatedAt));
    }

    [Fact]
    public void ToEntity_WebhookConfiguration()
    {
        // Arrange
        var webhookConfiguration = FakeWebhookConfiguration.CreateValid(Fixture);

        // Act
        var webhookConfigurationEntity = webhookConfiguration.ToEntity();

        // Assert
        webhookConfigurationEntity.WebhookConfigurationId.Should().Be(webhookConfiguration.WebhookConfigurationId);
        webhookConfigurationEntity.IsEnabled.Should().Be(webhookConfiguration.IsEnabled);
        webhookConfigurationEntity.EndpointUrl.Should().Be(webhookConfiguration.EndpointUrl.ToString());
        webhookConfigurationEntity.OrganizationId.Should().Be(webhookConfiguration.OrganizationId);
        webhookConfigurationEntity.CreatedAt.Should().Be(webhookConfiguration.CreatedAt);
        webhookConfigurationEntity.UpdatedAt.Should().Be(webhookConfiguration.UpdatedAt);
    }
}