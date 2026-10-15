using Core.Models.Cases;
using Presentation.Models.Cases;

namespace Presentation.Mappers.Cases;

public static class CaseAgeingDtoMapper
{
    public static CaseAgeingResponseDto ToDto(this CaseAgeing caseAgeing)
    {
        return new CaseAgeingResponseDto
        {
            AgeInMilliseconds = caseAgeing.Age.TotalMilliseconds,
            TimeToDueInMilliseconds = caseAgeing.TimeToDue.TotalMilliseconds,
            Bucket = caseAgeing.Bucket.ToString(),
            IsBreached = caseAgeing.IsBreached
        };
    }
}
