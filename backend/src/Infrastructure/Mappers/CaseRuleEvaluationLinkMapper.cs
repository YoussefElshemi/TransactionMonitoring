using Core.Models.Cases;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class CaseRuleEvaluationLinkMapper
{
    public static CaseRuleEvaluationLink ToModel(this CaseRuleEvaluationLinkEntity caseRuleEvaluationLinkEntity)
    {
        return new CaseRuleEvaluationLink
        {
            CaseId = new CaseId(caseRuleEvaluationLinkEntity.CaseId),
            RuleEvaluationId = new RuleEvaluationId(caseRuleEvaluationLinkEntity.RuleEvaluationId),
            CreatedAt = new CreatedAt(caseRuleEvaluationLinkEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(caseRuleEvaluationLinkEntity.UpdatedAt)
        };
    }

    public static CaseRuleEvaluationLinkEntity ToEntity(this CaseRuleEvaluationLink caseRuleEvaluationLink)
    {
        return new CaseRuleEvaluationLinkEntity
        {
            CaseId = caseRuleEvaluationLink.CaseId,
            RuleEvaluationId = caseRuleEvaluationLink.RuleEvaluationId,
            CreatedAt = caseRuleEvaluationLink.CreatedAt,
            UpdatedAt = caseRuleEvaluationLink.UpdatedAt
        };
    }
}