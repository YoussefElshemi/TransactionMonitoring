using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Decisions;

public record ResolveDecisionRequestDto
{
    [FromRoute]
    public required Guid DecisionId { get; init; }

    [FromBody]
    public required ResolveDecisionRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

public record ResolveDecisionRequestBodyDto
{
    public required string Status { get; init; }
    public string? Disposition { get; init; }
    public Guid? ReasonCodeId { get; init; }
    public string? Note { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid UserId { get; set; }
}
