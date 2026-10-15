using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.Cases;

public record ReopenCaseRequestDto : CaseRequestDto
{
    [FromBody]
    public required ReopenCaseRequestBodyDto Body { get; init; }
}

public record ReopenCaseRequestBodyDto
{
    public required string Reason { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid UserId { get; set; }
}
