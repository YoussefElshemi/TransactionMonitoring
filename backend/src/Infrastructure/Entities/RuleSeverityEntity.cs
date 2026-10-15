using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RuleSeverityEntity : BaseEntity
{
    public required int RuleSeverityId { get; init; }
    public required string Name { get; init; }
    public ICollection<RuleEntity> Rules { get; init; } = [];
    public ICollection<AlertEntity> Alerts { get; init; } = [];
    public ICollection<RuleVersionEntity> RuleVersions { get; init; } = [];
    public ICollection<CaseEntity> Cases { get; init; } = [];
    public ICollection<QueueEntity> Queues { get; init; } = [];
    public ICollection<CaseSlaTargetEntity> CaseSlaTargets { get; init; } = [];
    public ICollection<EscalationRuleEntity> EscalationRuleMinimums { get; init; } = [];
    public ICollection<EscalationRuleEntity> EscalationRuleRaises { get; init; } = [];
}
