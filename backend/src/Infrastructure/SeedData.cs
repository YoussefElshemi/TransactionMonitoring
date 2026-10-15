using System.Collections.Immutable;
using Core.Enums;
using Core.Helpers;
using Infrastructure.Entities;

namespace Infrastructure;

public static class SeedData
{
    private static readonly Dictionary<UserRole, string> BuiltInRoleDescriptions = new()
    {
        [UserRole.User] = "Works the desk: opens cases, records decisions, files what needs reporting.",
        [UserRole.Manager] = "Runs the desk: everything an analyst does, plus the configuration behind it and the roles the organization runs on.",
        [UserRole.Admin] = "Runs the installation. Every permission, in every organization.",
        [UserRole.Auditor] = "Sees everything and changes nothing."
    };

    // Ids derive from the enum value so a UserRoles row reads back as a role without a join.
    public static readonly ImmutableArray<ApplicationRoleEntity> IdentityRoles = [..CreateIdentityRoles()];

    // Fresh instances: the behavioural suite reinserts these after every reset (TRUNCATE Organizations CASCADE empties Roles), from parallel classes.
    public static IEnumerable<ApplicationRoleEntity> CreateIdentityRoles()
    {
        return Enum.GetValues(typeof(UserRole))
            .Cast<UserRole>()
            .Select(x => new ApplicationRoleEntity
            {
                Id = RoleHelper.MapFromUserRole(x),
                Name = x.ToString(),
                NormalizedName = x.ToString().ToUpperInvariant(),
                OrganizationId = null,
                Description = BuiltInRoleDescriptions[x],
                IsBuiltIn = true,
                CreatedAt = default,
                UpdatedAt = default,
                IsDeleted = false,
                DeletedAt = null
            });
    }

    public static readonly ImmutableArray<RolePermissionEntity> BuiltInRolePermissions = [..CreateBuiltInRolePermissions()];

    public static IEnumerable<RolePermissionEntity> CreateBuiltInRolePermissions()
    {
        return Enum.GetValues(typeof(UserRole))
            .Cast<UserRole>()
            .SelectMany(role => PermissionBundle.For(role).Select(permission => new RolePermissionEntity
            {
                RoleId = RoleHelper.MapFromUserRole(role),
                PermissionId = (int)permission,
                CreatedAt = default,
                UpdatedAt = default
            }));
    }

