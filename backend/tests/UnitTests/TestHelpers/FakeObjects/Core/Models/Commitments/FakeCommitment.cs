using AutoFixture;
using Core.Models.Commitments;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Commitments;

public static class FakeCommitment
{
    public static Commitment CreateValid(IFixture fixture)
    {
        var effectiveFrom = FakeEffectiveFrom.CreateValid(fixture);

        return new Commitment
        {
            CommitmentId = FakeCommitmentId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Amount = new CommitmentAmount(12000),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = new EffectiveTo(((DateTimeOffset)effectiveFrom).AddYears(1)),
            Drawn = new CommitmentAmount(0),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
