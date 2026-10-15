using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Organizations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

namespace UnitTests.Tests.Presentation.Mappers.Organizations;

public class ListOrganizationRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListOrganizationRequest()
    {
        // Arrange
        var listOrganizationRequestDto = FakeListOrganizationRequestDto.CreateValid(Fixture);

        // Act
        var listOrganizationRequest = listOrganizationRequestDto.ToModel();

        // Assert
        listOrganizationRequest.OrganizationId.Should().Be(new OrganizationId(listOrganizationRequestDto.OrganizationId));
        listOrganizationRequest.FriendlyName.Should().Be(new FriendlyName(listOrganizationRequestDto.FriendlyName!));
        listOrganizationRequest.PageNumber.Should().Be(new PageNumber(listOrganizationRequestDto.PageNumber));
        listOrganizationRequest.PageSize.Should().Be(new PageSize(listOrganizationRequestDto.PageSize));
        listOrganizationRequest.FromDate.Should().Be(new DateRange(listOrganizationRequestDto.FromDate));
        listOrganizationRequest.ToDate.Should().Be(new DateRange(listOrganizationRequestDto.ToDate));
    }
}