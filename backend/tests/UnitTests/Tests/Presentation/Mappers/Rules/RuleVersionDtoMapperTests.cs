using FluentAssertions;
using Presentation.Mappers.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.Tests.Presentation.Mappers.Rules;

public class RuleVersionDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_RuleVersion()
    {
        // Arrange
        var ruleVersion = FakeRuleVersion.CreateValid(Fixture);

        // Act
        var dto = ruleVersion.ToDto();

        // Assert
        dto.RuleVersionId.Should().Be(ruleVersion.RuleVersionId);
        dto.RuleId.Should().Be(ruleVersion.RuleId);
        dto.VersionNumber.Should().Be(ruleVersion.VersionNumber);
        dto.RuleBody.Should().BeEquivalentTo(ruleVersion.RuleBody.ToDto());
        dto.MatchPolarity.Should().Be(ruleVersion.MatchPolarity.ToString());
        dto.Action.Should().Be(ruleVersion.Action.ToString());
        dto.Severity.Should().Be(ruleVersion.Severity.ToString());
        dto.RiskWeight.Should().Be(ruleVersion.RiskWeight);
        dto.RuleSetId.Should().Be(ruleVersion.RuleSetId);
        dto.CreatedAt.Should().Be(ruleVersion.CreatedAt);
    }
}
