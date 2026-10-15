using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.CaseNotes;

public record UpdateCaseNoteRequestDto : CaseNoteRequestDto
{
    [FromBody]
    public required UpdateCaseNoteRequestBodyDto Body { get; init; }
}

public record UpdateCaseNoteRequestBodyDto
{
    public string? Message { get; init; }
}