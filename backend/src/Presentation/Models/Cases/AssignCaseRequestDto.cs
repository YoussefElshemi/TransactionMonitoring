using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Cases;

public record AssignCaseRequestDto : CaseRequestDto
{
    [FromBody]
    public required AssignCaseRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid AssignedByUserId { get; set; }
}

public record AssignCaseRequestBodyDto
{
    public Guid? UserId { get; set; }
}
