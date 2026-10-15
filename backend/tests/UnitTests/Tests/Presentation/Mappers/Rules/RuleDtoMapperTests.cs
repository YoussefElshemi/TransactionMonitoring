using FluentAssertions;
using Presentation.Mappers.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.Tests.Presentation.Mappers.Rules;

public class RuleDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_Rule()
    {
        // Arrange
        var rule = FakeRule.CreateValid(Fixture);

        // Act
        var ruleDto = rule.ToDto();

        // Assert
        ruleDto.RuleId.Should().Be(rule.RuleId);
        ruleDto.FriendlyName.Should().Be(rule.FriendlyName);
        ruleDto.RuleBody.Should().BeEquivalentTo(rule.RuleBody.ToDto());
        ruleDto.IsEnabled.Should().Be(rule.IsEnabled);
        ruleDto.RuleSetId.Should().Be(rule.RuleSetId);
        ruleDto.CreatedAt.Should().Be(rule.CreatedAt);
        ruleDto.UpdatedAt.Should().Be(rule.UpdatedAt);
        ruleDto.IsDeleted.Should().Be(rule.IsDeleted);
    }
}