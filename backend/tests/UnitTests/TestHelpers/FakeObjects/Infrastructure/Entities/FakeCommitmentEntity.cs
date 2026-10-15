using AutoFixture;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeCommitmentEntity
{
    public static CommitmentEntity CreateValid(IFixture fixture)
    {
        var effectiveFrom = fixture.Create<DateTimeOffset>();

        return new CommitmentEntity
        {
            CommitmentId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Amount = 12000m,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveFrom.AddYears(1),
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
