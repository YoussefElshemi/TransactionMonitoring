using Core.ValueObjects;

namespace Core.Models.Commitments;

public record CreateCommitmentRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required CommitmentAmount Amount { get; init; }
    public required EffectiveFrom? EffectiveFrom { get; init; }
    public required EffectiveTo EffectiveTo { get; init; }
    public required ApplicationUserId? CreatedByUserId { get; init; }
}
