using Core.Models.CaseNotes;
using Presentation.Models.CaseNotes;

namespace Presentation.Mappers.CaseNotes;

public static class CaseNoteDtoMapper
{
    public static CaseNoteResponseDto ToDto(this CaseNote caseNote)
    {
        return new CaseNoteResponseDto
        {
            Mode = caseNote.Mode.ToString(),
            CaseNoteId = caseNote.CaseNoteId,
            CaseId = caseNote.CaseId,
            Message = caseNote.Message,
            OrganizationId = caseNote.OrganizationId,
            CreatedByUserId = caseNote.CreatedByUserId,
            CreatedAt = caseNote.CreatedAt,
            UpdatedAt = caseNote.UpdatedAt
        };
    }
}
