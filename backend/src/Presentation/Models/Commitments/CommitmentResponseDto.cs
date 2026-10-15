namespace Presentation.Models.Commitments;

public record CommitmentResponseDto
{
    public required Guid CommitmentId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required decimal Amount { get; init; }
    public required DateTimeOffset EffectiveFrom { get; init; }
    public required DateTimeOffset EffectiveTo { get; init; }

    public required decimal Drawn { get; init; }
    public required decimal Remaining { get; init; }

    public required Guid? CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
