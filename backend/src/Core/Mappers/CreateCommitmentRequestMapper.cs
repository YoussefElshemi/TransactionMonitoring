using Core.Models.Commitments;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateCommitmentRequestMapper
{
    public static Commitment ToModel(this CreateCommitmentRequest request, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();

        return new Commitment
        {
            CommitmentId = new CommitmentId(Guid.NewGuid()),
            OrganizationId = request.OrganizationId,
            Amount = request.Amount,
            EffectiveFrom = request.EffectiveFrom ?? new EffectiveFrom(now),
            EffectiveTo = request.EffectiveTo,

            Drawn = new CommitmentAmount(0),
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
