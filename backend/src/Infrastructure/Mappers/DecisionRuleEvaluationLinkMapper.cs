using Core.Models.Decisions;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class DecisionRuleEvaluationLinkMapper
{
    public static DecisionRuleEvaluationLink ToModel(this DecisionRuleEvaluationLinkEntity decisionRuleEvaluationLinkEntity)
    {
        return new DecisionRuleEvaluationLink
        {
            DecisionId = new DecisionId(decisionRuleEvaluationLinkEntity.DecisionId),
            RuleEvaluationId = new RuleEvaluationId(decisionRuleEvaluationLinkEntity.RuleEvaluationId),
            CreatedAt = new CreatedAt(decisionRuleEvaluationLinkEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(decisionRuleEvaluationLinkEntity.UpdatedAt)
        };
    }

    public static DecisionRuleEvaluationLinkEntity ToEntity(this DecisionRuleEvaluationLink decisionRuleEvaluationLink)
    {
        return new DecisionRuleEvaluationLinkEntity
        {
            DecisionId = decisionRuleEvaluationLink.DecisionId,
            RuleEvaluationId = decisionRuleEvaluationLink.RuleEvaluationId,
            CreatedAt = decisionRuleEvaluationLink.CreatedAt,
            UpdatedAt = decisionRuleEvaluationLink.UpdatedAt
        };
    }
}
