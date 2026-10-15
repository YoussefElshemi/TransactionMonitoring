namespace Presentation.Models.RateLimitOverrides;

public record RateLimitOverrideResponseDto
{
    public required Guid RateLimitOverrideId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string? Pool { get; init; }
    public required int PermitLimit { get; init; }
    public required int TokensPerPeriod { get; init; }
    public required int ReplenishmentPeriodSeconds { get; init; }
    public required string Reason { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
