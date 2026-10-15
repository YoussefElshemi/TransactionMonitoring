using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RuleEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid RuleId { get; init; }
    public required string FriendlyName { get; set; }
    public required string RuleBody { get; set; }
    public required int MatchPolarityId { get; set; }
    public RuleMatchPolarityEntity MatchPolarityEntity { get; init; } = null!;
    public required int ActionId { get; set; }
    public RuleActionEntity ActionEntity { get; init; } = null!;
    public required int SeverityId { get; set; }
    public RuleSeverityEntity SeverityEntity { get; init; } = null!;
    public required int RiskWeight { get; set; }
    public required int? HoldWindow { get; set; }
    public required int? HoldExpiryVerdictId { get; set; }
    public HoldExpiryVerdictEntity? HoldExpiryVerdictEntity { get; init; }
    public required int? AlertSubjectRoleId { get; set; }
    public PartyRoleEntity? AlertSubjectRoleEntity { get; init; }
    public required int? DeduplicationWindow { get; set; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required bool IsEnabled { get; set; }
    public required Guid RuleSetId { get; set; }
    public required Guid CreatedByUserId { get; init; }
    public required Guid CurrentRuleVersionId { get; set; }
    public required int CurrentVersionNumber { get; set; }
    public required bool HasActiveDeployment { get; set; }
    public ApplicationUserEntity CreatedByUser { get; init; } = null!;
    public RuleSetEntity RuleSet { get; init; } = null!;
    public ICollection<RuleEvaluationEntity> RuleEvaluations { get; init; } = [];
    public ICollection<RuleVersionEntity> RuleVersions { get; init; } = [];
    public ICollection<AlertEntity> Alerts { get; init; } = [];
    public ICollection<QueueRuleEntity> QueueRules { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => RuleId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Rule;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Rule;
}