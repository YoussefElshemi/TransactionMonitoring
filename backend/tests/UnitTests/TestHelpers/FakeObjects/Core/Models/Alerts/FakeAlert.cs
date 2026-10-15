using AutoFixture;
using Core.Enums;
using Core.Models.Alerts;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Alerts;

public static class FakeAlert
{
    public static Alert CreateValid(IFixture fixture)
    {
        return new Alert
        {
            Mode = Mode.Live,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
            AlertId = FakeAlertId.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Status = AlertStatus.Open,
            Severity = RuleSeverity.Medium,
            RuleId = FakeRuleId.CreateValid(fixture),
            RuleVersionId = FakeRuleVersionId.CreateValid(fixture),
            RuleEvaluationId = FakeRuleEvaluationId.CreateValid(fixture),
            RuleEvaluation = null,
            TransactionId = FakeTransactionId.CreateValid(fixture),
            PartyId = FakePartyId.CreateValid(fixture),
            PartyRole = PartyRole.Remitter,
            CaseId = null,
            SuppressionReason = null,
            SuppressedByAlertId = null,
            RaisedAt = FakeRaisedAt.CreateValid(fixture),
            ClosedAt = null
        };
    }
}
