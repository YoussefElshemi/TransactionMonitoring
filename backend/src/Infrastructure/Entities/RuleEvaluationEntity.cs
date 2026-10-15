using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RuleEvaluationEntity : DeletableEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid RuleEvaluationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid RuleId { get; init; }
    public RuleEntity Rule { get; init; } = null!;
    public required Guid RuleVersionId { get; init; }
    public RuleVersionEntity RuleVersion { get; init; } = null!;
    public required Guid TransactionId { get; init; }
    public TransactionEntity Transaction { get; init; } = null!;
    public required bool Matched { get; init; }
    public required string? ConditionMatches { get; init; }

    public ICollection<CaseRuleEvaluationLinkEntity> CaseRuleEvaluationLinks { get; init; } = [];
    public ICollection<CaseEntity> Cases { get; init; } = [];
    public ICollection<DecisionRuleEvaluationLinkEntity> DecisionRuleEvaluationLinks { get; init; } = [];
    public ICollection<DecisionEntity> Decisions { get; init; } = [];
    public ICollection<AlertEntity> Alerts { get; init; } = [];
}