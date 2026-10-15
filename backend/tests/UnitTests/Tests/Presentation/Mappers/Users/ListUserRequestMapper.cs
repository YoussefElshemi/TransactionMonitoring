using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Users;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Users;

namespace UnitTests.Tests.Presentation.Mappers.Users;

public class ListUserRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListUserRequestDto()
    {
        // Arrange
        var listUserRequestDto = FakeListUserRequestDto.CreateValid(Fixture);

        // Act
        var listUserRequest = listUserRequestDto.ToModel();

        // Assert
        listUserRequest.UserName.Should().Be(new UserName(listUserRequestDto.UserName!));
        listUserRequest.PageNumber.Should().Be(new PageNumber(listUserRequestDto.PageNumber));
        listUserRequest.PageSize.Should().Be(new PageSize(listUserRequestDto.PageSize));
        listUserRequest.FromDate.Should().Be(new DateRange(listUserRequestDto.FromDate));
        listUserRequest.ToDate.Should().Be(new DateRange(listUserRequestDto.ToDate));
    }
}