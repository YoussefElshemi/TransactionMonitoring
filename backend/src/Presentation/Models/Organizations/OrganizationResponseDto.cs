namespace Presentation.Models.Organizations;

public record OrganizationResponseDto
{
    public required Guid OrganizationId { get; init; }
    public required string FriendlyName { get; init; }
    public required string Status { get; init; }
    public required int? HoldWindow { get; init; }
    public required string? HoldExpiryVerdict { get; init; }

    public required bool? CaseGroupingEnabled { get; init; }
    public required bool? DeduplicationEnabled { get; init; }
    public required int? CaseGroupingPeriod { get; init; }
    public required int? DeduplicationWindow { get; init; }
    public required string? CaseGroupingScope { get; init; }
    public required string? CaseGroupingSeverityPolicy { get; init; }
    public required int? MaxAlertsPerCase { get; init; }
    public required string? DefaultLocale { get; init; }
    public required string? DefaultTimeZone { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}