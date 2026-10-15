using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Queues;

public record OrganizationListQueueRequestDto : ListQueueRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }
}
