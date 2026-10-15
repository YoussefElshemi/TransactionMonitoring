namespace Presentation.Models.Plans;

public record PlanResponseDto
{
    public required Guid PlanId { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required Guid? OrganizationId { get; init; }
    public required bool IsEnabled { get; init; }
    public required int CurrentVersionNumber { get; init; }
    public required PlanVersionResponseDto? CurrentVersion { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
