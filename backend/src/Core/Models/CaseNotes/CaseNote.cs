using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.CaseNotes;

public record CaseNote : DeletableModel
{
    public required CaseNoteId CaseNoteId { get; init; }
    public required CaseId CaseId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required Message Message { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }

    public required Mode Mode { get; init; }
}