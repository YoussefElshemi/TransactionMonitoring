using Core.Mappers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

namespace UnitTests.Tests.Core.Mappers;

public class RuleBodyMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_CreateOrganizationRequest()
    {
        // Arrange
        var ruleBody = FakeRuleBody.CreateValid(Fixture);

        // Act
        var ruleBodyModel = ruleBody.ToModel();

        // Assert
        ruleBodyModel.Condition.Should().Be(ruleBody.Condition);
        ruleBodyModel.Field.Should().Be(new RuleField(ruleBody.Field!));
        ruleBodyModel.Value.Should().Be(RuleValue.ToObject(ruleBody.Value!.Value));
    }
}