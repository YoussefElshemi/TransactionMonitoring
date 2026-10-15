using AutoFixture;
using Core.Enums;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeRuleBodyDto
{
    public static RuleBodyDto CreateBasicRule(IFixture fixture)
    {
        return new RuleBodyDto
        {
            Condition = RuleCondition.EqualTo.ToString(),
            Field = fixture.Create<string>(),
            Value = fixture.Create<string>()
        };
    }

    public static RuleBodyDto CreateCompositeRule(IFixture fixture)
    {
        return new RuleBodyDto
        {
            Condition = null,
            Field = null,
            Value = null
        };
    }
}