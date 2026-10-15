using AutoFixture;
using Presentation.Models.RuleSets;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleSets;

public static class FakeEvaluateAllRuleSetRequestDto
{
    public static EvaluateAllRuleSetRequestDto CreateValid(IFixture fixture)
    {
        return new EvaluateAllRuleSetRequestDto
        {
            Payload = FakeJsonObject.CreateValid(fixture),
            CreateCase = fixture.Create<bool>()
        };
    }
}