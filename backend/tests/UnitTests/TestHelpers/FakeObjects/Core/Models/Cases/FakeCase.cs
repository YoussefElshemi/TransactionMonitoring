using AutoFixture;
using Core.Enums;
using Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;

public static class FakeCase
{
    public static Case CreateValid(IFixture fixture)
    {
        return new Case
        {
            Mode = Mode.Live,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
            CaseId = FakeCaseId.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            TransactionId = FakeTransactionId.CreateValid(fixture),
            PartyId = FakePartyId.CreateValid(fixture),
            Status = fixture.Create<CaseStatus>(),
            Severity = fixture.Create<RuleSeverity>(),
            RuleEvaluations = [],
            Alerts = [],
            Disposition = null,
            CaseReasonCodeId = null,
            ReasonCode = null,
            QueueId = null,
            Queue = null,
            AssignedToUserId = FakeApplicationUserId.CreateValid(fixture),
            AssignedToUser = FakeApplicationUser.CreateValid(fixture),
            AssignedAt = FakeAssignedAt.CreateValid(fixture),
            DueAt = FakeDueAt.CreateValid(fixture),
            EscalatedAt = null,
            EscalationRuleId = null,
            BreachNotifiedAt = null,
            ResolvedAt = FakeResolvedAt.CreateValid(fixture),
            ReopenedAt = null
        };
    }
}