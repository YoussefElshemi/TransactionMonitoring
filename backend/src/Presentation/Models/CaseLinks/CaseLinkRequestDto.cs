using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.CaseLinks;

public record CaseLinkRequestDto
{
    [FromRoute]
    public required Guid CaseId { get; init; }

    [FromRoute]
    public required Guid CaseLinkId { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}
