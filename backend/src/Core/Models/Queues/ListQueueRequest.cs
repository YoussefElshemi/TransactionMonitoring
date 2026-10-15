using Core.Enums;
using Core.Models.Common;
using Core.Models.Permissions;
using Core.ValueObjects;

namespace Core.Models.Queues;

public record ListQueueRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required QueueName? Name { get; init; }
    public required QueueRoutingPolicy? RoutingPolicy { get; init; }
    public required IsEnabled? IsEnabled { get; init; }

    public required ApplicationUserId? MemberUserId { get; init; }

    public PermissionFilter? PermissionFilter { get; init; }
}
