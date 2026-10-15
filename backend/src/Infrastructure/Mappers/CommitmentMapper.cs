using Core.Models.Commitments;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class CommitmentMapper
{
    public static Commitment ToModel(this CommitmentEntity commitmentEntity, decimal drawn)
    {
        return new Commitment
        {
            CommitmentId = new CommitmentId(commitmentEntity.CommitmentId),
            OrganizationId = new OrganizationId(commitmentEntity.OrganizationId),
            Amount = new CommitmentAmount(commitmentEntity.Amount),
            EffectiveFrom = new EffectiveFrom(commitmentEntity.EffectiveFrom),
            EffectiveTo = new EffectiveTo(commitmentEntity.EffectiveTo),
            Drawn = new CommitmentAmount(drawn),
            CreatedByUserId = commitmentEntity.CreatedByUserId.HasValue ? new ApplicationUserId(commitmentEntity.CreatedByUserId.Value) : null,
            CreatedAt = new CreatedAt(commitmentEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(commitmentEntity.UpdatedAt),
            IsDeleted = new IsDeleted(commitmentEntity.IsDeleted),
            DeletedAt = commitmentEntity.DeletedAt.HasValue ? new DeletedAt(commitmentEntity.DeletedAt.Value) : null
        };
    }

    public static CommitmentEntity ToEntity(this Commitment commitment)
    {
        return new CommitmentEntity
        {
            CommitmentId = commitment.CommitmentId,
            OrganizationId = commitment.OrganizationId,
            Amount = commitment.Amount,
            EffectiveFrom = commitment.EffectiveFrom,
            EffectiveTo = commitment.EffectiveTo,
            CreatedByUserId = commitment.CreatedByUserId.HasValue ? commitment.CreatedByUserId.Value : null,
            CreatedAt = commitment.CreatedAt,
            UpdatedAt = commitment.UpdatedAt,
            IsDeleted = commitment.IsDeleted,
            DeletedAt = commitment.DeletedAt
        };
    }
}
