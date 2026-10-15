namespace Presentation.Models.BackTests;

public record BackTestResponseDto
{
    public required Guid BackTestId { get; init; }
    public required Guid RuleId { get; init; }
    public required Guid RuleVersionId { get; init; }
    public required string Status { get; init; }
    public required int SampleSize { get; init; }
    public required int TotalEvaluations { get; init; }
    public required int ProcessedCount { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required DateTimeOffset? CompletedAt { get; init; }

    public required string Mode { get; init; }
}
