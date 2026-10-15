using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Subscriptions;

public record CreateSubscriptionRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required CreateSubscriptionRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreateSubscriptionRequestBodyDto
{
    public required Guid PlanVersionId { get; init; }

    public DateTimeOffset? EffectiveFrom { get; init; }
}
