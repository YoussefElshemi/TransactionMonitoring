using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.OrganizationUsers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationUsers;

namespace UnitTests.Tests.Presentation.Mappers.OrganizationUsers;

public class ListOrganizationUserRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListOrganizationUserRequest()
    {
        // Arrange
        var listOrganizationUserRequest = FakeListOrganizationUserRequestDto.CreateValid(Fixture);

        // Act
        var listOrganizationRequest = listOrganizationUserRequest.ToModel();

        // Assert
        listOrganizationRequest.OrganizationId.Should().Be(new OrganizationId(listOrganizationUserRequest.OrganizationId));
        listOrganizationRequest.UserName.Should().Be(new UserName(listOrganizationUserRequest.UserName!));
        listOrganizationRequest.PageNumber.Should().Be(new PageNumber(listOrganizationUserRequest.PageNumber));
        listOrganizationRequest.PageSize.Should().Be(new PageSize(listOrganizationUserRequest.PageSize));
        listOrganizationRequest.FromDate.Should().Be(new DateRange(listOrganizationUserRequest.FromDate));
        listOrganizationRequest.ToDate.Should().Be(new DateRange(listOrganizationUserRequest.ToDate));
    }
}