using AutoFixture;
using Core.Enums;
using Core.Models.Decisions;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Decisions;

public static class FakeDecision
{
    public static Decision CreateValid(IFixture fixture)
    {
        return new Decision
        {
            Mode = Mode.Live,
            DecisionId = FakeDecisionId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            TransactionId = FakeTransactionId.CreateValid(fixture),
            ExternalReference = FakeExternalReference.CreateValid(fixture),
            Status = DecisionStatus.Held,
            ReasonCode = DecisionReasonCode.RuleReviewRequired,
            CaseId = FakeCaseId.CreateValid(fixture),
            RuleEvaluations = [],
            ResolvedByUserId = FakeApplicationUserId.CreateValid(fixture),
            ResolvedByUser = FakeApplicationUser.CreateValid(fixture),
            HeldAt = FakeHeldAt.CreateValid(fixture),
            ExpiresAt = FakeExpiresAt.CreateValid(fixture),
            HoldExpiryVerdict = HoldExpiryVerdict.Block,
            ResolvedAt = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture)
        };
    }
}
