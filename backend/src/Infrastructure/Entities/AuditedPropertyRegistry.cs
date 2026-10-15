using Core.Enums;
using Core.Extensions;
using Core.Helpers;

namespace Infrastructure.Entities;

public static class AuditedPropertyRegistry
{
    public static readonly IReadOnlyCollection<AuditableEntityType> Retired =
    [
        AuditableEntityType.ApiKey,
        AuditableEntityType.OnCallShift
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> RateLimitOverride =
    [
        AuditedProperty.Mapped<int?, string>(nameof(RateLimitOverrideEntity.PoolId), poolId => poolId.HasValue ? ((RateLimitPool)poolId.Value).ToString() : "All", "Pool"),
        nameof(RateLimitOverrideEntity.PermitLimit),
        nameof(RateLimitOverrideEntity.TokensPerPeriod),
        nameof(RateLimitOverrideEntity.ReplenishmentPeriodSeconds),
        nameof(RateLimitOverrideEntity.Reason),
        nameof(RateLimitOverrideEntity.ExpiresAt),
        nameof(RateLimitOverrideEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Rule =
    [
        nameof(RuleEntity.FriendlyName),
        AuditedProperty.Mapped<string, string>(nameof(RuleEntity.RuleBody), body => body.FromBase64String()),
        AuditedProperty.Mapped<int, string>(nameof(RuleEntity.ActionId), actionId => ((RuleAction)actionId).ToString(), "Action"),
        AuditedProperty.Mapped<int, string>(nameof(RuleEntity.SeverityId), severityId => ((RuleSeverity)severityId).ToString(), "Severity"),
        nameof(RuleEntity.RiskWeight),
        nameof(RuleEntity.HoldWindow),
        AuditedProperty.Mapped<int, string>(nameof(RuleEntity.HoldExpiryVerdictId), verdictId => ((HoldExpiryVerdict)verdictId).ToString(), "HoldExpiryVerdict"),

        AuditedProperty.Mapped<int, string>(nameof(RuleEntity.AlertSubjectRoleId), roleId => ((PartyRole)roleId).ToString(), "AlertSubjectRole"),
        nameof(RuleEntity.DeduplicationWindow),
        nameof(RuleEntity.IsEnabled),
        nameof(RuleEntity.RuleSetId),
        nameof(RuleEntity.CurrentVersionNumber),
        nameof(RuleEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> RuleSet =
    [
        nameof(RuleSetEntity.FriendlyName),
        nameof(RuleSetEntity.IsEnabled),
        nameof(RuleSetEntity.ReviewThreshold),
        nameof(RuleSetEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Case =
    [
        nameof(CaseEntity.FriendlyName),
        AuditedProperty.Mapped<int, string>(nameof(CaseEntity.StatusId), statusId => ((CaseStatus)statusId).ToString(), "Status"),
        AuditedProperty.Mapped<int, string>(nameof(CaseEntity.SeverityId), severityId => ((RuleSeverity)severityId).ToString(), "Severity"),

        AuditedProperty.Mapped<int, string>(nameof(CaseEntity.DispositionId), dispositionId => ((CaseDisposition)dispositionId).ToString(), "Disposition"),
        nameof(CaseEntity.CaseReasonCodeId),
        nameof(CaseEntity.AssignedToUserId),
        nameof(CaseEntity.AssignedAt),

        nameof(CaseEntity.QueueId),
        nameof(CaseEntity.EscalatedAt),
        nameof(CaseEntity.EscalationRuleId),
        nameof(CaseEntity.ResolvedAt),
        nameof(CaseEntity.ReopenedAt),
        nameof(CaseEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> CaseReasonCode =
    [
        nameof(CaseReasonCodeEntity.Description),
        AuditedProperty.Mapped<int, string>(nameof(CaseReasonCodeEntity.DispositionId), dispositionId => ((CaseDisposition)dispositionId).ToString(), "Disposition"),
        nameof(CaseReasonCodeEntity.IsEnabled),
        nameof(CaseReasonCodeEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Queue =
    [
        nameof(QueueEntity.Name),
        nameof(QueueEntity.Description),
        nameof(QueueEntity.Priority),
        AuditedProperty.Mapped<int, string>(nameof(QueueEntity.RoutingPolicyId), policyId => ((QueueRoutingPolicy)policyId).ToString(), "RoutingPolicy"),
        AuditedProperty.Mapped<int, string>(nameof(QueueEntity.MinimumSeverityId), severityId => ((RuleSeverity)severityId).ToString(), "MinimumSeverity"),
        AuditedProperty.Mapped<string, string>(nameof(QueueEntity.Matcher), matcher => matcher.FromBase64String()),
        nameof(QueueEntity.IsEnabled),
        nameof(QueueEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> EscalationRule =
    [
        nameof(EscalationRuleEntity.Name),
        nameof(EscalationRuleEntity.Description),
        nameof(EscalationRuleEntity.Priority),
        nameof(EscalationRuleEntity.UnassignedForMinutes),
        nameof(EscalationRuleEntity.HoldExpiryWithinMinutes),
        AuditedProperty.Mapped<int, string>(nameof(EscalationRuleEntity.MinimumSeverityId), severityId => ((RuleSeverity)severityId).ToString(), "MinimumSeverity"),
        AuditedProperty.Mapped<string, string>(nameof(EscalationRuleEntity.Matcher), matcher => matcher.FromBase64String()),
        nameof(EscalationRuleEntity.TargetQueueId),
        AuditedProperty.Mapped<int, string>(nameof(EscalationRuleEntity.RaiseSeverityToId), severityId => ((RuleSeverity)severityId).ToString(), "RaiseSeverityTo"),
        nameof(EscalationRuleEntity.IsEnabled),
        nameof(EscalationRuleEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Approval =
    [
        AuditedProperty.Mapped<int, string>(nameof(ApprovalEntity.StatusId), statusId => ((ApprovalStatus)statusId).ToString(), "Status"),
        nameof(ApprovalEntity.ReviewedByUserId),
        nameof(ApprovalEntity.ReviewedAt),
        nameof(ApprovalEntity.ReviewNote)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Transaction =
    [
        nameof(TransactionEntity.ExternalReference),
        nameof(TransactionEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Decision =
    [
        AuditedProperty.Mapped<int, string>(nameof(DecisionEntity.StatusId), statusId => ((DecisionStatus)statusId).ToString(), "Status"),
        AuditedProperty.Mapped<int, string>(nameof(DecisionEntity.ReasonCodeId), reasonCodeId => ((DecisionReasonCode)reasonCodeId).ToString(), "ReasonCode"),
        nameof(DecisionEntity.CaseId),
        nameof(DecisionEntity.ResolvedByUserId),
        nameof(DecisionEntity.HeldAt),
        nameof(DecisionEntity.ExpiresAt),
        AuditedProperty.Mapped<int, string>(nameof(DecisionEntity.HoldExpiryVerdictId), verdictId => ((HoldExpiryVerdict)verdictId).ToString(), "HoldExpiryVerdict"),
        nameof(DecisionEntity.ResolvedAt)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> CaseNote =
    [
        nameof(CaseNoteEntity.CaseId),
        nameof(CaseNoteEntity.Message),
        nameof(CaseNoteEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> SuspiciousActivityReport =
    [
        AuditedProperty.Mapped<int, string>(nameof(SuspiciousActivityReportEntity.StatusId), statusId => ((SuspiciousActivityReportStatus)statusId).ToString(), "Status"),
        nameof(SuspiciousActivityReportEntity.FiledByUserId),
        nameof(SuspiciousActivityReportEntity.FiledAt),
        nameof(SuspiciousActivityReportEntity.FilingReference),
        nameof(SuspiciousActivityReportEntity.AcknowledgedAt),
        nameof(SuspiciousActivityReportEntity.AcknowledgementReference),
        nameof(SuspiciousActivityReportEntity.NotFiledReason),
        nameof(SuspiciousActivityReportEntity.DecidedByUserId),
        nameof(SuspiciousActivityReportEntity.DecidedAt)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Attachment =
    [
        AuditedProperty.Mapped<int, string>(nameof(AttachmentEntity.ScanStatusId), scanStatusId => ((AttachmentScanStatus)scanStatusId).ToString(), "ScanStatus"),
        nameof(AttachmentEntity.ScannedAt),
        nameof(AttachmentEntity.ThreatName),
        nameof(AttachmentEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> CaseLink =
    [
        nameof(CaseLinkEntity.Reason),
        nameof(CaseLinkEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> NotificationSubscription =
    [
        nameof(NotificationSubscriptionEntity.Destination),
        AuditedProperty.Mapped<int?, string?>(nameof(NotificationSubscriptionEntity.MinimumSeverityId), severityId => severityId.HasValue ? ((RuleSeverity)severityId.Value).ToString() : null, "MinimumSeverity"),
        nameof(NotificationSubscriptionEntity.NotifyOnBreach),
        nameof(NotificationSubscriptionEntity.IsEnabled),
        nameof(NotificationSubscriptionEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> ApiClient =
    [
        nameof(ApiClientEntity.FriendlyName),
        nameof(ApiClientEntity.IsEnabled),
        nameof(ApiClientEntity.ApiVersion),
        AuditedProperty.Related(nameof(ApiClientEntity.Scopes), AuditedRelationRegistry.ApiClientScopes),
        nameof(ApiClientEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> ApiClientSecret =
    [
        nameof(ApiClientSecretEntity.ExpiresAt),
        nameof(ApiClientSecretEntity.RevokedAt),
        nameof(ApiClientSecretEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> WebhookConfiguration =
    [
        nameof(WebhookConfigurationEntity.EndpointUrl),
        nameof(WebhookConfigurationEntity.IsEnabled),
        nameof(WebhookConfigurationEntity.SecretKeyRotatedAt),
        nameof(WebhookConfigurationEntity.PayloadVersion),
        nameof(WebhookConfigurationEntity.DisabledAt),
        nameof(WebhookConfigurationEntity.DisabledReason),
        nameof(WebhookConfigurationEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Plan =
    [
        nameof(PlanEntity.Name),
        nameof(PlanEntity.Description),
        nameof(PlanEntity.IsEnabled),
        nameof(PlanEntity.CurrentVersionNumber),
        nameof(PlanEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Subscription =
    [
        nameof(SubscriptionEntity.PlanVersionId),
        nameof(SubscriptionEntity.EffectiveFrom),
        nameof(SubscriptionEntity.EffectiveTo),
        nameof(SubscriptionEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> ContractDiscount =
    [
        AuditedProperty.Mapped<int, string>(nameof(ContractDiscountEntity.DiscountTypeId), typeId => ((DiscountType)typeId).ToString(), "DiscountType"),
        nameof(ContractDiscountEntity.Value),
        nameof(ContractDiscountEntity.Description),
        nameof(ContractDiscountEntity.EffectiveFrom),
        nameof(ContractDiscountEntity.EffectiveTo),
        nameof(ContractDiscountEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> InvoiceAdjustment =
    [
        nameof(InvoiceAdjustmentEntity.Month),
        nameof(InvoiceAdjustmentEntity.Year),
        nameof(InvoiceAdjustmentEntity.Description),
        nameof(InvoiceAdjustmentEntity.Amount),
        nameof(InvoiceAdjustmentEntity.AppliedToInvoiceId),
        nameof(InvoiceAdjustmentEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Commitment =
    [
        nameof(CommitmentEntity.Amount),
        nameof(CommitmentEntity.EffectiveFrom),
        nameof(CommitmentEntity.EffectiveTo),
        nameof(CommitmentEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> BillingEntity =
    [
        nameof(BillingEntityEntity.LegalName),
        nameof(BillingEntityEntity.Address),
        nameof(BillingEntityEntity.CountryCode),
        nameof(BillingEntityEntity.TaxIdentifier),
        AuditedProperty.Mapped<int, string>(nameof(BillingEntityEntity.CurrencyId), currencyId => ((Currency)currencyId).ToString(), "Currency"),
        nameof(BillingEntityEntity.IsEnabled),
        nameof(BillingEntityEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> BillingProfile =
    [
        nameof(BillingProfileEntity.BillingEntityId),
        nameof(BillingProfileEntity.LegalName),
        nameof(BillingProfileEntity.ContactName),
        nameof(BillingProfileEntity.ContactEmail),
        nameof(BillingProfileEntity.Address),
        nameof(BillingProfileEntity.CountryCode),
        nameof(BillingProfileEntity.TaxIdentifier),
        nameof(BillingProfileEntity.TaxIdentifierVerifiedAt),
        AuditedProperty.Mapped<int, string>(nameof(BillingProfileEntity.CurrencyId), currencyId => ((Currency)currencyId).ToString(), "Currency"),
        nameof(BillingProfileEntity.PaymentTerms),
        nameof(BillingProfileEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> CreditNote =
    [
        nameof(CreditNoteEntity.CreditNoteNumber),
        nameof(CreditNoteEntity.InvoiceId),
        nameof(CreditNoteEntity.Reason),
        nameof(CreditNoteEntity.NetAmount),
        nameof(CreditNoteEntity.TaxAmount),
        nameof(CreditNoteEntity.Amount)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Organization =
    [
        nameof(OrganizationEntity.FriendlyName),
        AuditedProperty.Mapped<int, string>(nameof(OrganizationEntity.StatusId), statusId => ((OrganizationStatus)statusId).ToString(), "Status"),
        nameof(OrganizationEntity.HoldWindow),
        AuditedProperty.Mapped<int, string>(nameof(OrganizationEntity.HoldExpiryVerdictId), verdictId => ((HoldExpiryVerdict)verdictId).ToString(), "HoldExpiryVerdict"),
        nameof(OrganizationEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> ApplicationUser =
    [
        nameof(ApplicationUserEntity.UserName),
        nameof(ApplicationUserEntity.Email),
        nameof(ApplicationUserEntity.EmailConfirmed),
        nameof(ApplicationUserEntity.OrganizationId),
        AuditedProperty.Related(nameof(ApplicationUserEntity.Roles), AuditedRelationRegistry.ApplicationUserRoles),
        nameof(ApplicationUserEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> TransactionSchema =
    [
        AuditedProperty.Mapped<string, string>(nameof(TransactionSchemaEntity.Fields), fields => fields.FromBase64String()),
        nameof(TransactionSchemaEntity.CurrentVersionNumber),
        nameof(TransactionSchemaEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> PartyMapping =
    [
        AuditedProperty.Mapped<string, string>(nameof(PartyMappingEntity.Mappings), mappings => mappings.FromBase64String()),
        nameof(PartyMappingEntity.CurrentVersionNumber),
        nameof(PartyMappingEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> VelocityProfile =
    [
        AuditedProperty.Mapped<string, string>(nameof(VelocityProfileEntity.Metrics), metrics => metrics.FromBase64String()),
        AuditedProperty.Mapped<string, string>(nameof(VelocityProfileEntity.Thresholds), thresholds => thresholds.FromBase64String()),
        nameof(VelocityProfileEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Party =
    [
        nameof(PartyEntity.DisplayName),
        nameof(PartyEntity.RiskScore),
        nameof(PartyEntity.RiskNote),
        nameof(PartyEntity.RiskSetByUserId)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Alert =
    [
        AuditedProperty.Mapped<int, string>(nameof(AlertEntity.StatusId), statusId => ((AlertStatus)statusId).ToString(), "Status"),
        nameof(AlertEntity.CaseId),
        AuditedProperty.Mapped<int, string>(nameof(AlertEntity.SuppressionReasonId), reasonId => ((AlertSuppressionReason)reasonId).ToString(), "SuppressionReason"),
        nameof(AlertEntity.SuppressedByAlertId),
        nameof(AlertEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Role =
    [
        nameof(ApplicationRoleEntity.Name),
        nameof(ApplicationRoleEntity.Description),

        AuditedProperty.Related(nameof(ApplicationRoleEntity.RolePermissions), AuditedRelationRegistry.RolePermissions),
        nameof(ApplicationRoleEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> RoleAssignment =
    [
        nameof(RoleAssignmentEntity.UserId),
        nameof(RoleAssignmentEntity.RoleId),
        AuditedProperty.Mapped<int, string>(nameof(RoleAssignmentEntity.ScopeTypeId), scopeTypeId => ((PermissionScopeType)scopeTypeId).ToString(), "ScopeType"),
        nameof(RoleAssignmentEntity.SubOrganizationId),
        nameof(RoleAssignmentEntity.RuleSetId),
        nameof(RoleAssignmentEntity.QueueId),
        nameof(RoleAssignmentEntity.AssignedByUserId)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Invitation =
    [
        nameof(InvitationEntity.Email),
        AuditedProperty.Mapped<string, string>(nameof(InvitationEntity.RoleId), roleId => RoleHelper.TryMapToUserRole(Guid.Parse(roleId), out var userRole) ? userRole.ToString() : roleId, "Role"),
        nameof(InvitationEntity.SubOrganizationId),
        nameof(InvitationEntity.InvitedByUserId),
        nameof(InvitationEntity.ExpiresAt),
        nameof(InvitationEntity.AcceptedAt),
        nameof(InvitationEntity.AcceptedByUserId),
        nameof(InvitationEntity.RevokedAt),
        nameof(InvitationEntity.RevokedByUserId)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> SubOrganization =
    [
        nameof(SubOrganizationEntity.Name),
        nameof(SubOrganizationEntity.Description),
        nameof(SubOrganizationEntity.ExternalReference),
        nameof(SubOrganizationEntity.IsEnabled),

        nameof(SubOrganizationEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> Incident =
    [
        nameof(IncidentEntity.Title),
        nameof(IncidentEntity.Summary),
        AuditedProperty.Mapped<int, string>(nameof(IncidentEntity.StatusId), statusId => ((IncidentStatus)statusId).ToString(), "Status"),
        AuditedProperty.Mapped<int, string>(nameof(IncidentEntity.ImpactId), impactId => ((IncidentImpact)impactId).ToString(), "Impact"),
        nameof(IncidentEntity.StartedAt),
        nameof(IncidentEntity.ResolvedAt),
        nameof(IncidentEntity.RootCause),
        nameof(IncidentEntity.Remediation),
        nameof(IncidentEntity.ReviewPublishedAt),
        nameof(IncidentEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> MaintenanceWindow =
    [
        nameof(MaintenanceWindowEntity.Title),
        nameof(MaintenanceWindowEntity.Description),
        AuditedProperty.Mapped<int, string>(nameof(MaintenanceWindowEntity.StatusId), statusId => ((MaintenanceStatus)statusId).ToString(), "Status"),
        nameof(MaintenanceWindowEntity.ScheduledStart),
        nameof(MaintenanceWindowEntity.ScheduledEnd),
        nameof(MaintenanceWindowEntity.AnnouncedAt),
        nameof(MaintenanceWindowEntity.IsDeleted)
    ];

    public static readonly IReadOnlyCollection<AuditedProperty> AvailabilityCommitment =
    [
        nameof(AvailabilityCommitmentEntity.AvailabilityTarget),
        nameof(AvailabilityCommitmentEntity.LatencyTargetMilliseconds),
        nameof(AvailabilityCommitmentEntity.LatencyPercentile),
        nameof(AvailabilityCommitmentEntity.EffectiveFrom),
        nameof(AvailabilityCommitmentEntity.EffectiveTo),
        nameof(AvailabilityCommitmentEntity.IsDeleted)
    ];

    public static IReadOnlyCollection<AuditedProperty> For(AuditableEntityType entityType)
    {
        return entityType switch
        {
            AuditableEntityType.Rule => Rule,
            AuditableEntityType.RuleSet => RuleSet,
            AuditableEntityType.Case => Case,
            AuditableEntityType.Transaction => Transaction,
            AuditableEntityType.Decision => Decision,
            AuditableEntityType.CaseNote => CaseNote,
            AuditableEntityType.ApiClient => ApiClient,
            AuditableEntityType.ApiClientSecret => ApiClientSecret,
            AuditableEntityType.WebhookConfiguration => WebhookConfiguration,
            AuditableEntityType.Organization => Organization,
            AuditableEntityType.ApplicationUser => ApplicationUser,
            AuditableEntityType.TransactionSchema => TransactionSchema,
            AuditableEntityType.PartyMapping => PartyMapping,
            AuditableEntityType.Party => Party,
            AuditableEntityType.VelocityProfile => VelocityProfile,
            AuditableEntityType.Alert => Alert,
            AuditableEntityType.CaseReasonCode => CaseReasonCode,
            AuditableEntityType.Queue => Queue,
            AuditableEntityType.EscalationRule => EscalationRule,
            AuditableEntityType.Approval => Approval,
            AuditableEntityType.Attachment => Attachment,
            AuditableEntityType.CaseLink => CaseLink,
            AuditableEntityType.NotificationSubscription => NotificationSubscription,
            AuditableEntityType.SuspiciousActivityReport => SuspiciousActivityReport,
            AuditableEntityType.Role => Role,
            AuditableEntityType.RoleAssignment => RoleAssignment,
            AuditableEntityType.Invitation => Invitation,
            AuditableEntityType.SubOrganization => SubOrganization,
            AuditableEntityType.RateLimitOverride => RateLimitOverride,
            AuditableEntityType.Plan => Plan,
            AuditableEntityType.Subscription => Subscription,
            AuditableEntityType.ContractDiscount => ContractDiscount,
            AuditableEntityType.InvoiceAdjustment => InvoiceAdjustment,
            AuditableEntityType.Commitment => Commitment,
            AuditableEntityType.BillingEntity => BillingEntity,
            AuditableEntityType.BillingProfile => BillingProfile,
            AuditableEntityType.CreditNote => CreditNote,
            AuditableEntityType.Incident => Incident,
            AuditableEntityType.MaintenanceWindow => MaintenanceWindow,
            AuditableEntityType.AvailabilityCommitment => AvailabilityCommitment,
            _ => []
        };
    }
}
