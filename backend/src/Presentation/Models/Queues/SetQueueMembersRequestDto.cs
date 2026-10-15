using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Queues;

public record SetQueueMembersRequestDto : OrganizationQueueRequestDto
{
    [FromBody]
    public required SetQueueMembersRequestBodyDto Body { get; init; }
}

public record SetQueueMembersRequestBodyDto
{
    public required Guid[] UserIds { get; init; }
}
