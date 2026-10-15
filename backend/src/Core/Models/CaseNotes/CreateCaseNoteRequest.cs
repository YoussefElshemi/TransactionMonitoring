using Core.ValueObjects;

namespace Core.Models.CaseNotes;

public record CreateCaseNoteRequest
{
    public required CaseId CaseId { get; init; }
    public required Message Message { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
}