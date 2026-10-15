using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeCaseEntity
{
    public static CaseEntity CreateValid(IFixture fixture)
    {
        return new CaseEntity
        {
            ModeId = (int)Mode.Live,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>(),
            CaseId = fixture.Create<Guid>(),
            FriendlyName = fixture.Create<string>(),
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            TransactionId = null,
            PartyId = null,
            StatusId = (int)fixture.Create<CaseStatus>(),
            StatusEntity = null!,
            SeverityId = (int)fixture.Create<RuleSeverity>(),
            SeverityEntity = null!,
            DispositionId = null,
            CaseReasonCodeId = null,
            QueueId = null,
            AssignedToUserId = fixture.Create<Guid>(),
            AssignedToUser = FakeApplicationUserEntity.CreateValid(fixture),
            AssignedAt = fixture.Create<DateTimeOffset>(),
            DueAt = fixture.Create<DateTimeOffset>(),
            EscalatedAt = null,
            EscalationRuleId = null,
            BreachNotifiedAt = null,
            ResolvedAt = fixture.Create<DateTimeOffset>(),
            ReopenedAt = null
        };
    }
}