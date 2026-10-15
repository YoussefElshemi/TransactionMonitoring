using System.Text.Json.Nodes;
using AutoFixture;
using Core.Enums;
using Core.Models.RuleEvaluations;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;

public static class FakeConditionMatch
{
    public static ConditionMatch CreateValid(IFixture fixture)
    {
        return new ConditionMatch
        {
            Field = fixture.Create<string>(),
            Condition = fixture.Create<RuleCondition>(),
            Expected = JsonValue.Create(fixture.Create<string>()),
            Actual = JsonValue.Create(fixture.Create<string>()),
            Matched = fixture.Create<bool>()
        };
    }
}
