using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.CreditNotes;

public record CreditNoteRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromRoute]
    public required Guid CreditNoteId { get; init; }
}
