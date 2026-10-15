using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.Commitments;

public record Commitment : DeletableModel
{
    public required CommitmentId CommitmentId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required CommitmentAmount Amount { get; init; }
    public required EffectiveFrom EffectiveFrom { get; init; }
    public required EffectiveTo EffectiveTo { get; init; }

    public required CommitmentAmount Drawn { get; init; }

    public required ApplicationUserId? CreatedByUserId { get; init; }
}
