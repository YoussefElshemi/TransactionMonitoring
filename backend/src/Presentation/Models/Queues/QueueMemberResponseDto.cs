namespace Presentation.Models.Queues;

public record QueueMemberResponseDto
{
    public required Guid UserId { get; init; }
    public required string? UserName { get; init; }
    public required DateTimeOffset? LastAssignedAt { get; init; }
}
