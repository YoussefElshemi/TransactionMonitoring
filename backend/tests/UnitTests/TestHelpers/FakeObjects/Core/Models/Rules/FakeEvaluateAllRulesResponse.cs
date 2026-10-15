using AutoFixture;
using Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;

public static class FakeEvaluateAllRulesResponse
{
    public static EvaluateAllRulesResponse CreateValid(IFixture fixture)
    {
        return new EvaluateAllRulesResponse
        {
            Results = [FakeEvaluateRuleResponse.CreateValid(fixture)],
            Outcome = FakeRuleOutcome.CreateValid(fixture),
            TransactionId = FakeTransactionId.CreateValid(fixture),
            ExternalReference = FakeExternalReference.CreateValid(fixture),
            CaseId = FakeCaseId.CreateValid(fixture),
            Case = FakeCase.CreateValid(fixture),
            Decision = FakeDecision.CreateValid(fixture)
        };
    }
}