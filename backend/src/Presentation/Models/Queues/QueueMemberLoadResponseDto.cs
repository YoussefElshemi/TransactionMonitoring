namespace Presentation.Models.Queues;

public record QueueMemberLoadResponseDto
{
    public required Guid UserId { get; init; }
    public required string? UserName { get; init; }
    public required int OpenCaseCount { get; init; }
    public required DateTimeOffset? LastAssignedAt { get; init; }
}
