using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Organizations;

namespace Presentation.Models.Queues;

public record OrganizationQueueRequestDto : OrganizationRequestDto
{
    [FromRoute]
    public required Guid QueueId { get; init; }
}
