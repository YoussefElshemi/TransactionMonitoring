namespace Presentation.Models.Simulations;

public record SimulationResponseDto
{
    public required Guid SimulationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Scenario { get; init; }
    public required string Status { get; init; }
    public required int TotalPayments { get; init; }
    public required int ProcessedCount { get; init; }
    public required int Seed { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset? CompletedAt { get; init; }
    public required string? FailureReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
