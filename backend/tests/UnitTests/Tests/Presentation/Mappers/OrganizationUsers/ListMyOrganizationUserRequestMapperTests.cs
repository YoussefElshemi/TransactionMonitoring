using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.OrganizationUsers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationUsers;

namespace UnitTests.Tests.Presentation.Mappers.OrganizationUsers;

public class ListMyOrganizationUserRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListMyOrganizationUserRequest()
    {
        // Arrange
        var listMyOrganizationUserRequest = FakeListMyOrganizationUserRequestDto.CreateValid(Fixture);

        // Act
        var listOrganizationUserRequest = listMyOrganizationUserRequest.ToModel();

        // Assert
        listOrganizationUserRequest.OrganizationId.Should().Be(new OrganizationId(listMyOrganizationUserRequest.OrganizationId));
        listOrganizationUserRequest.UserName.Should().Be(new UserName(listMyOrganizationUserRequest.UserName!));
        listOrganizationUserRequest.PageNumber.Should().Be(new PageNumber(listMyOrganizationUserRequest.PageNumber));
        listOrganizationUserRequest.PageSize.Should().Be(new PageSize(listMyOrganizationUserRequest.PageSize));
        listOrganizationUserRequest.FromDate.Should().Be(new DateRange(listMyOrganizationUserRequest.FromDate));
        listOrganizationUserRequest.ToDate.Should().Be(new DateRange(listMyOrganizationUserRequest.ToDate));
    }
}
