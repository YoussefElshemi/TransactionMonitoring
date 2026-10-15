using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.OrganizationWebhookConfigurations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

namespace UnitTests.Tests.Presentation.Mappers.OrganizationWebhookConfigurations;

public class UpdateOrganizationWebhookConfigurationRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_WebhookConfiguration()
    {
        // Arrange
        var updateWebhookConfigurationRequestDto = FakeUpdateWebhookConfigurationRequestDto.CreateValid(Fixture);

        // Act
        var updateWebhookConfigurationRequest = updateWebhookConfigurationRequestDto.ToModel();

        // Assert
        updateWebhookConfigurationRequest.WebhookConfigurationId.Should().Be(new WebhookConfigurationId(updateWebhookConfigurationRequestDto.WebhookConfigurationId));
        updateWebhookConfigurationRequest.EndpointUrl.Should().Be(new Uri(updateWebhookConfigurationRequestDto.Body.EndpointUrl!));
        updateWebhookConfigurationRequest.IsEnabled.Should().Be(new IsEnabled(updateWebhookConfigurationRequestDto.Body.IsEnabled!.Value));
    }
}