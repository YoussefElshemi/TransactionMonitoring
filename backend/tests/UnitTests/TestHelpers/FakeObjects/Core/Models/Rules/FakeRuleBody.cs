using AutoFixture;
using Core.Enums;
using Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

public static class FakeRuleBody
{
    public static RuleBody CreateValid(IFixture fixture)
    {
        return new RuleBody
        {
            Condition = fixture.Create<RuleCondition>(),
            Field = FakeRuleField.CreateValid(fixture),
            Value = FakeRuleValue.CreateValid(fixture),
            And = [],
            Or = [],
            Not = []
        };
    }
}