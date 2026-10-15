namespace Presentation.Models.VelocityProfiles;

public record VelocityMetricDto
{
    public required string Name { get; init; }
    public required string Path { get; init; }
}

public record VelocityThresholdDto
{
    public required string Name { get; init; }
    public required decimal Value { get; init; }
}

public record VelocityProfileResponseDto
{
    public required Guid VelocityProfileId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required List<VelocityMetricDto> Metrics { get; init; }
    public required List<VelocityThresholdDto> Thresholds { get; init; }
    public required bool IsDeleted { get; init; }
    public required DateTimeOffset? DeletedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}

public record VelocityProfileUsageResponseDto
{
    public required Guid RuleId { get; init; }
    public required string FriendlyName { get; init; }
    public required bool IsEnabled { get; init; }
    public required string[] Metrics { get; init; }
    public required string[] Thresholds { get; init; }
}

public record VelocityBackfillResponseDto
{
    public required Guid VelocityBackfillId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string[] Metrics { get; init; }
    public required string Status { get; init; }
    public required int TotalTransactions { get; init; }
    public required int ProcessedCount { get; init; }
    public required DateTimeOffset? CompletedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
