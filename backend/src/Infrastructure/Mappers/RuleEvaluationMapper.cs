using Core.Enums;
using Core.Extensions;
using Core.Models.RuleEvaluations;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class RuleEvaluationMapper
{
    public static RuleEvaluation ToModel(this RuleEvaluationEntity ruleEvaluationEntity)
    {
        return new RuleEvaluation
        {
            Mode = (Mode)ruleEvaluationEntity.ModeId,
            RuleEvaluationId = new RuleEvaluationId(ruleEvaluationEntity.RuleEvaluationId),
            OrganizationId = new OrganizationId(ruleEvaluationEntity.OrganizationId),
            RuleId = new RuleId(ruleEvaluationEntity.RuleId),
            Rule = ruleEvaluationEntity.Rule.ToModel(),
            RuleVersionId = new RuleVersionId(ruleEvaluationEntity.RuleVersionId),
            RuleVersion = ruleEvaluationEntity.RuleVersion.ToModel(),
            TransactionId = new TransactionId(ruleEvaluationEntity.TransactionId),
            Matched = new Matched(ruleEvaluationEntity.Matched),
            ConditionMatches = ruleEvaluationEntity.ConditionMatches?.FromBase64String().TryDeserialize<List<ConditionMatch>>(),
            CreatedAt = new CreatedAt(ruleEvaluationEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(ruleEvaluationEntity.UpdatedAt),
            IsDeleted = new IsDeleted(ruleEvaluationEntity.IsDeleted),
            DeletedAt = ruleEvaluationEntity.DeletedAt.HasValue ? new DeletedAt(ruleEvaluationEntity.DeletedAt.Value) : null
        };
    }

    public static RuleEvaluationEntity ToEntity(this RuleEvaluation ruleEvaluation)
    {
        return new RuleEvaluationEntity
        {
            ModeId = (int)ruleEvaluation.Mode,
            RuleEvaluationId = ruleEvaluation.RuleEvaluationId,
            OrganizationId = ruleEvaluation.OrganizationId,
            RuleId = ruleEvaluation.RuleId,
            RuleVersionId = ruleEvaluation.RuleVersionId,
            TransactionId = ruleEvaluation.TransactionId,
            Matched = ruleEvaluation.Matched,
            ConditionMatches = ruleEvaluation.ConditionMatches?.Serialize().ToBase64String(),
            CreatedAt = ruleEvaluation.CreatedAt,
            UpdatedAt = ruleEvaluation.UpdatedAt,
            IsDeleted = ruleEvaluation.IsDeleted,
            DeletedAt = ruleEvaluation.DeletedAt
        };
    }
}
