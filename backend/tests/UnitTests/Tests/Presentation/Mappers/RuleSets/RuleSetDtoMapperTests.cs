using FluentAssertions;
using Presentation.Mappers.RuleSets;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;

namespace UnitTests.Tests.Presentation.Mappers.RuleSets;

public class RuleSetDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_RuleSet()
    {
        // Arrange
        var ruleSet = FakeRuleSet.CreateValid(Fixture);

        // Act
        var ruleSetDto = ruleSet.ToDto();

        // Assert
        ruleSetDto.RuleSetId.Should().Be(ruleSet.RuleSetId);
        ruleSetDto.FriendlyName.Should().Be(ruleSet.FriendlyName);
        ruleSetDto.RuleCount.Should().Be(ruleSet.Rules.Count(x => !x.IsDeleted));
        ruleSetDto.IsEnabled.Should().Be(ruleSet.IsEnabled);
        ruleSetDto.CreatedAt.Should().Be(ruleSet.CreatedAt);
        ruleSetDto.UpdatedAt.Should().Be(ruleSet.UpdatedAt);
    }
}