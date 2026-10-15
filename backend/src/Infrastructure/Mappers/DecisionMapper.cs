using Core.Enums;
using Core.Models.Decisions;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class DecisionMapper
{
    public static Decision ToModel(this DecisionEntity decisionEntity, ExternalReference? externalReference)
    {
        return new Decision
        {
            Mode = (Mode)decisionEntity.ModeId,
            DecisionId = new DecisionId(decisionEntity.DecisionId),
            OrganizationId = new OrganizationId(decisionEntity.OrganizationId),
            TransactionId = new TransactionId(decisionEntity.TransactionId),
            ExternalReference = externalReference,
            Status = (DecisionStatus)decisionEntity.StatusId,
            ReasonCode = (DecisionReasonCode)decisionEntity.ReasonCodeId,
            CaseId = decisionEntity.CaseId.HasValue ? new CaseId(decisionEntity.CaseId.Value) : null,
            RuleEvaluations = decisionEntity.RuleEvaluations.Select(x => x.ToModel()).ToList(),
            ResolvedByUserId = decisionEntity.ResolvedByUserId.HasValue ? new ApplicationUserId(decisionEntity.ResolvedByUserId.Value) : null,
            ResolvedByUser = decisionEntity.ResolvedByUser?.ToModel(),
            HeldAt = decisionEntity.HeldAt.HasValue ? new HeldAt(decisionEntity.HeldAt.Value) : null,
            ExpiresAt = decisionEntity.ExpiresAt.HasValue ? new ExpiresAt(decisionEntity.ExpiresAt.Value) : null,
            HoldExpiryVerdict = (HoldExpiryVerdict?)decisionEntity.HoldExpiryVerdictId,
            ResolvedAt = decisionEntity.ResolvedAt.HasValue ? new ResolvedAt(decisionEntity.ResolvedAt.Value) : null,
            CreatedAt = new CreatedAt(decisionEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(decisionEntity.UpdatedAt)
        };
    }

    public static DecisionEntity ToEntity(this Decision decision)
    {
        return new DecisionEntity
        {
            ModeId = (int)decision.Mode,
            DecisionId = decision.DecisionId,
            OrganizationId = decision.OrganizationId,
            TransactionId = decision.TransactionId,
            StatusId = (int)decision.Status,
            ReasonCodeId = (int)decision.ReasonCode,
            CaseId = decision.CaseId,
            ResolvedByUserId = decision.ResolvedByUserId,
            HeldAt = decision.HeldAt,
            ExpiresAt = decision.ExpiresAt,
            HoldExpiryVerdictId = (int?)decision.HoldExpiryVerdict,
            ResolvedAt = decision.ResolvedAt,
            CreatedAt = decision.CreatedAt,
            UpdatedAt = decision.UpdatedAt
        };
    }
}
