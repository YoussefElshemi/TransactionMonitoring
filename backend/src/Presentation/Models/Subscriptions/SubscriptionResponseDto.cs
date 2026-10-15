using Presentation.Models.Plans;

namespace Presentation.Models.Subscriptions;

public record SubscriptionResponseDto
{
    public required Guid SubscriptionId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid PlanVersionId { get; init; }
    public required PlanVersionResponseDto? PlanVersion { get; init; }
    public required string? PlanName { get; init; }
    public required DateTimeOffset EffectiveFrom { get; init; }
    public required DateTimeOffset? EffectiveTo { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
