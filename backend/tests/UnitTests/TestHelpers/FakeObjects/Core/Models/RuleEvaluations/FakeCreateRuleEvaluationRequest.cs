using AutoFixture;
using Core.Models.RuleEvaluations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;

public static class FakeCreateRuleEvaluationRequest
{
    public static CreateRuleEvaluationRequest CreateValid(IFixture fixture)
    {
        return new CreateRuleEvaluationRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            RuleId = FakeRuleId.CreateValid(fixture),
            RuleVersionId = FakeRuleVersionId.CreateValid(fixture),
            TransactionId = FakeTransactionId.CreateValid(fixture),
            Payload = FakeJsonObject.CreateValid(fixture),
            Matched = FakeMatched.CreateValid(fixture),
            ConditionMatches = [FakeConditionMatch.CreateValid(fixture)]
        };
    }
}
