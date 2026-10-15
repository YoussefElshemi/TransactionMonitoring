using AutoFixture;
using Core.Models.RuleSets;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;

public static class FakeEvaluateRuleSetRequest
{
    public static EvaluateRuleSetRequest CreateValid(IFixture fixture)
    {
        return new EvaluateRuleSetRequest
        {
            RuleSetId = FakeRuleSetId.CreateValid(fixture),
            Payload = FakeJsonObject.CreateValid(fixture),
            ExternalReference = FakeExternalReference.CreateValid(fixture),
            CreateCase = FakeCreateCase.CreateValid(fixture)
        };
    }
}