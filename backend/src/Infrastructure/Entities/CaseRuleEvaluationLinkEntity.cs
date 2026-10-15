using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record CaseRuleEvaluationLinkEntity : BaseEntity
{
    public required Guid CaseId { get; init; }
    public CaseEntity Case { get; init; } = null!;
    public required Guid RuleEvaluationId { get; init; }
    public RuleEvaluationEntity RuleEvaluation { get; init; } = null!;
}