using Presentation.Models.Common;

namespace Presentation.Models.Cases;

public record ListCaseRequestDto : PagedRequestDto
{
    public string? FriendlyName { get; init; }
    public string? Status { get; init; }
    public string? Disposition { get; init; }
    public string? Severity { get; init; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? QueueId { get; init; }
    public bool? Unrouted { get; init; }
    public Guid? QueueMemberUserId { get; set; }
    public bool? Breached { get; init; }
    public DateTimeOffset? DueBefore { get; init; }
    public bool? Escalated { get; init; }
    public Guid? PartyId { get; init; }
    public string? Query { get; init; }
}