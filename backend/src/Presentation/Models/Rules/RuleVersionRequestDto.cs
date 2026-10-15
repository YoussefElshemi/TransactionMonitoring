using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Rules;

public record RuleVersionRequestDto
{
    [FromRoute]
    public required Guid RuleId { get; init; }

    [FromRoute]
    public required int VersionNumber { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}
