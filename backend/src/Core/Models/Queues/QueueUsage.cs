using Core.ValueObjects;

namespace Core.Models.Queues;

public record QueueUsage
{
    public required QueueId QueueId { get; init; }
    public required QueueName Name { get; init; }

    public required TotalCount CaseCount { get; init; }
    public required TotalCount OpenCaseCount { get; init; }
    public required TotalCount MemberCount { get; init; }
}
