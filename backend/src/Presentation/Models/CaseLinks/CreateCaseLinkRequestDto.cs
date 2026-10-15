using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.CaseLinks;

public record CreateCaseLinkRequestDto
{
    [FromRoute]
    public required Guid CaseId { get; init; }

    [FromBody]
    public required CreateCaseLinkRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

public record CreateCaseLinkRequestBodyDto
{
    public required Guid LinkedCaseId { get; init; }
    public string? Reason { get; init; }
}
