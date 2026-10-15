using Core.Models.Cases;
using Core.ValueObjects;
using Presentation.Models.Cases;

namespace Presentation.Mappers.Cases;

public static class ReopenCaseRequestMapper
{
    public static ReopenCaseRequest ToModel(this ReopenCaseRequestDto reopenCaseRequestDto)
    {
        return new ReopenCaseRequest
        {
            CaseId = new CaseId(reopenCaseRequestDto.CaseId),
            Reason = new Message(reopenCaseRequestDto.Body.Reason),
            ReopenedByUserId = new ApplicationUserId(reopenCaseRequestDto.Body.UserId)
        };
    }
}
