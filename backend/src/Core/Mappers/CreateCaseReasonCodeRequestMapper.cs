using Core.Enums;
using Core.Models.CaseReasonCodes;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateCaseReasonCodeRequestMapper
{
    public static CaseReasonCode ToModel(this CreateCaseReasonCodeRequest createCaseReasonCodeRequest, TimeProvider timeProvider, Mode mode)
    {
        return new CaseReasonCode
        {
            Mode = mode,
            CaseReasonCodeId = new CaseReasonCodeId(Guid.NewGuid()),
            OrganizationId = createCaseReasonCodeRequest.OrganizationId,
            Code = createCaseReasonCodeRequest.Code,
            Description = createCaseReasonCodeRequest.Description,
            Disposition = createCaseReasonCodeRequest.Disposition,
            IsEnabled = createCaseReasonCodeRequest.IsEnabled,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
