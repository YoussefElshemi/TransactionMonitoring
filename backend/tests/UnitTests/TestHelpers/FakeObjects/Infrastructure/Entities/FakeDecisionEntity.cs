using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeDecisionEntity
{
    public static DecisionEntity CreateValid(IFixture fixture)
    {
        var transactionEntity = FakeTransactionEntity.CreateValid(fixture);

        return new DecisionEntity
        {
            ModeId = (int)Mode.Live,
            DecisionId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            TransactionId = transactionEntity.TransactionId,
            Transaction = transactionEntity,
            StatusId = (int)DecisionStatus.Held,
            StatusEntity = null!,
            ReasonCodeId = (int)DecisionReasonCode.RuleReviewRequired,
            ReasonCodeEntity = null!,
            CaseId = null,
            ResolvedByUserId = null,
            HeldAt = fixture.Create<DateTimeOffset>(),
            ExpiresAt = null,
            HoldExpiryVerdictId = null,
            ResolvedAt = null,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
