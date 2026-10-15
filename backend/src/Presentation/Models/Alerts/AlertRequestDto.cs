using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Alerts;

public record AlertRequestDto
{
    [FromRoute]
    public required Guid AlertId { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}
