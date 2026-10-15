using AutoFixture;
using Core.Enums;
using Core.Extensions;
using Core.ValueObjects;
using Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeAuditLogEntity
{
    public static AuditLogEntity CreateValid(IFixture fixture)
    {
        return new AuditLogEntity
        {
            AuditLogId = fixture.Create<Guid>(),
            EntityTypeId = (int)AuditableEntityType.Rule,
            EntityId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            OperationId = (int)AuditOperation.Updated,
            Snapshot = FakeJsonObject.CreateValid(fixture).Serialize(),
            ChangedByApiClientId = null,
            ChangedByUserId = fixture.Create<Guid>(),
            CreatedAt = fixture.Create<DateTimeOffset>(),

            // Set by the insert trigger in a real database, but always present on a row read back.
            Sequence = 1,
            PreviousHash = null,
            Hash = new string('a', Hash.Length)
        };
    }
}
