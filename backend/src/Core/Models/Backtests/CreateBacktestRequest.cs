using Core.ValueObjects;

namespace Core.Models.BackTests;

public record CreateBackTestRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RuleId RuleId { get; init; }
    public required VersionNumber? VersionNumber { get; init; }
    public required SampleSize SampleSize { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
}
