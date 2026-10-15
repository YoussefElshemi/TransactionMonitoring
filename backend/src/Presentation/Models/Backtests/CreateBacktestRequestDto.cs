using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.BackTests;

public record CreateBackTestRequestDto
{
    [FromRoute]
    public required Guid RuleId { get; init; }

    [FromBody]
    public CreateBackTestRequestBodyDto? Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid CreatedByUserId { get; set; }
}

public record CreateBackTestRequestBodyDto
{
    public int SampleSize { get; init; } = 100;
    public int? VersionNumber { get; init; }
}
