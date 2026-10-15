using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class RuleBodyMapperTests : BaseTestClass
{
    [Fact]
    public void ToEntity_RuleBody()
    {
        // Arrange
        var ruleBodyModel = FakeRuleBodyModel.CreateValid(Fixture);

        // Act
        var ruleBody = ruleBodyModel.ToModel();

        // Assert
        ruleBody.Condition.Should().Be(ruleBodyModel.Condition);
        ruleBody.Field.Should().Be(new RuleField(ruleBodyModel.Field!));
        ruleBody.Value.Should().Be(new RuleValue(ruleBodyModel.Value!));
    }
}