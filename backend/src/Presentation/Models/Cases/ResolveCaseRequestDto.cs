using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Cases;

public record ResolveCaseRequestDto : CaseRequestDto
{
    [FromBody]
    public required ResolveCaseRequestBodyDto Body { get; init; }
}

public record ResolveCaseRequestBodyDto
{
    public required string Status { get; init; }
    public required string Disposition { get; init; }
    public Guid? ReasonCodeId { get; init; }
    public string? Note { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid UserId { get; set; }
}