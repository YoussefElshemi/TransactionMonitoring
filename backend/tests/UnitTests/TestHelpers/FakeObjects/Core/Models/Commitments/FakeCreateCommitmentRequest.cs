using AutoFixture;
using Core.Models.Commitments;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Commitments;

public static class FakeCreateCommitmentRequest
{
    public static CreateCommitmentRequest CreateValid(IFixture fixture)
    {
        var effectiveFrom = FakeEffectiveFrom.CreateValid(fixture);

        return new CreateCommitmentRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Amount = new CommitmentAmount(12000),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = new EffectiveTo(((DateTimeOffset)effectiveFrom).AddYears(1)),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture)
        };
    }
}
