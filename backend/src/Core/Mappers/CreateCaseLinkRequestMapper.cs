using Core.Enums;
using Core.Models.CaseLinks;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateCaseLinkRequestMapper
{
    public static CaseLink ToModel(this CreateCaseLinkRequest createCaseLinkRequest, TimeProvider timeProvider, Mode mode)
    {
        return new CaseLink
        {
            Mode = mode,
            CaseLinkId = new CaseLinkId(Guid.NewGuid()),
            OrganizationId = createCaseLinkRequest.OrganizationId,
            CaseId = createCaseLinkRequest.CaseId,
            LinkedCaseId = createCaseLinkRequest.LinkedCaseId,
            LinkedCase = null,
            Reason = createCaseLinkRequest.Reason,
            CreatedByUserId = createCaseLinkRequest.CreatedByUserId,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
