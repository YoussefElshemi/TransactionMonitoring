using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Jobs;
using Core.Mappers;
using Core.Models.Common;
using Core.Models.Rules;
using Core.Models.VelocityProfiles;
using Core.ValueObjects;
using Quartz;

namespace Core.Services;

public class VelocityProfileService(
    IVelocityProfileRepository velocityProfileRepository,
    IVelocityBackfillRepository velocityBackfillRepository,
    ITransactionSchemaRepository transactionSchemaRepository,
    IRuleRepository ruleRepository,
    IUnitOfWork unitOfWork,
    ISchedulerFactory schedulerFactory,
    TimeProvider timeProvider,
    IModeContext modeContext) : IVelocityProfileService
{
    public async Task<VelocityProfile> SetAsync(SetVelocityProfileRequest setVelocityProfileRequest, CancellationToken stoppingToken)
    {
        await EnforceSchemaAsync(setVelocityProfileRequest, stoppingToken);

        await EnforceRulesSurviveAsync(
            setVelocityProfileRequest.OrganizationId,
            setVelocityProfileRequest.Metrics,
            setVelocityProfileRequest.Thresholds,
            stoppingToken);

        var existing = await velocityProfileRepository.FindAsync(setVelocityProfileRequest.OrganizationId, new IncludeDeleted(true), stoppingToken);

        if (existing == null)
        {
            await velocityProfileRepository.AddAsync(setVelocityProfileRequest.ToModel(timeProvider, modeContext.CurrentOrLive), stoppingToken);
        }
        else
        {
            await velocityProfileRepository.ReplaceAsync(
                setVelocityProfileRequest.OrganizationId,
                setVelocityProfileRequest.Metrics,
                setVelocityProfileRequest.Thresholds,
                stoppingToken);
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);

        await BackfillAsync(setVelocityProfileRequest, existing, stoppingToken);

        return await GetAsync(setVelocityProfileRequest.OrganizationId, stoppingToken);
    }

    public async Task<VelocityProfile> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var velocityProfile = await velocityProfileRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        return velocityProfile ?? throw new InvalidOperationException($"No velocity profile is defined for organisation '{organizationId}'.");
    }

    public async Task<VelocityProfile> GetIncludingDeletedAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var velocityProfile = await velocityProfileRepository.FindAsync(organizationId, new IncludeDeleted(true), stoppingToken);

        return velocityProfile ?? throw new InvalidOperationException($"No velocity profile has ever been defined for organisation '{organizationId}'.");
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return velocityProfileRepository.ExistsAsync(organizationId, includeDeleted, stoppingToken);
    }

    public async Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        await EnforceRulesSurviveAsync(organizationId, null, null, stoppingToken);

        await velocityProfileRepository.DeleteAsync(organizationId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        await velocityProfileRepository.RecoverAsync(organizationId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task<List<VelocityProfileUsage>> ListUsagesAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var rules = await ruleRepository.ListByOrganizationAsync(organizationId, stoppingToken);

        return rules
            .Select(x => new { Rule = x, Names = VelocityHelper.CollectNames(x.RuleBody.ToModel()) })
            .Where(x => x.Names.Metrics.Count > 0)
            .Select(x => new VelocityProfileUsage
            {
                RuleId = x.Rule.RuleId,
                FriendlyName = x.Rule.FriendlyName,
                IsEnabled = x.Rule.IsEnabled,
                Metrics = x.Names.Metrics,
                Thresholds = x.Names.Thresholds
            })
            .ToList();
    }

    public async Task EnforceRuleBodyAsync(OrganizationId organizationId, RuleBodyModel ruleBody, CancellationToken stoppingToken)
    {
        if (VelocityHelper.CollectConditions(ruleBody).Count == 0)
        {
            return;
        }

        var velocityProfile = await velocityProfileRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);
        var transactionSchema = await transactionSchemaRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        var violations = VelocityHelper.ValidateRuleBody(ruleBody, velocityProfile, transactionSchema);

        if (violations.Count > 0)
        {
            throw new VelocityRuleViolationException(violations);
        }
    }

    public Task<VelocityBackfill> GetBackfillAsync(VelocityBackfillId velocityBackfillId, CancellationToken stoppingToken)
    {
        return velocityBackfillRepository.GetAsync(velocityBackfillId, stoppingToken);
    }

    public Task<Exists> BackfillExistsAsync(OrganizationId organizationId, VelocityBackfillId velocityBackfillId, CancellationToken stoppingToken)
    {
        return velocityBackfillRepository.ExistsAsync(organizationId, velocityBackfillId, stoppingToken);
    }

    public Task<PagedResponse<VelocityBackfill>> ListAsync(ListVelocityBackfillRequest listVelocityBackfillRequest, CancellationToken stoppingToken)
    {
        return velocityBackfillRepository.ListAsync(listVelocityBackfillRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListVelocityBackfillRequest listVelocityBackfillRequest, CancellationToken stoppingToken)
    {
        return velocityBackfillRepository.GetCountAsync(listVelocityBackfillRequest, stoppingToken);
    }

    private async Task BackfillAsync(SetVelocityProfileRequest setVelocityProfileRequest, VelocityProfile? existing, CancellationToken stoppingToken)
    {
        var declared = (existing?.Metrics ?? [])
            .Select(x => (string)x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = setVelocityProfileRequest.Metrics
            .Where(x => !declared.Contains(x.Name))
            .Select(x => x.Name)
            .ToList();

        if (existing != null && existing.IsDeleted)
        {
            added = setVelocityProfileRequest.Metrics.ConvertAll(x => x.Name);
        }

        if (added.Count == 0)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        var velocityBackfill = new VelocityBackfill
        {
            Mode = modeContext.CurrentOrLive,
            VelocityBackfillId = new VelocityBackfillId(Guid.NewGuid()),
            OrganizationId = setVelocityProfileRequest.OrganizationId,
            Metrics = added,
            Status = VelocityBackfillStatus.Pending,
            TotalTransactions = new TotalCount(0),
            ProcessedCount = new ProcessedCount(0),
            CompletedAt = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };

        await velocityBackfillRepository.AddAsync(velocityBackfill, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        var job = JobBuilder.Create<VelocityBackfillJob>()
            .WithIdentity(velocityBackfill.VelocityBackfillId.ToString(), "VelocityBackfills")
            .StoreDurably()
            .RequestRecovery()
            .Build();

        var trigger = TriggerBuilder.Create()
            .ForJob(job)
            .WithIdentity(velocityBackfill.VelocityBackfillId.ToString(), "VelocityBackfills")
            .StartNow()
            .Build();

        var scheduler = await schedulerFactory.GetScheduler(stoppingToken);
        await scheduler.ScheduleJob(job, trigger, stoppingToken);
    }

    private async Task EnforceRulesSurviveAsync(
        OrganizationId organizationId,
        List<VelocityMetric>? metrics,
        List<VelocityThreshold>? thresholds,
        CancellationToken stoppingToken)
    {
        var rules = await ruleRepository.ListByOrganizationAsync(organizationId, stoppingToken);
        var transactionSchema = await transactionSchemaRepository.FindAsync(organizationId, new IncludeDeleted(false), stoppingToken);

        var now = timeProvider.GetUtcNow();

        var prospective = metrics == null
            ? null
            : new VelocityProfile
            {
                Mode = modeContext.CurrentOrLive,
                VelocityProfileId = new VelocityProfileId(Guid.Empty),
                OrganizationId = organizationId,
                Metrics = metrics,
                Thresholds = thresholds ?? [],
                CreatedByUserId = new ApplicationUserId(Guid.Empty),
                CreatedAt = new CreatedAt(now),
                UpdatedAt = new UpdatedAt(now),
                IsDeleted = new IsDeleted(false),
                DeletedAt = null
            };

        var stranded = new List<VelocityProfileUsage>();

        foreach (var rule in rules)
        {
            var ruleBody = rule.RuleBody.ToModel();
            var names = VelocityHelper.CollectNames(ruleBody);

            if (names.Metrics.Count == 0 || VelocityHelper.ValidateRuleBody(ruleBody, prospective, transactionSchema).Count == 0)
            {
                continue;
            }

            stranded.Add(new VelocityProfileUsage
            {
                RuleId = rule.RuleId,
                FriendlyName = rule.FriendlyName,
                IsEnabled = rule.IsEnabled,
                Metrics = names.Metrics,
                Thresholds = names.Thresholds
            });
        }

        if (stranded.Count > 0)
        {
            throw new VelocityProfileInUseException(stranded);
        }
    }

    private async Task EnforceSchemaAsync(SetVelocityProfileRequest setVelocityProfileRequest, CancellationToken stoppingToken)
    {
        var transactionSchema = await transactionSchemaRepository.FindAsync(setVelocityProfileRequest.OrganizationId, new IncludeDeleted(false), stoppingToken);

        if (transactionSchema == null)
        {
            return;
        }

        var violations = TransactionSchemaHelper.ValidateVelocityProfile(setVelocityProfileRequest.Metrics, transactionSchema);

        if (violations.Count > 0)
        {
            throw new VelocityProfileSchemaViolationException(violations);
        }
    }
}
