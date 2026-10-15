using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.CaseNotes;

public record CreateCaseNoteRequestDto
{
    [FromRoute]
    public required Guid CaseId { get; init; }

    [FromBody]
    public required CreateCaseNoteRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid OrganizationId { get; set; }
}

public record CreateCaseNoteRequestBodyDto
{
    public required string Message { get; init; }
}