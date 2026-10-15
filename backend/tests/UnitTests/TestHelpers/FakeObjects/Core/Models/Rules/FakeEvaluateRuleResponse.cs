using AutoFixture;
using Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

public static class FakeEvaluateRuleResponse
{
    public static EvaluateRuleResponse CreateValid(IFixture fixture)
    {
        return new EvaluateRuleResponse
        {
            CaseId = FakeCaseId.CreateValid(fixture),
            Case = FakeCase.CreateValid(fixture),
            RuleId = FakeRuleId.CreateValid(fixture),
            Rule = FakeRule.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            Matched = FakeMatched.CreateValid(fixture),
            Policy = FakeRulePolicy.CreateValid(fixture),
            Outcome = FakeRuleOutcome.CreateValid(fixture),
            TransactionId = FakeTransactionId.CreateValid(fixture),
            ExternalReference = FakeExternalReference.CreateValid(fixture),
            Decision = FakeDecision.CreateValid(fixture)
        };
    }
}