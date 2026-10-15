using AutoFixture;
using Core.Enums;
using Core.Models.Alerts;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Alerts;

public static class FakeAlertCandidate
{
    public static AlertCandidate CreateValid(IFixture fixture)
    {
        return new AlertCandidate
        {
            RuleId = FakeRuleId.CreateValid(fixture),
            RuleVersionId = FakeRuleVersionId.CreateValid(fixture),
            RuleEvaluationId = FakeRuleEvaluationId.CreateValid(fixture),
            Severity = RuleSeverity.Medium,
            PartyId = FakePartyId.CreateValid(fixture),
            PartyRole = PartyRole.Remitter,
            AlertPolicy = FakeAlertPolicy.CreateValid(fixture)
        };
    }
}
