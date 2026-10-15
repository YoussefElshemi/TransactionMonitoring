using FluentAssertions;
using Presentation.Mappers.Users;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;

namespace UnitTests.Tests.Presentation.Mappers.Users;

public class UserDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_ApplicationUser()
    {
        // Arrange
        var applicationUser = FakeApplicationUser.CreateValid(Fixture);

        // Act
        var userResponseDto = applicationUser.ToDto();

        // Assert
        userResponseDto.UserId.Should().Be(applicationUser.UserId);
        userResponseDto.Roles.Should().BeEquivalentTo(applicationUser.Roles.Select(x => x.ToString()).ToArray());
        userResponseDto.IsEnabled.Should().Be(applicationUser.IsEnabled);
        userResponseDto.Email.Should().Be(applicationUser.Email);
        userResponseDto.UserName.Should().Be(applicationUser.UserName);
        userResponseDto.IsEmailConfirmed.Should().Be(applicationUser.EmailConfirmed);
        userResponseDto.CreatedAt.Should().Be(applicationUser.CreatedAt);
        userResponseDto.UpdatedAt.Should().Be(applicationUser.UpdatedAt);
    }
}