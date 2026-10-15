using Core.Enums;
using Core.Helpers;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class RoleHelperTests : BaseTestClass
{
    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001", UserRole.User)]
    [InlineData("00000000-0000-0000-0000-000000000002", UserRole.Manager)]
    [InlineData("00000000-0000-0000-0000-000000000003", UserRole.Admin)]
    public void MapToUserRole_GivenRoleId_ReturnsRole(string roleId, UserRole expectedRole)
    {
        // Act
        var role = RoleHelper.MapToUserRole(Guid.Parse(roleId));

        // Assert
        role.Should().Be(expectedRole);
    }

    [Theory]
    [InlineData(UserRole.User, "00000000-0000-0000-0000-000000000001")]
    [InlineData(UserRole.Manager, "00000000-0000-0000-0000-000000000002")]
    [InlineData(UserRole.Admin, "00000000-0000-0000-0000-000000000003")]
    public void MapFromUserRole_GivenRole_ReturnsRoleId(UserRole expectedRole, string roleId)
    {
        // Act
        var role = RoleHelper.MapFromUserRole(expectedRole);

        // Assert
        role.Should().Be(Guid.Parse(roleId));
    }
}