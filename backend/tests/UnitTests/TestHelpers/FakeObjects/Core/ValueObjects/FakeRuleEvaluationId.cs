using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeRuleEvaluationId
{
    public static RuleEvaluationId CreateValid(IFixture fixture)
    {
        return new RuleEvaluationId(fixture.Create<Guid>());
    }
}