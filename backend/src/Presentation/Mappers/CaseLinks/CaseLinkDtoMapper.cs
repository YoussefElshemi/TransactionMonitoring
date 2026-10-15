using Core.Models.CaseLinks;
using Presentation.Models.CaseLinks;

namespace Presentation.Mappers.CaseLinks;

public static class CaseLinkDtoMapper
{
    public static CaseLinkResponseDto ToDto(this CaseLink caseLink)
    {
        return new CaseLinkResponseDto
        {
            Mode = caseLink.Mode.ToString(),
            CaseLinkId = caseLink.CaseLinkId,
            OrganizationId = caseLink.OrganizationId,
            CaseId = caseLink.CaseId,
            LinkedCaseId = caseLink.LinkedCaseId,
            Reason = caseLink.Reason?.ToString(),
            CreatedByUserId = caseLink.CreatedByUserId,
            CreatedAt = caseLink.CreatedAt,
            UpdatedAt = caseLink.UpdatedAt
        };
    }
}
