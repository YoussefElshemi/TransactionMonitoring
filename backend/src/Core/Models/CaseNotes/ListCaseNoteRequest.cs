using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.CaseNotes;

public record ListCaseNoteRequest : PagedRequest
{
    public required CaseId CaseId { get; init; }
    public required Message? Message { get; init; }
}