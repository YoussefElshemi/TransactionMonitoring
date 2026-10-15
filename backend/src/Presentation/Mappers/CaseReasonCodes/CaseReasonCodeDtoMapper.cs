using Core.Models.CaseReasonCodes;
using Presentation.Models.CaseReasonCodes;

namespace Presentation.Mappers.CaseReasonCodes;

public static class CaseReasonCodeDtoMapper
{
    public static CaseReasonCodeResponseDto ToDto(this CaseReasonCode caseReasonCode)
    {
        return new CaseReasonCodeResponseDto
        {
            Mode = caseReasonCode.Mode.ToString(),
            CaseReasonCodeId = caseReasonCode.CaseReasonCodeId,
            OrganizationId = caseReasonCode.OrganizationId,
            Code = caseReasonCode.Code,
            Description = caseReasonCode.Description,
            Disposition = caseReasonCode.Disposition?.ToString(),
            IsEnabled = caseReasonCode.IsEnabled,
            CreatedAt = caseReasonCode.CreatedAt,
            UpdatedAt = caseReasonCode.UpdatedAt
        };
    }

    public static CaseReasonCodeUsageResponseDto ToDto(this CaseReasonCodeUsage caseReasonCodeUsage)
    {
        return new CaseReasonCodeUsageResponseDto
        {
            CaseReasonCodeId = caseReasonCodeUsage.CaseReasonCodeId,
            Code = caseReasonCodeUsage.Code,
            CaseCount = caseReasonCodeUsage.CaseCount
        };
    }
}
