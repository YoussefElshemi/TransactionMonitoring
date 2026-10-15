using Core.Models.CaseNotes;
using Core.ValueObjects;
using Presentation.Models.CaseNotes;

namespace Presentation.Mappers.CaseNotes;

public static class ListCaseNoteRequestMapper
{
    public static ListCaseNoteRequest ToModel(this ListCaseNoteRequestDto listCaseNoteRequestDto)
    {
        return new ListCaseNoteRequest
        {
            CaseId = new CaseId(listCaseNoteRequestDto.CaseId),
            Message = !string.IsNullOrWhiteSpace(listCaseNoteRequestDto.Message) ? new Message(listCaseNoteRequestDto.Message) : null,
            PageNumber = new PageNumber(listCaseNoteRequestDto.PageNumber),
            PageSize = new PageSize(listCaseNoteRequestDto.PageSize),
            FromDate = new DateRange(listCaseNoteRequestDto.FromDate),
            ToDate = new DateRange(listCaseNoteRequestDto.ToDate)
        };
    }
}