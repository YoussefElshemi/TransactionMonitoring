using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.OrganizationWebhookConfigurations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

namespace UnitTests.Tests.Presentation.Mappers.OrganizationWebhookConfigurations;

public class ListMyOrganizationWebhookConfigurationRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListMyOrganizationWebhookConfigurationRequest()
    {
        // Arrange
        var listMyOrganizationWebhookRequestDto = FakeListMyOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

        // Act
        var listOrganizationWebhookRequest = listMyOrganizationWebhookRequestDto.ToModel();

        // Assert
        listOrganizationWebhookRequest.OrganizationId.Should().Be(new OrganizationId(listMyOrganizationWebhookRequestDto.OrganizationId));
        listOrganizationWebhookRequest.FriendlyName.Should().Be(new FriendlyName(listMyOrganizationWebhookRequestDto.FriendlyName!));
        listOrganizationWebhookRequest.PageNumber.Should().Be(new PageNumber(listMyOrganizationWebhookRequestDto.PageNumber));
        listOrganizationWebhookRequest.PageSize.Should().Be(new PageSize(listMyOrganizationWebhookRequestDto.PageSize));
        listOrganizationWebhookRequest.FromDate.Should().Be(new DateRange(listMyOrganizationWebhookRequestDto.FromDate));
        listOrganizationWebhookRequest.ToDate.Should().Be(new DateRange(listMyOrganizationWebhookRequestDto.ToDate));
    }
}
