using Core.Enums;
using Core.Models.Cases;
using Core.ValueObjects;
using Presentation.Models.Cases;

namespace Presentation.Mappers.Cases;

public static class ChangeCaseStatusRequestMapper
{
    public static ChangeCaseStatusRequest ToModel(this ChangeCaseStatusRequestDto changeCaseStatusRequestDto)
    {
        return new ChangeCaseStatusRequest
        {
            CaseId = new CaseId(changeCaseStatusRequestDto.CaseId),
            Status = Enum.Parse<CaseStatus>(changeCaseStatusRequestDto.Body.Status, true),
            Note = !string.IsNullOrWhiteSpace(changeCaseStatusRequestDto.Body.Note) ? new Message(changeCaseStatusRequestDto.Body.Note) : null,
            ChangedByUserId = new ApplicationUserId(changeCaseStatusRequestDto.Body.UserId)
        };
    }
}
