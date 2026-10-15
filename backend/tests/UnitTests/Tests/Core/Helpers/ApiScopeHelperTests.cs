using Core.Enums;
using Core.Helpers;
using FluentAssertions;
using Presentation.Constants;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class ApiScopeHelperTests : BaseTestClass
{
    [Theory]
    [InlineData(ApiScope.RulesEvaluate, "rules:evaluate")]
    [InlineData(ApiScope.RuleSetsEvaluate, "rulesets:evaluate")]
    public void ToScopeString_GivenAKnownScope_ReturnsTheWireValue(ApiScope apiScope, string expected)
    {
        // Act
        var scopeString = ApiScopeHelper.ToScopeString(apiScope);

        // Assert
        scopeString.Should().Be(expected);
    }

    [Fact]
    public void ToScopeString_GivenAnUndefinedScope_Throws()
    {
        // Act
        var act = () => ApiScopeHelper.ToScopeString((ApiScope)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(ApiScope.RulesEvaluate)]
    [InlineData(ApiScope.RuleSetsEvaluate)]
    public void TryParse_GivenAWireValue_RoundTrips(ApiScope apiScope)
    {
        // Act
        var parsed = ApiScopeHelper.TryParse(ApiScopeHelper.ToScopeString(apiScope), out var result);

        // Assert
        parsed.Should().BeTrue();
        result.Should().Be(apiScope);
    }

    [Fact]
    public void TryParse_GivenAnUnknownValue_ReturnsFalse()
    {
        // Act
        var parsed = ApiScopeHelper.TryParse("rules:delete", out _);

        // Assert
        parsed.Should().BeFalse();
    }

    [Fact]
    public void ToPolicyName_ForEveryScope_MatchesTheDeclaredPolicyConstant()
    {
        // Arrange
        var declared = typeof(AuthConstants.ApiScopePolicies)
            .GetFields()
            .Where(x => x is { IsLiteral: true, IsInitOnly: false })
            .Select(x => (string)x.GetRawConstantValue()!)
            .ToList();

        // Act
        var generated = Enum.GetValues<ApiScope>().Select(ApiScopeHelper.ToPolicyName).ToList();

        // Assert
        generated.Should().BeEquivalentTo(declared);
    }
}
