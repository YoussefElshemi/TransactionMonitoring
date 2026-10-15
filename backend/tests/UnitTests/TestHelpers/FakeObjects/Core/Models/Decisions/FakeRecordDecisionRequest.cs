using AutoFixture;
using Core.Enums;
using Core.Models.Decisions;
using Core.Models.Rules;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;

public static class FakeRecordDecisionRequest
{
    public static RecordDecisionRequest CreateValid(IFixture fixture)
    {
        return new RecordDecisionRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            TransactionId = FakeTransactionId.CreateValid(fixture),
            Outcome = new RuleOutcome
            {
                Action = RuleAction.Review,
                Severity = RuleSeverity.High,
                RiskScore = new RiskScore(50)
            },
            AnyMatched = new Matched(true),
            HoldPolicy = new HoldPolicy
            {
                HoldWindow = FakeHoldWindow.CreateValid(fixture),
                HoldExpiryVerdict = HoldExpiryVerdict.Block
            },
            CaseId = FakeCaseId.CreateValid(fixture),
            RuleEvaluationIds = [FakeRuleEvaluationId.CreateValid(fixture)]
        };
    }
}
