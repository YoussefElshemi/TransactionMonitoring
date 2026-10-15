using Core.Enums;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Mappers.Rules;

public class RuleBodyDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_RuleBodyDto()
    {
        // Arrange
        var ruleBodyDto = FakeRuleBodyDto.CreateBasicRule(Fixture);

        // Act
        var ruleBody = ruleBodyDto.ToModel();

        // Assert
        ruleBody.Condition.Should().Be(Enum.Parse<RuleCondition>(ruleBodyDto.Condition!, true));
        ruleBody.Field.Should().Be(new RuleField(ruleBodyDto.Field!));
        ruleBody.Value.Should().Be(new RuleValue(ruleBodyDto.Value!));
        ruleBody.And.Should().BeEquivalentTo(ruleBodyDto.And?.Select(x => x.ToModel()).ToList());
        ruleBody.Or.Should().BeEquivalentTo(ruleBodyDto.Or?.Select(x => x.ToModel()).ToList());
        ruleBody.Not.Should().BeEquivalentTo(ruleBodyDto.Not?.Select(x => x.ToModel()).ToList());
    }

    [Fact]
    public void ToDto_RuleBody()
    {
        // Arrange
        var ruleBody = FakeRuleBody.CreateValid(Fixture);

        // Act
        var ruleBodyDto = ruleBody.ToDto();

        // Assert
        ruleBodyDto.Condition.Should().Be(ruleBody.Condition.ToString());
        ruleBodyDto.Field.Should().Be(ruleBody.Field);
        ruleBodyDto.Value.Should().Be(ruleBody.Value.ToString());
        ruleBodyDto.And.Should().BeEquivalentTo(ruleBody.And?.Select(x => x.ToDto()).ToArray());
        ruleBodyDto.Or.Should().BeEquivalentTo(ruleBody.Or?.Select(x => x.ToDto()).ToArray());
        ruleBodyDto.Not.Should().BeEquivalentTo(ruleBody.Not?.Select(x => x.ToDto()).ToArray());
    }
}