    public static readonly ImmutableArray<PermissionEntity> Permissions =
    [
        ..Enum.GetValues(typeof(Permission))
            .Cast<Permission>()
            .Select(x => new PermissionEntity
            {
                PermissionId = (int)x,
                Name = PermissionHelper.ToPermissionString(x),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<PermissionScopeTypeEntity> PermissionScopeTypes =
    [
        ..Enum.GetValues(typeof(PermissionScopeType))
            .Cast<PermissionScopeType>()
            .Select(x => new PermissionScopeTypeEntity
            {
                PermissionScopeTypeId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<ApiScopeEntity> ApiScopes =
    [
        ..Enum.GetValues(typeof(ApiScope))
            .Cast<ApiScope>()
            .Select(x => new ApiScopeEntity
            {
                ApiScopeId = (int)x,
                Name = ApiScopeHelper.ToScopeString(x),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<CaseStatusEntity> CaseStatuses =
    [
        ..Enum.GetValues(typeof(CaseStatus))
            .Cast<CaseStatus>()
            .Select(x => new CaseStatusEntity
            {
                CaseStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<RateLimitPoolEntity> RateLimitPools =
    [
        ..Enum.GetValues(typeof(RateLimitPool))
            .Cast<RateLimitPool>()
            .Select(x => new RateLimitPoolEntity
            {
                RateLimitPoolId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<CaseDispositionEntity> CaseDispositions =
    [
        ..Enum.GetValues(typeof(CaseDisposition))
            .Cast<CaseDisposition>()
            .Select(x => new CaseDispositionEntity
            {
                CaseDispositionId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<CaseGroupingScopeEntity> CaseGroupingScopes =
    [
        ..Enum.GetValues(typeof(CaseGroupingScope))
            .Cast<CaseGroupingScope>()
            .Select(x => new CaseGroupingScopeEntity
            {
                CaseGroupingScopeId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<CaseGroupingSeverityPolicyEntity> CaseGroupingSeverityPolicies =
    [
        ..Enum.GetValues(typeof(CaseGroupingSeverityPolicy))
            .Cast<CaseGroupingSeverityPolicy>()
            .Select(x => new CaseGroupingSeverityPolicyEntity
            {
                CaseGroupingSeverityPolicyId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<AlertStatusEntity> AlertStatuses =
    [
        ..Enum.GetValues(typeof(AlertStatus))
            .Cast<AlertStatus>()
            .Select(x => new AlertStatusEntity
            {
                AlertStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<QueueRoutingPolicyEntity> QueueRoutingPolicies =
    [
        ..Enum.GetValues(typeof(QueueRoutingPolicy))
            .Cast<QueueRoutingPolicy>()
            .Select(x => new QueueRoutingPolicyEntity
            {
                QueueRoutingPolicyId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<AlertSuppressionReasonEntity> AlertSuppressionReasons =
    [
        ..Enum.GetValues(typeof(AlertSuppressionReason))
            .Cast<AlertSuppressionReason>()
            .Select(x => new AlertSuppressionReasonEntity
            {
                AlertSuppressionReasonId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<DecisionStatusEntity> DecisionStatuses =
    [
        ..Enum.GetValues(typeof(DecisionStatus))
            .Cast<DecisionStatus>()
            .Select(x => new DecisionStatusEntity
            {
                DecisionStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<DecisionReasonCodeEntity> DecisionReasonCodes =
    [
        ..Enum.GetValues(typeof(DecisionReasonCode))
            .Cast<DecisionReasonCode>()
            .Select(x => new DecisionReasonCodeEntity
            {
                DecisionReasonCodeId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<HoldExpiryVerdictEntity> HoldExpiryVerdicts =
    [
        ..Enum.GetValues(typeof(HoldExpiryVerdict))
            .Cast<HoldExpiryVerdict>()
            .Select(x => new HoldExpiryVerdictEntity
            {
                HoldExpiryVerdictId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<OrganizationStatusEntity> OrganizationStatuses =
    [
        ..Enum.GetValues(typeof(OrganizationStatus))
            .Cast<OrganizationStatus>()
            .Select(x => new OrganizationStatusEntity
            {
                OrganizationStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<BackTestStatusEntity> BackTestStatuses =
    [
        ..Enum.GetValues(typeof(BackTestStatus))
            .Cast<BackTestStatus>()
            .Select(x => new BackTestStatusEntity
            {
                BackTestStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<DeploymentStrategyEntity> DeploymentStrategies =
    [
        ..Enum.GetValues(typeof(DeploymentStrategy))
            .Cast<DeploymentStrategy>()
            .Select(x => new DeploymentStrategyEntity
            {
                DeploymentStrategyId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<RuleDeploymentStatusEntity> RuleDeploymentStatuses =
    [
        ..Enum.GetValues(typeof(RuleDeploymentStatus))
            .Cast<RuleDeploymentStatus>()
            .Select(x => new RuleDeploymentStatusEntity
            {
                RuleDeploymentStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<ApprovalStatusEntity> ApprovalStatuses =
    [
        ..Enum.GetValues(typeof(ApprovalStatus))
            .Cast<ApprovalStatus>()
            .Select(x => new ApprovalStatusEntity
            {
                ApprovalStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<ApprovalSubjectEntity> ApprovalSubjects =
    [
        ..Enum.GetValues(typeof(ApprovalSubject))
            .Cast<ApprovalSubject>()
            .Select(x => new ApprovalSubjectEntity
            {
                ApprovalSubjectId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<SuspiciousActivityReportStatusEntity> SuspiciousActivityReportStatuses =
    [
        ..Enum.GetValues(typeof(SuspiciousActivityReportStatus))
            .Cast<SuspiciousActivityReportStatus>()
            .Select(x => new SuspiciousActivityReportStatusEntity
            {
                SuspiciousActivityReportStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<AttachmentScanStatusEntity> AttachmentScanStatuses =
    [
        ..Enum.GetValues(typeof(AttachmentScanStatus))
            .Cast<AttachmentScanStatus>()
            .Select(x => new AttachmentScanStatusEntity
            {
                AttachmentScanStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<NotificationChannelEntity> NotificationChannels =
    [
        ..Enum.GetValues(typeof(NotificationChannel))
            .Cast<NotificationChannel>()
            .Select(x => new NotificationChannelEntity
            {
                NotificationChannelId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<RuleActionEntity> RuleActions =
    [
        ..Enum.GetValues(typeof(RuleAction))
            .Cast<RuleAction>()
            .Select(x => new RuleActionEntity
            {
                RuleActionId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<RuleMatchPolarityEntity> RuleMatchPolarities =
    [
        ..Enum.GetValues(typeof(RuleMatchPolarity))
            .Cast<RuleMatchPolarity>()
            .Select(x => new RuleMatchPolarityEntity
            {
                RuleMatchPolarityId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<PartyRoleEntity> PartyRoles =
    [
        ..Enum.GetValues(typeof(PartyRole))
            .Cast<PartyRole>()
            .Select(x => new PartyRoleEntity
            {
                PartyRoleId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<VelocityBackfillStatusEntity> VelocityBackfillStatuses =
    [
        ..Enum.GetValues(typeof(VelocityBackfillStatus))
            .Cast<VelocityBackfillStatus>()
            .Select(x => new VelocityBackfillStatusEntity
            {
                VelocityBackfillStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<RuleSeverityEntity> RuleSeverities =
    [
        ..Enum.GetValues(typeof(RuleSeverity))
            .Cast<RuleSeverity>()
            .Select(x => new RuleSeverityEntity
            {
                RuleSeverityId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<AuditableEntityTypeEntity> AuditableEntityTypes =
    [
        ..Enum.GetValues(typeof(AuditableEntityType))
            .Cast<AuditableEntityType>()
            .Select(x => new AuditableEntityTypeEntity
            {
                AuditableEntityTypeId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<AuditOperationEntity> AuditOperations =
    [
        ..Enum.GetValues(typeof(AuditOperation))
            .Cast<AuditOperation>()
            .Select(x => new AuditOperationEntity
            {
                AuditOperationId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<ModeEntity> Modes =
    [
        ..Enum.GetValues(typeof(Mode))
            .Cast<Mode>()
            .Select(x => new ModeEntity
            {
                ModeId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<SimulationStatusEntity> SimulationStatuses =
    [
        ..Enum.GetValues(typeof(SimulationStatus))
            .Cast<SimulationStatus>()
            .Select(x => new SimulationStatusEntity
            {
                SimulationStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<SimulationScenarioEntity> SimulationScenarios =
    [
        ..Enum.GetValues(typeof(SimulationScenario))
            .Cast<SimulationScenario>()
            .Select(x => new SimulationScenarioEntity
            {
                SimulationScenarioId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<PricingModelEntity> PricingModels =
    [
        ..Enum.GetValues(typeof(PricingModel))
            .Cast<PricingModel>()
            .Select(x => new PricingModelEntity
            {
                PricingModelId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<BillableResourceEntity> BillableResources =
    [
        ..Enum.GetValues(typeof(BillableResource))
            .Cast<BillableResource>()
            .Select(x => new BillableResourceEntity
            {
                BillableResourceId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<InvoiceLineTypeEntity> InvoiceLineTypes =
    [
        ..Enum.GetValues(typeof(InvoiceLineType))
            .Cast<InvoiceLineType>()
            .Select(x => new InvoiceLineTypeEntity
            {
                InvoiceLineTypeId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<CurrencyEntity> Currencies =
    [
        ..Enum.GetValues(typeof(Currency))
            .Cast<Currency>()
            .Select(x => new CurrencyEntity
            {
                CurrencyId = (int)x,
                Name = x.ToString().ToUpperInvariant(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<InvoiceStatusEntity> InvoiceStatuses =
    [
        ..Enum.GetValues(typeof(InvoiceStatus))
            .Cast<InvoiceStatus>()
            .Select(x => new InvoiceStatusEntity
            {
                InvoiceStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<PaymentStatusEntity> PaymentStatuses =
    [
        ..Enum.GetValues(typeof(PaymentStatus))
            .Cast<PaymentStatus>()
            .Select(x => new PaymentStatusEntity
            {
                PaymentStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<PaymentMethodKindEntity> PaymentMethodKinds =
    [
        ..Enum.GetValues(typeof(PaymentMethodKind))
            .Cast<PaymentMethodKind>()
            .Select(x => new PaymentMethodKindEntity
            {
                PaymentMethodKindId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<AccountEntryReasonEntity> AccountEntryReasons =
    [
        ..Enum.GetValues(typeof(AccountEntryReason))
            .Cast<AccountEntryReason>()
            .Select(x => new AccountEntryReasonEntity
            {
                AccountEntryReasonId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<DunningStageEntity> DunningStages =
    [
        ..Enum.GetValues(typeof(DunningStage))
            .Cast<DunningStage>()
            .Select(x => new DunningStageEntity
            {
                DunningStageId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<TaxTreatmentEntity> TaxTreatments =
    [
        ..Enum.GetValues(typeof(TaxTreatment))
            .Cast<TaxTreatment>()
            .Select(x => new TaxTreatmentEntity
            {
                TaxTreatmentId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<DiscountTypeEntity> DiscountTypes =
    [
        ..Enum.GetValues(typeof(DiscountType))
            .Cast<DiscountType>()
            .Select(x => new DiscountTypeEntity
            {
                DiscountTypeId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<ServiceComponentEntity> ServiceComponents =
    [
        ..Enum.GetValues(typeof(ServiceComponent))
            .Cast<ServiceComponent>()
            .Select(x => new ServiceComponentEntity
            {
                ServiceComponentId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<IncidentStatusEntity> IncidentStatuses =
    [
        ..Enum.GetValues(typeof(IncidentStatus))
            .Cast<IncidentStatus>()
            .Select(x => new IncidentStatusEntity
            {
                IncidentStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<IncidentImpactEntity> IncidentImpacts =
    [
        ..Enum.GetValues(typeof(IncidentImpact))
            .Cast<IncidentImpact>()
            .Select(x => new IncidentImpactEntity
            {
                IncidentImpactId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<MaintenanceStatusEntity> MaintenanceStatuses =
    [
        ..Enum.GetValues(typeof(MaintenanceStatus))
            .Cast<MaintenanceStatus>()
            .Select(x => new MaintenanceStatusEntity
            {
                MaintenanceStatusId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];

    public static readonly ImmutableArray<ProbeSourceEntity> ProbeSources =
    [
        ..Enum.GetValues(typeof(ProbeSource))
            .Cast<ProbeSource>()
            .Select(x => new ProbeSourceEntity
            {
                ProbeSourceId = (int)x,
                Name = x.ToString(),
                CreatedAt = default,
                UpdatedAt = default
            })
    ];
}