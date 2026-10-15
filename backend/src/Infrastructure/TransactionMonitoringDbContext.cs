using System.Diagnostics;
using System.Linq.Expressions;
using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure;

public class TransactionMonitoringDbContext(DbContextOptions options, IModeContext modeContext) : IdentityDbContext<ApplicationUserEntity, ApplicationRoleEntity, Guid>(options), IUnitOfWork
{
    // EF turns a context member access in a filter into a parameter, so this is evaluated per query.
    public int? ModeFilterId => (int?)modeContext.Current;

    public const string DatabaseConnectionName = "TransactionMonitoringDb";

    public DbSet<ApiClientEntity> ApiClients { get; init; }
    public DbSet<ApiClientSecretEntity> ApiClientSecrets { get; init; }
    public DbSet<ApiClientScopeEntity> ApiClientScopes { get; init; }
    public DbSet<ApiScopeEntity> ApiScopes { get; init; }
    public DbSet<OrganizationEntity> Organizations { get; init; }
    public DbSet<RuleEntity> Rules { get; init; }
    public DbSet<RuleVersionEntity> RuleVersions { get; init; }
    public DbSet<RuleDeploymentEntity> RuleDeployments { get; init; }
    public DbSet<DeploymentStrategyEntity> DeploymentStrategies { get; init; }
    public DbSet<RuleDeploymentStatusEntity> RuleDeploymentStatuses { get; init; }
    public DbSet<RuleActionEntity> RuleActions { get; init; }
    public DbSet<RuleMatchPolarityEntity> RuleMatchPolarities { get; init; }
    public DbSet<RuleSeverityEntity> RuleSeverities { get; init; }
    public DbSet<RuleSetEntity> RuleSets { get; init; }
    public DbSet<RuleEvaluationEntity> RuleEvaluations { get; init; }
    public DbSet<TransactionEntity> Transactions { get; init; }
    public DbSet<AuditLogEntity> AuditLogs { get; init; }
    public DbSet<AuditableEntityTypeEntity> AuditableEntityTypes { get; init; }
    public DbSet<AuditOperationEntity> AuditOperations { get; init; }
    public DbSet<UsageEntity> Usages { get; init; }
    public DbSet<InvoiceEntity> Invoices { get; init; }
    public DbSet<CaseEntity> Cases { get; init; }
    public DbSet<CaseRuleEvaluationLinkEntity> CaseRuleLinks { get; init; }
    public DbSet<DecisionEntity> Decisions { get; init; }
    public DbSet<DecisionRuleEvaluationLinkEntity> DecisionRuleLinks { get; init; }
    public DbSet<DecisionStatusEntity> DecisionStatuses { get; init; }
    public DbSet<DecisionReasonCodeEntity> DecisionReasonCodes { get; init; }
    public DbSet<HoldExpiryVerdictEntity> HoldExpiryVerdicts { get; init; }
    public DbSet<OutboxMessageEntity> OutboxMessages { get; init; }
    public DbSet<IdempotencyRecordEntity> IdempotencyRecords { get; init; }
    public DbSet<WebhookConfigurationEntity> WebhookConfigurations { get; init; }
    public DbSet<WebhookConfigurationEventEntity> WebhookConfigurationEvents { get; init; }
    public DbSet<CaseNoteEntity> CaseNotes { get; init; }
    public DbSet<BackTestStatusEntity> BackTestStatuses { get; init; }
    public DbSet<BackTestEntity> BackTests { get; init; }
    public DbSet<BackTestResultEntity> BackTestResults { get; init; }
    public DbSet<TransactionSchemaEntity> TransactionSchemas { get; init; }
    public DbSet<TransactionSchemaVersionEntity> TransactionSchemaVersions { get; init; }
    public DbSet<PartyEntity> Parties { get; init; }
    public DbSet<TransactionPartyEntity> TransactionParties { get; init; }
    public DbSet<PartyRoleEntity> PartyRoles { get; init; }
    public DbSet<PartyMappingEntity> PartyMappings { get; init; }
    public DbSet<PartyMappingVersionEntity> PartyMappingVersions { get; init; }
    public DbSet<VelocityProfileEntity> VelocityProfiles { get; init; }
    public DbSet<VelocityObservationEntity> VelocityObservations { get; init; }
    public DbSet<VelocityBackfillEntity> VelocityBackfills { get; init; }
    public DbSet<VelocityBackfillStatusEntity> VelocityBackfillStatuses { get; init; }
    public DbSet<AlertEntity> Alerts { get; init; }
    public DbSet<AlertStatusEntity> AlertStatuses { get; init; }
    public DbSet<AlertSuppressionReasonEntity> AlertSuppressionReasons { get; init; }
    public DbSet<CaseDispositionEntity> CaseDispositions { get; init; }
    public DbSet<CaseReasonCodeEntity> CaseReasonCodes { get; init; }
    public DbSet<RateLimitOverrideEntity> RateLimitOverrides { get; init; }
    public DbSet<RateLimitPoolEntity> RateLimitPools { get; init; }
    public DbSet<CaseGroupingScopeEntity> CaseGroupingScopes { get; init; }
    public DbSet<CaseGroupingSeverityPolicyEntity> CaseGroupingSeverityPolicies { get; init; }
    public DbSet<QueueEntity> Queues { get; init; }
    public DbSet<QueueMemberEntity> QueueMembers { get; init; }
    public DbSet<QueueRuleEntity> QueueRules { get; init; }
    public DbSet<QueueRoutingPolicyEntity> QueueRoutingPolicies { get; init; }
    public DbSet<CaseSlaTargetEntity> CaseSlaTargets { get; init; }
    public DbSet<EscalationRuleEntity> EscalationRules { get; init; }
    public DbSet<ApprovalEntity> Approvals { get; init; }
    public DbSet<ApprovalPolicyEntity> ApprovalPolicies { get; init; }
    public DbSet<ApprovalStatusEntity> ApprovalStatuses { get; init; }
    public DbSet<ApprovalSubjectEntity> ApprovalSubjects { get; init; }
    public DbSet<AttachmentEntity> Attachments { get; init; }
    public DbSet<AttachmentScanStatusEntity> AttachmentScanStatuses { get; init; }
    public DbSet<CaseLinkEntity> CaseLinks { get; init; }
    public DbSet<NotificationSubscriptionEntity> NotificationSubscriptions { get; init; }
    public DbSet<NotificationChannelEntity> NotificationChannels { get; init; }
    public DbSet<SuspiciousActivityReportEntity> SuspiciousActivityReports { get; init; }
    public DbSet<SuspiciousActivityReportStatusEntity> SuspiciousActivityReportStatuses { get; init; }
    public DbSet<SuspiciousActivityReportPolicyEntity> SuspiciousActivityReportPolicies { get; init; }
    public DbSet<OrganizationDataKeyEntity> OrganizationDataKeys { get; init; }
    public DbSet<AuditAnchorEntity> AuditAnchors { get; init; }
    public DbSet<PermissionEntity> Permissions { get; init; }
    public DbSet<PermissionScopeTypeEntity> PermissionScopeTypes { get; init; }
    public DbSet<RolePermissionEntity> RolePermissions { get; init; }
    public DbSet<RoleAssignmentEntity> RoleAssignments { get; init; }
    public DbSet<SubOrganizationEntity> SubOrganizations { get; init; }
    public DbSet<ModeEntity> Modes { get; init; }
    public DbSet<SimulationEntity> Simulations { get; init; }
    public DbSet<SimulationStatusEntity> SimulationStatuses { get; init; }
    public DbSet<SimulationScenarioEntity> SimulationScenarios { get; init; }
    public DbSet<PlanEntity> Plans { get; init; }
    public DbSet<PlanVersionEntity> PlanVersions { get; init; }
    public DbSet<PlanPriceEntity> PlanPrices { get; init; }
    public DbSet<PlanPriceTierEntity> PlanPriceTiers { get; init; }
    public DbSet<PricingModelEntity> PricingModels { get; init; }
    public DbSet<BillableResourceEntity> BillableResources { get; init; }
    public DbSet<InvoiceLineEntity> InvoiceLines { get; init; }
    public DbSet<InvoiceLineTypeEntity> InvoiceLineTypes { get; init; }
    public DbSet<ContractDiscountEntity> ContractDiscounts { get; init; }
    public DbSet<DiscountTypeEntity> DiscountTypes { get; init; }
    public DbSet<InvoiceAdjustmentEntity> InvoiceAdjustments { get; init; }
    public DbSet<CommitmentEntity> Commitments { get; init; }
    public DbSet<SubscriptionEntity> Subscriptions { get; init; }
    public DbSet<CurrencyEntity> Currencies { get; init; }
    public DbSet<InvoiceStatusEntity> InvoiceStatuses { get; init; }
    public DbSet<TaxTreatmentEntity> TaxTreatments { get; init; }
    public DbSet<BillingEntityEntity> BillingEntities { get; init; }
    public DbSet<BillingProfileEntity> BillingProfiles { get; init; }
    public DbSet<ExchangeRateEntity> ExchangeRates { get; init; }
    public DbSet<ServiceCostRateEntity> ServiceCostRates { get; init; }
    public DbSet<InvoiceNumberSequenceEntity> InvoiceNumberSequences { get; init; }
    public DbSet<CreditNoteEntity> CreditNotes { get; init; }
    public DbSet<CreditNoteLineEntity> CreditNoteLines { get; init; }
    public DbSet<ServiceComponentEntity> ServiceComponents { get; init; }
    public DbSet<ProbeSourceEntity> ProbeSources { get; init; }
    public DbSet<IncidentStatusEntity> IncidentStatuses { get; init; }
    public DbSet<IncidentImpactEntity> IncidentImpacts { get; init; }
    public DbSet<MaintenanceStatusEntity> MaintenanceStatuses { get; init; }
    public DbSet<ComponentProbeEntity> ComponentProbes { get; init; }
    public DbSet<ComponentUptimeDayEntity> ComponentUptimeDays { get; init; }
    public DbSet<IncidentEntity> Incidents { get; init; }
    public DbSet<IncidentComponentEntity> IncidentComponents { get; init; }
    public DbSet<IncidentUpdateEntity> IncidentUpdates { get; init; }
    public DbSet<MaintenanceWindowEntity> MaintenanceWindows { get; init; }
    public DbSet<MaintenanceWindowComponentEntity> MaintenanceWindowComponents { get; init; }
    public DbSet<StatusSubscriptionEntity> StatusSubscriptions { get; init; }
    public DbSet<AvailabilityWindowEntity> AvailabilityWindows { get; init; }
    public DbSet<AvailabilityWindowLatencyBucketEntity> AvailabilityWindowLatencyBuckets { get; init; }
    public DbSet<AvailabilityCommitmentEntity> AvailabilityCommitments { get; init; }
    public DbSet<ServiceCreditBandEntity> ServiceCreditBands { get; init; }
    public DbSet<ServiceCreditEntity> ServiceCredits { get; init; }

