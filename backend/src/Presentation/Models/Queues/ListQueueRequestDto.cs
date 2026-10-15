using Presentation.Models.Common;

namespace Presentation.Models.Queues;

public record ListQueueRequestDto : PagedRequestDto
{
    public string? Name { get; init; }
    public string? RoutingPolicy { get; init; }
    public bool? IsEnabled { get; init; }
    public Guid? MemberUserId { get; set; }
}
