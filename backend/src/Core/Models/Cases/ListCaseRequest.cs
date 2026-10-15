using Core.Enums;
using Core.Models.Common;
using Core.Models.Permissions;
using Core.ValueObjects;

namespace Core.Models.Cases;

public record ListCaseRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName? FriendlyName { get; init; }
    public required CaseStatus? Status { get; init; }
    public required CaseDisposition? Disposition { get; init; }
    public required RuleSeverity? Severity { get; init; }
    public required ApplicationUserId? AssignedToUserId { get; init; }
    public required QueueId? QueueId { get; init; }

    public required Unrouted? Unrouted { get; init; }

    public required ApplicationUserId? QueueMemberUserId { get; init; }

    public required IsBreached? Breached { get; init; }

    public required DueAt? DueBefore { get; init; }

    public required IsEscalated? Escalated { get; init; }

    public required PartyId? PartyId { get; init; }

    public required SearchQuery? Query { get; init; }

    public PermissionFilter? PermissionFilter { get; init; }
}