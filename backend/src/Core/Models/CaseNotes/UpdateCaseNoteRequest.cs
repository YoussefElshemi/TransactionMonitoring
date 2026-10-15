using Core.ValueObjects;

namespace Core.Models.CaseNotes;

public record UpdateCaseNoteRequest
{
    public required CaseNoteId CaseNoteId { get; init; }
    public required Message? Message { get; init; }
}