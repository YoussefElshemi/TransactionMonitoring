using Core.Enums;
using Core.Models.CaseReasonCodes;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class CaseReasonCodeMapper
{
    public static CaseReasonCode ToModel(this CaseReasonCodeEntity caseReasonCodeEntity)
    {
        return new CaseReasonCode
        {
            Mode = (Mode)caseReasonCodeEntity.ModeId,
            CaseReasonCodeId = new CaseReasonCodeId(caseReasonCodeEntity.CaseReasonCodeId),
            OrganizationId = new OrganizationId(caseReasonCodeEntity.OrganizationId),
            Code = new CaseReasonCodeName(caseReasonCodeEntity.Code),
            Description = new CaseReasonCodeDescription(caseReasonCodeEntity.Description),
            Disposition = caseReasonCodeEntity.DispositionId.HasValue ? (CaseDisposition)caseReasonCodeEntity.DispositionId.Value : null,
            IsEnabled = new IsEnabled(caseReasonCodeEntity.IsEnabled),
            CreatedAt = new CreatedAt(caseReasonCodeEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(caseReasonCodeEntity.UpdatedAt),
            IsDeleted = new IsDeleted(caseReasonCodeEntity.IsDeleted),
            DeletedAt = caseReasonCodeEntity.DeletedAt.HasValue ? new DeletedAt(caseReasonCodeEntity.DeletedAt.Value) : null
        };
    }

    public static CaseReasonCodeEntity ToEntity(this CaseReasonCode caseReasonCode)
    {
        return new CaseReasonCodeEntity
        {
            ModeId = (int)caseReasonCode.Mode,
            CaseReasonCodeId = caseReasonCode.CaseReasonCodeId,
            OrganizationId = caseReasonCode.OrganizationId,
            Code = caseReasonCode.Code,
            Description = caseReasonCode.Description,
            DispositionId = caseReasonCode.Disposition.HasValue ? (int)caseReasonCode.Disposition.Value : null,
            IsEnabled = caseReasonCode.IsEnabled,
            CreatedAt = caseReasonCode.CreatedAt,
            UpdatedAt = caseReasonCode.UpdatedAt,
            IsDeleted = caseReasonCode.IsDeleted,
            DeletedAt = caseReasonCode.DeletedAt
        };
    }
}
