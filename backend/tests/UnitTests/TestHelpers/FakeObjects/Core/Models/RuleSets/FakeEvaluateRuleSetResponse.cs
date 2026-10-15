using AutoFixture;
using Core.Models.RuleSets;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.RuleSets;

public static class FakeEvaluateRuleSetResponse
{
    public static EvaluateRuleSetResponse CreateValid(IFixture fixture)
    {
        return new EvaluateRuleSetResponse
        {
            RuleSet = FakeRuleSet.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            Results = [FakeEvaluateRuleResponse.CreateValid(fixture)],
            Outcome = FakeRuleOutcome.CreateValid(fixture),
            Case = FakeCase.CreateValid(fixture),
            RuleSetId = FakeRuleSetId.CreateValid(fixture),
            TransactionId = FakeTransactionId.CreateValid(fixture),
            ExternalReference = FakeExternalReference.CreateValid(fixture),
            CaseId = FakeCaseId.CreateValid(fixture),
            Decision = FakeDecision.CreateValid(fixture)
        };
    }
}