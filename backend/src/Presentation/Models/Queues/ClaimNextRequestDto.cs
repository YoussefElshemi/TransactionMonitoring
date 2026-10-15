using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Queues;

public record ClaimNextRequestDto
{
    [FromRoute]
    public Guid? QueueId { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid UserId { get; set; }
}
