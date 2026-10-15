using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.OrganizationWebhookConfigurations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

namespace UnitTests.Tests.Presentation.Mappers.OrganizationWebhookConfigurations;

public class ListOrganizationWebhookConfigurationRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListOrganizationWebhookConfigurationRequest()
    {
        // Arrange
        var listOrganizationWebhookRequestDto = FakeListOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

        // Act
        var listOrganizationWebhookRequest = listOrganizationWebhookRequestDto.ToModel();

        // Assert
        listOrganizationWebhookRequest.OrganizationId.Should().Be(new OrganizationId(listOrganizationWebhookRequestDto.OrganizationId));
        listOrganizationWebhookRequest.FriendlyName.Should().Be(new FriendlyName(listOrganizationWebhookRequestDto.FriendlyName!));
        listOrganizationWebhookRequest.PageNumber.Should().Be(new PageNumber(listOrganizationWebhookRequestDto.PageNumber));
        listOrganizationWebhookRequest.PageSize.Should().Be(new PageSize(listOrganizationWebhookRequestDto.PageSize));
        listOrganizationWebhookRequest.FromDate.Should().Be(new DateRange(listOrganizationWebhookRequestDto.FromDate));
        listOrganizationWebhookRequest.ToDate.Should().Be(new DateRange(listOrganizationWebhookRequestDto.ToDate));
    }
}