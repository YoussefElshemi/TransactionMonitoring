using Presentation.Models.Rules;

namespace Presentation.Models.Queues;

public record QueueResponseDto
{
    public required Guid QueueId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required int Priority { get; init; }
    public required string RoutingPolicy { get; init; }
    public required string? MinimumSeverity { get; init; }
    public required Guid[] RuleIds { get; init; }
    public required RuleBodyDto? Matcher { get; init; }
    public required bool IsEnabled { get; init; }
    public required QueueMemberResponseDto[] Members { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
