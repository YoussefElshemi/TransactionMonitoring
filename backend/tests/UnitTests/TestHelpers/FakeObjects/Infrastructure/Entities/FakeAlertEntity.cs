using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeAlertEntity
{
    public static AlertEntity CreateValid(IFixture fixture)
    {
        return new AlertEntity
        {
            ModeId = (int)Mode.Live,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>(),
            AlertId = fixture.Create<Guid>(),
            FriendlyName = fixture.Create<string>(),
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            StatusId = (int)AlertStatus.Open,
            StatusEntity = null!,
            SeverityId = (int)fixture.Create<RuleSeverity>(),
            SeverityEntity = null!,
            RuleId = fixture.Create<Guid>(),
            RuleVersionId = fixture.Create<Guid>(),
            RuleEvaluationId = fixture.Create<Guid>(),
            TransactionId = fixture.Create<Guid>(),
            PartyId = fixture.Create<Guid>(),
            PartyRoleId = (int)PartyRole.Remitter,
            CaseId = null,
            SuppressionReasonId = null,
            SuppressedByAlertId = null,
            RaisedAt = fixture.Create<DateTimeOffset>(),
            ClosedAt = null
        };
    }
}