    public DbSet<PaymentEntity> Payments { get; init; }
    public DbSet<PaymentStatusEntity> PaymentStatuses { get; init; }
    public DbSet<PaymentMethodKindEntity> PaymentMethodKinds { get; init; }
    public DbSet<AccountEntryEntity> AccountEntries { get; init; }
    public DbSet<AccountEntryReasonEntity> AccountEntryReasons { get; init; }
    public DbSet<DunningStateEntity> DunningStates { get; init; }
    public DbSet<DunningStageEntity> DunningStages { get; init; }
    public DbSet<ProviderEventRecordEntity> ProviderEventRecords { get; init; }
    public DbSet<InvitationEntity> Invitations { get; init; }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken stoppingToken)
    {
        Activity.Current?.AddEvent(new ActivityEvent("Repository Save Requested", DateTimeOffset.UtcNow));

        try
        {
            await base.SaveChangesAsync(stoppingToken);
            Activity.Current?.AddEvent(new ActivityEvent("Repository Save Successful", DateTimeOffset.UtcNow));
        }
        catch (Exception exception)
        {
            Activity.Current?.AddException(exception);
            Activity.Current?.AddEvent(new ActivityEvent("Repository Save Failed", DateTimeOffset.UtcNow));
            Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.Message);

            if (exception is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException })
            {
                throw new UniqueConstraintViolationException(postgresException.ConstraintName, exception);
            }

            throw;
        }
    }

    async Task<T> IUnitOfWork.ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken stoppingToken)
    {
        if (Database.CurrentTransaction is not null)
        {
            return await operation();
        }

        await using var transaction = await Database.BeginTransactionAsync(stoppingToken);

        var result = await operation();

        await transaction.CommitAsync(stoppingToken);

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionMonitoringDbContext).Assembly);
        modelBuilder.AddQuartz(builder => builder.UsePostgreSql(string.Empty, Schemas.Quartz));

        ApplyModeScoping(modelBuilder);

        // pg_trgm is trusted, so the database owner can create it without superuser.
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.HasSequence<int>(Sequences.CaseNumber, Schemas.Service)
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.HasSequence<int>(Sequences.TransactionNumber, Schemas.Service)
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.HasSequence<int>(Sequences.AlertNumber, Schemas.Service)
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.HasSequence<int>(Sequences.SuspiciousActivityReportNumber, Schemas.Service)
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.HasSequence<int>(Sequences.IncidentNumber, Schemas.Service)
            .StartsAt(1)
            .IncrementsBy(1);
    }

    private void ApplyModeScoping(ModelBuilder modelBuilder)
    {
        var modeFilterId = Expression.Property(
            Expression.Constant(this),
            nameof(ModeFilterId));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IModeScoped).IsAssignableFrom(entityType.ClrType) || entityType.BaseType is not null)
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "x");

            var body = Expression.OrElse(
                Expression.Equal(modeFilterId, Expression.Constant(null, typeof(int?))),
                Expression.Equal(
                    Expression.Convert(Expression.Property(parameter, nameof(IModeScoped.ModeId)), typeof(int?)),
                    modeFilterId));

            // Live is the database default, so pre-existing and unstamped rows land in live.
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(IModeScoped.ModeId))
                .HasDefaultValue((int)Mode.Live);

            modelBuilder.Entity(entityType.ClrType)
                .HasOne(typeof(ModeEntity))
                .WithMany()
                .HasForeignKey(nameof(IModeScoped.ModeId))
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
