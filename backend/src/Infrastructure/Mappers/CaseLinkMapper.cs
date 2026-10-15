using Core.Enums;
using Core.Models.CaseLinks;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class CaseLinkMapper
{
    public static CaseLink ToModel(this CaseLinkEntity caseLinkEntity, CaseId? viewpointCaseId = null)
    {
        var isReversed = viewpointCaseId.HasValue && caseLinkEntity.LinkedCaseId == viewpointCaseId.Value;

        var linkedCaseEntity = isReversed ? caseLinkEntity.Case : caseLinkEntity.LinkedCase;

        return new CaseLink
        {
            Mode = (Mode)caseLinkEntity.ModeId,
            CaseLinkId = new CaseLinkId(caseLinkEntity.CaseLinkId),
            OrganizationId = new OrganizationId(caseLinkEntity.OrganizationId),
            CaseId = new CaseId(isReversed ? caseLinkEntity.LinkedCaseId : caseLinkEntity.CaseId),
            LinkedCaseId = new CaseId(isReversed ? caseLinkEntity.CaseId : caseLinkEntity.LinkedCaseId),
            LinkedCase = linkedCaseEntity?.ToModel(),
            Reason = caseLinkEntity.Reason is null ? null : new LinkReason(caseLinkEntity.Reason),
            CreatedByUserId = new ApplicationUserId(caseLinkEntity.CreatedByUserId),
            CreatedAt = new CreatedAt(caseLinkEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(caseLinkEntity.UpdatedAt),
            IsDeleted = new IsDeleted(caseLinkEntity.IsDeleted),
            DeletedAt = caseLinkEntity.DeletedAt.HasValue ? new DeletedAt(caseLinkEntity.DeletedAt.Value) : null
        };
    }

    public static CaseLinkEntity ToEntity(this CaseLink caseLink)
    {
        return new CaseLinkEntity
        {
            ModeId = (int)caseLink.Mode,
            CaseLinkId = caseLink.CaseLinkId,
            OrganizationId = caseLink.OrganizationId,
            CaseId = caseLink.CaseId,
            LinkedCaseId = caseLink.LinkedCaseId,
            Reason = caseLink.Reason?.ToString(),
            CreatedByUserId = caseLink.CreatedByUserId,
            CreatedAt = caseLink.CreatedAt,
            UpdatedAt = caseLink.UpdatedAt,
            IsDeleted = caseLink.IsDeleted,
            DeletedAt = caseLink.DeletedAt
        };
    }
}
