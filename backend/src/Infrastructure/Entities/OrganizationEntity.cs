using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record OrganizationEntity : DeletableEntity, IAuditable
{
    public required Guid OrganizationId { get; init; }
    public required string FriendlyName { get; set; }
    public required int StatusId { get; set; }
    public OrganizationStatusEntity StatusEntity { get; init; } = null!;
    public required int? HoldWindow { get; set; }
    public required int? HoldExpiryVerdictId { get; set; }
    public HoldExpiryVerdictEntity? HoldExpiryVerdictEntity { get; init; }
    public required int? DeduplicationWindow { get; set; }
    public required int? CaseGroupingPeriod { get; set; }
    public required bool? CaseGroupingEnabled { get; set; }
    public required bool? DeduplicationEnabled { get; set; }
    public required int? CaseGroupingScopeId { get; set; }
    public CaseGroupingScopeEntity? CaseGroupingScopeEntity { get; init; }
    public required int? CaseGroupingSeverityPolicyId { get; set; }
    public CaseGroupingSeverityPolicyEntity? CaseGroupingSeverityPolicyEntity { get; init; }
    public required int? MaxAlertsPerCase { get; set; }

    public required int? SandboxMonthlyCredits { get; set; }

    public required string? DefaultLocale { get; set; }
    public required string? DefaultTimeZone { get; set; }
    public ICollection<ApplicationUserEntity> ApplicationUsers { get; init; } = [];
    public ICollection<ApiClientEntity> ApiClients { get; init; } = [];
    public ICollection<InvoiceEntity> Invoices { get; init; } = [];
    public ICollection<CreditNoteEntity> CreditNotes { get; init; } = [];

    public BillingProfileEntity? BillingProfile { get; init; }
    public ICollection<RuleEntity> Rules { get; init; } = [];
    public ICollection<RuleSetEntity> RuleSets { get; init; } = [];
    public ICollection<UsageEntity> Usages { get; init; } = [];
    public ICollection<RuleEvaluationEntity> RuleEvaluations { get; init; } = [];
    public ICollection<TransactionEntity> Transactions { get; init; } = [];
    public ICollection<CaseEntity> Cases { get; init; } = [];
    public ICollection<DecisionEntity> Decisions { get; init; } = [];
    public ICollection<WebhookConfigurationEntity> WebhookConfigurations { get; init; } = [];
    public ICollection<CaseReasonCodeEntity> CaseReasonCodes { get; init; } = [];
    public ICollection<RateLimitOverrideEntity> RateLimitOverrides { get; init; } = [];
    public ICollection<CaseNoteEntity> CaseNotes { get; init; } = [];
    public ICollection<AttachmentEntity> Attachments { get; init; } = [];
    public ICollection<CaseLinkEntity> CaseLinks { get; init; } = [];
    public ICollection<NotificationSubscriptionEntity> NotificationSubscriptions { get; init; } = [];
    public ICollection<IdempotencyRecordEntity> IdempotencyRecords { get; init; } = [];
    public ICollection<TransactionSchemaEntity> TransactionSchemas { get; init; } = [];
    public ICollection<PartyMappingEntity> PartyMappings { get; init; } = [];
    public ICollection<PartyEntity> Parties { get; init; } = [];
    public ICollection<VelocityProfileEntity> VelocityProfiles { get; init; } = [];
    public ICollection<VelocityObservationEntity> VelocityObservations { get; init; } = [];
    public ICollection<VelocityBackfillEntity> VelocityBackfills { get; init; } = [];
    public ICollection<AlertEntity> Alerts { get; init; } = [];
    public ICollection<QueueEntity> Queues { get; init; } = [];
    public ICollection<CaseSlaTargetEntity> CaseSlaTargets { get; init; } = [];
    public ICollection<EscalationRuleEntity> EscalationRules { get; init; } = [];
    public ICollection<SubOrganizationEntity> SubOrganizations { get; init; } = [];
    public ICollection<ApplicationRoleEntity> Roles { get; init; } = [];
    public ICollection<InvitationEntity> Invitations { get; init; } = [];
    public ICollection<SimulationEntity> Simulations { get; init; } = [];
    public ICollection<OutboxMessageEntity> OutboxMessages { get; init; } = [];
    public ICollection<PlanEntity> Plans { get; init; } = [];
    public ICollection<SubscriptionEntity> Subscriptions { get; init; } = [];
    public ICollection<ContractDiscountEntity> ContractDiscounts { get; init; } = [];
    public ICollection<InvoiceAdjustmentEntity> InvoiceAdjustments { get; init; } = [];
    public ICollection<CommitmentEntity> Commitments { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => OrganizationId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Organization;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Organization;
}
