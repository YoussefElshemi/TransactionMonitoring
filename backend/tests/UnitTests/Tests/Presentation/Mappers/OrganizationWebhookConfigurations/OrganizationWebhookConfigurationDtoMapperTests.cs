using FluentAssertions;
using Presentation.Mappers.OrganizationWebhookConfigurations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;

namespace UnitTests.Tests.Presentation.Mappers.OrganizationWebhookConfigurations;

public class OrganizationWebhookConfigurationDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_OrganizationWebhookConfiguration()
    {
        // Arrange
        var organizationWebhookConfiguration = FakeWebhookConfiguration.CreateValid(Fixture);

        // Act
        var organizationWebhookConfigurationDto = organizationWebhookConfiguration.ToDto();

        // Assert
        organizationWebhookConfigurationDto.WebhookConfigurationId.Should().Be(organizationWebhookConfiguration.WebhookConfigurationId);
        organizationWebhookConfigurationDto.EndpointUrl.Should().Be(organizationWebhookConfiguration.EndpointUrl.ToString());
        organizationWebhookConfigurationDto.IsEnabled.Should().Be(organizationWebhookConfiguration.IsEnabled);
        organizationWebhookConfigurationDto.CreatedAt.Should().Be(organizationWebhookConfiguration.CreatedAt);
        organizationWebhookConfigurationDto.UpdatedAt.Should().Be(organizationWebhookConfiguration.UpdatedAt);
    }
}