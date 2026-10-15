namespace Presentation.Models.Queues;

public record QueueUsageResponseDto
{
    public required Guid QueueId { get; init; }
    public required string Name { get; init; }
    public required int CaseCount { get; init; }
    public required int OpenCaseCount { get; init; }
    public required int MemberCount { get; init; }
}
