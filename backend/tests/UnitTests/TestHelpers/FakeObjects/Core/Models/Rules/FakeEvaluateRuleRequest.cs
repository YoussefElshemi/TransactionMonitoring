using AutoFixture;
using Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

public static class FakeEvaluateRuleRequest
{
    public static EvaluateRuleRequest CreateValid(IFixture fixture)
    {
        return new EvaluateRuleRequest
        {
            RuleId = FakeRuleId.CreateValid(fixture),
            Payload = FakeJsonObject.CreateValid(fixture),
            ExternalReference = FakeExternalReference.CreateValid(fixture),
            CreateCase = FakeCreateCase.CreateValid(fixture)
        };
    }
}