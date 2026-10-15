namespace Infrastructure.Entities;

public record RuleVersionEntity : IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid RuleVersionId { get; init; }
    public required Guid RuleId { get; init; }
    public RuleEntity Rule { get; init; } = null!;
    public required int VersionNumber { get; init; }
    public required string RuleBody { get; init; }
    public required int MatchPolarityId { get; init; }
    public RuleMatchPolarityEntity MatchPolarityEntity { get; init; } = null!;
    public required int ActionId { get; init; }
    public RuleActionEntity ActionEntity { get; init; } = null!;
    public required int SeverityId { get; init; }
    public RuleSeverityEntity SeverityEntity { get; init; } = null!;
    public required int RiskWeight { get; init; }
    public required Guid RuleSetId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public ICollection<AlertEntity> Alerts { get; init; } = [];
}
