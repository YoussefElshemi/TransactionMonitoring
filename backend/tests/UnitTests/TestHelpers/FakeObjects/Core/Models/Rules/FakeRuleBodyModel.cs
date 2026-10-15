using AutoFixture;
using Core.Enums;
using Core.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

public static class FakeRuleBodyModel
{
    public static RuleBodyModel CreateValid(IFixture fixture)
    {
        return new RuleBodyModel
        {
            Condition = fixture.Create<RuleCondition>(),
            Field = fixture.Create<string>(),
            Value = fixture.Create<string>()
        };
    }
}