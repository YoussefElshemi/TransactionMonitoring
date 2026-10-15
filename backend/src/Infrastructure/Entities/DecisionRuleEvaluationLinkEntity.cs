using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record DecisionRuleEvaluationLinkEntity : BaseEntity
{
    public required Guid DecisionId { get; init; }
    public DecisionEntity Decision { get; init; } = null!;
    public required Guid RuleEvaluationId { get; init; }
    public RuleEvaluationEntity RuleEvaluation { get; init; } = null!;
}
