using Core.Models.CaseNotes;
using Core.ValueObjects;
using Presentation.Models.CaseNotes;

namespace Presentation.Mappers.CaseNotes;

public static class UpdateCaseNoteRequestDtoMapper
{
    public static UpdateCaseNoteRequest ToModel(this UpdateCaseNoteRequestDto updateCaseNoteRequestDto)
    {
        return new UpdateCaseNoteRequest
        {
            CaseNoteId = new CaseNoteId(updateCaseNoteRequestDto.CaseNoteId),
            Message = updateCaseNoteRequestDto.Body.Message != null ? new Message(updateCaseNoteRequestDto.Body.Message) : null
        };
    }
}