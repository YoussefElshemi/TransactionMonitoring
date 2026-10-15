using Core.Enums;
using Core.Helpers;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class PermissionHelperTests : BaseTestClass
{
    [Fact]
    public void ToPermissionString_ForEveryPermission_ReturnsADeclaredName()
    {
        // Arrange
        var permissions = Enum.GetValues<Permission>();

        // Act
        var act = () => permissions.Select(PermissionHelper.ToPermissionString).ToList();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ToPermissionString_ForEveryPermission_IsUnique()
    {
        // Act
        var names = Enum.GetValues<Permission>().Select(PermissionHelper.ToPermissionString).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ToPermissionString_ForEveryPermission_IsAResourceAndAnAction()
    {
        // Act
        var names = Enum.GetValues<Permission>().Select(PermissionHelper.ToPermissionString).ToList();

        // Assert
        names.Should().AllSatisfy(x => x.Split(':').Should().HaveCount(2));
    }

    [Fact]
    public void TryParse_ForEveryPermission_RoundTrips()
    {
        // Arrange
        var permissions = Enum.GetValues<Permission>();

        // Act
        var parsed = permissions.ToDictionary(
            x => x,
            x => PermissionHelper.TryParse(PermissionHelper.ToPermissionString(x), out var result) ? result : default);

        // Assert
        parsed.Should().AllSatisfy(x => x.Value.Should().Be(x.Key));
    }

    [Fact]
    public void TryParse_GivenAnUnknownValue_ReturnsFalse()
    {
        // Act
        var parsed = PermissionHelper.TryParse("cases:incinerate", out _);

        // Assert
        parsed.Should().BeFalse();
    }

    [Fact]
    public void TryParse_IsCaseInsensitive()
    {
        // Act
        var parsed = PermissionHelper.TryParse("CASES:READ", out var permission);

        // Assert
        parsed.Should().BeTrue();
        permission.Should().Be(Permission.CasesRead);
    }

    [Fact]
    public void ToPolicyName_PrefixesTheWireName()
    {
        // Act
        var policyName = PermissionHelper.ToPolicyName(Permission.RulesDeploy);

        // Assert
        policyName.Should().Be("permission:rules:deploy");
    }

    [Fact]
    public void TryParsePolicyName_RoundTripsAPolicyName()
    {
        // Act
        var parsed = PermissionHelper.TryParsePolicyName(PermissionHelper.ToPolicyName(Permission.CasesAssign), out var permission);

        // Assert
        parsed.Should().BeTrue();
        permission.Should().Be(Permission.CasesAssign);
    }

    [Fact]
    public void TryParsePolicyName_GivenABareName_ReturnsFalse()
    {
        // Act
        var parsed = PermissionHelper.TryParsePolicyName("cases:read", out _);

        // Assert
        parsed.Should().BeFalse();
    }

    [Fact]
    public void ToPermissionString_GivenAnUndefinedPermission_Throws()
    {
        // Act
        var act = () => PermissionHelper.ToPermissionString((Permission)9999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
