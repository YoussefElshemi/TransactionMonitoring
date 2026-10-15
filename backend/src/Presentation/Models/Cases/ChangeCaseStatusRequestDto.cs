using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Cases;

public record ChangeCaseStatusRequestDto : CaseRequestDto
{
    [FromBody]
    public required ChangeCaseStatusRequestBodyDto Body { get; init; }
}

public record ChangeCaseStatusRequestBodyDto
{
    public required string Status { get; init; }
    public string? Note { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid UserId { get; set; }
}
