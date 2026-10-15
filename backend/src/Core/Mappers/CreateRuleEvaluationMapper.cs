using Core.Enums;
using Core.Models.RuleEvaluations;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateRuleEvaluationRequestMapper
{
    public static RuleEvaluation ToModel(this CreateRuleEvaluationRequest createRuleEvaluationRequest, TimeProvider timeProvider, Mode mode)
    {
        return new RuleEvaluation
        {
            Mode = mode,
            RuleEvaluationId = new RuleEvaluationId(Guid.NewGuid()),
            OrganizationId = createRuleEvaluationRequest.OrganizationId,
            RuleId = createRuleEvaluationRequest.RuleId,
            Rule = null,
            RuleVersionId = createRuleEvaluationRequest.RuleVersionId,
            RuleVersion = null,
            TransactionId = createRuleEvaluationRequest.TransactionId,
            Matched = createRuleEvaluationRequest.Matched,
            ConditionMatches = createRuleEvaluationRequest.ConditionMatches,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}