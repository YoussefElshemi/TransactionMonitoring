using Core.Enums;
using Core.Extensions;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Common;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SuspiciousActivityReportRepository(
    TransactionMonitoringDbContext context,
    IPayloadProtectionService payloadProtectionService,
    TimeProvider timeProvider) : PagedRepository<ListSuspiciousActivityReportRequest, SuspiciousActivityReportEntity, SuspiciousActivityReport>,
    ISuspiciousActivityReportRepository
{
    public async Task AddAsync(SuspiciousActivityReport suspiciousActivityReport, CancellationToken stoppingToken)
    {
        await context.SuspiciousActivityReports.AddAsync(suspiciousActivityReport.ToEntity(), stoppingToken);
    }

    public async Task<SuspiciousActivityReport> GetAsync(SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken)
    {
        var suspiciousActivityReportEntity = await WithUsers()
            .FirstAsync(x => x.SuspiciousActivityReportId == suspiciousActivityReportId, stoppingToken);

        return suspiciousActivityReportEntity.ToModel();
    }

    public async Task<List<SuspiciousActivityReport>> ListByIdsAsync(
        IReadOnlyCollection<SuspiciousActivityReportId> suspiciousActivityReportIds,
        CancellationToken stoppingToken)
    {
        if (suspiciousActivityReportIds.Count == 0)
        {
            return [];
        }

        var suspiciousActivityReportIdValues = suspiciousActivityReportIds.Select(x => (Guid)x).Distinct().ToList();

        var suspiciousActivityReportEntities = await WithUsers()
            .Where(x => suspiciousActivityReportIdValues.Contains(x.SuspiciousActivityReportId))
            .ToListAsync(stoppingToken);

        return suspiciousActivityReportEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<Exists> ExistsAsync(
        OrganizationId organizationId,
        SuspiciousActivityReportId suspiciousActivityReportId,
        CancellationToken stoppingToken)
    {
        var exists = await context.SuspiciousActivityReports
            .AnyAsync(x => x.OrganizationId == organizationId && x.SuspiciousActivityReportId == suspiciousActivityReportId, stoppingToken);

        return new Exists(exists);
    }

    public async Task<SuspiciousActivityReport?> FindByCaseAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var suspiciousActivityReportEntity = await WithUsers()
            .FirstOrDefaultAsync(x => x.CaseId == caseId, stoppingToken);

        return suspiciousActivityReportEntity?.ToModel();
    }

    public async Task<SuspiciousActivityReport> UpdateNarrativeAsync(
        UpdateSuspiciousActivityReportRequest updateSuspiciousActivityReportRequest,
        CancellationToken stoppingToken)
    {
        var suspiciousActivityReportEntity = await context.SuspiciousActivityReports
            .FirstAsync(x => x.SuspiciousActivityReportId == updateSuspiciousActivityReportRequest.SuspiciousActivityReportId, stoppingToken);

        suspiciousActivityReportEntity.Narrative = updateSuspiciousActivityReportRequest.Narrative;
        suspiciousActivityReportEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.SuspiciousActivityReports.Update(suspiciousActivityReportEntity);

        return suspiciousActivityReportEntity.ToModel();
    }

    // Loaded without users: Update attaches the whole graph, and a pinned user blocks the approval path from attaching.
    public async Task<SuspiciousActivityReport> TransitionAsync(
        SuspiciousActivityReportTransition suspiciousActivityReportTransition,
        CancellationToken stoppingToken)
    {
        var suspiciousActivityReportEntity = await context.SuspiciousActivityReports
            .FirstAsync(x => x.SuspiciousActivityReportId == suspiciousActivityReportTransition.SuspiciousActivityReportId, stoppingToken);

        var now = timeProvider.GetUtcNow();

        suspiciousActivityReportEntity.StatusId = (int)suspiciousActivityReportTransition.Status;

        if (suspiciousActivityReportTransition.FilingReference.HasValue)
        {
            suspiciousActivityReportEntity.FiledByUserId = suspiciousActivityReportTransition.FiledByUserId.HasValue
                ? suspiciousActivityReportTransition.FiledByUserId.Value
                : null;
            suspiciousActivityReportEntity.FiledAt = now;
            suspiciousActivityReportEntity.FilingReference = suspiciousActivityReportTransition.FilingReference.Value.ToString();
        }

        if (suspiciousActivityReportTransition.Status == SuspiciousActivityReportStatus.Acknowledged)
        {
            suspiciousActivityReportEntity.AcknowledgedAt = now;
            suspiciousActivityReportEntity.AcknowledgementReference = suspiciousActivityReportTransition.AcknowledgementReference?.ToString();
        }

        if (suspiciousActivityReportTransition.NotFiledReason.HasValue)
        {
            suspiciousActivityReportEntity.NotFiledReason = suspiciousActivityReportTransition.NotFiledReason.Value.ToString();
            suspiciousActivityReportEntity.DecidedByUserId = suspiciousActivityReportTransition.DecidedByUserId.HasValue
                ? suspiciousActivityReportTransition.DecidedByUserId.Value
                : null;
            suspiciousActivityReportEntity.DecidedAt = now;
        }

        suspiciousActivityReportEntity.UpdatedAt = now;

        context.SuspiciousActivityReports.Update(suspiciousActivityReportEntity);

        return suspiciousActivityReportEntity.ToModel();
    }

    public async Task<FriendlyName> GenerateFriendlyName(CancellationToken stoppingToken)
    {
        var result = await context.Database
            .SqlQueryRaw<int>($"SELECT nextval('\"{Schemas.Service}\".\"{Sequences.SuspiciousActivityReportNumber}\"') as \"Value\"")
            .FirstAsync(stoppingToken);

        return new FriendlyName("SAR" + result.ToString("D10"));
    }

    public async Task<List<SuspiciousActivityReport>> ListOverdueNotificationCandidatesAsync(Count batchSize, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();

        var suspiciousActivityReportEntities = await context.SuspiciousActivityReports
            .Where(x => x.OverdueNotifiedAt == null &&
                        x.DueAt < now &&
                        !SuspiciousActivityReportStatusExtensions.TerminalStatusIds.Contains(x.StatusId))
            .OrderBy(x => x.DueAt)
            .ThenBy(x => x.SuspiciousActivityReportId)
            .Take(batchSize)
            .ToListAsync(stoppingToken);

        return suspiciousActivityReportEntities.ConvertAll(x => x.ToModel());
    }

    public async Task MarkOverdueNotifiedAsync(List<SuspiciousActivityReportId> suspiciousActivityReportIds, CancellationToken stoppingToken)
    {
        var ids = suspiciousActivityReportIds.ConvertAll(x => (Guid)x);

        var now = timeProvider.GetUtcNow();

        var suspiciousActivityReportEntities = await context.SuspiciousActivityReports
            .Where(x => ids.Contains(x.SuspiciousActivityReportId))
            .ToListAsync(stoppingToken);

        foreach (var suspiciousActivityReportEntity in suspiciousActivityReportEntities)
        {
            suspiciousActivityReportEntity.OverdueNotifiedAt = now;
            suspiciousActivityReportEntity.UpdatedAt = now;

            context.SuspiciousActivityReports.Update(suspiciousActivityReportEntity);
        }
    }

    public async Task<NarrativeSource> GetNarrativeSourceAsync(CaseId caseId, Count noteLimit, CancellationToken stoppingToken)
    {
        var caseEntity = await context.Cases
            .Include(x => x.ReasonCode)
            .FirstAsync(x => x.CaseId == caseId, stoppingToken);

        var transactionEntity = caseEntity.TransactionId.HasValue
            ? await context.Transactions
                .Include(x => x.Decision)
                .Include(x => x.TransactionParties)
                .ThenInclude(x => x.Party)
                .FirstOrDefaultAsync(x => x.TransactionId == caseEntity.TransactionId.Value, stoppingToken)
            : null;

        var caseNoteEntities = await context.CaseNotes
            .Where(x => x.CaseId == caseId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Take(noteLimit)
            .ToListAsync(stoppingToken);

        // Walked from the evaluations: projecting the navigation off the link table yields an entity type EF refuses to Include onto.
        var ruleEvaluationEntities = await context.RuleEvaluations
            .Include(x => x.Rule)
            .Include(x => x.RuleVersion)
            .Where(x => x.CaseRuleEvaluationLinks.Any(y => y.CaseId == caseId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        var parties = transactionEntity is not null
            ? await ListPartyHistoryAsync(transactionEntity, stoppingToken)
            : [];

        var dataKey = transactionEntity is not null
            ? await payloadProtectionService.FindDataKeyAsync(new OrganizationId(transactionEntity.OrganizationId), stoppingToken)
            : null;

        return new NarrativeSource
        {
            Case = caseEntity.ToModel(),
            Transaction = transactionEntity?.ToModel(dataKey),
            Notes = caseNoteEntities.ConvertAll(x => x.ToModel()),
            RuleEvaluations = ruleEvaluationEntities.ConvertAll(x => x.ToModel()),
            Parties = parties
        };
    }

    private async Task<List<NarrativePartyHistory>> ListPartyHistoryAsync(TransactionEntity transactionEntity, CancellationToken stoppingToken)
    {
        var partyIds = transactionEntity.TransactionParties.Select(x => x.PartyId).Distinct().ToList();

        if (partyIds.Count == 0)
        {
            return [];
        }

        var partyFacts = await context.TransactionParties
            .Where(x => partyIds.Contains(x.PartyId))
            .GroupBy(x => x.PartyId)
            .Select(x => new
            {
                PartyId = x.Key,
                Count = x.Count(),
                RoleIds = x.Select(y => y.PartyRoleId).Distinct().ToList()
            })
            .ToListAsync(stoppingToken);

        var caseCounts = await context.Cases
            .Where(x => x.PartyId != null && partyIds.Contains(x.PartyId.Value) && !x.IsDeleted)
            .GroupBy(x => x.PartyId!.Value)
            .Select(x => new { PartyId = x.Key, Count = x.Count() })
            .ToListAsync(stoppingToken);

        var parties = new List<NarrativePartyHistory>();

        foreach (var transactionPartyEntity in transactionEntity.TransactionParties)
        {
            if (transactionPartyEntity.Party is null)
            {
                continue;
            }

            var facts = partyFacts.FirstOrDefault(x => x.PartyId == transactionPartyEntity.PartyId);
            var roles = facts?.RoleIds.ConvertAll(x => (PartyRole)x) ?? [];

            parties.Add(new NarrativePartyHistory
            {
                Party = transactionPartyEntity.Party.ToModel(roles),
                CaseCount = new TotalCount(caseCounts.FirstOrDefault(x => x.PartyId == transactionPartyEntity.PartyId)?.Count ?? 0),
                TransactionCount = new TotalCount(facts?.Count ?? 0)
            });
        }

        return parties;
    }

    protected override Task<List<SuspiciousActivityReport>> MapPageAsync(
        List<SuspiciousActivityReportEntity> items,
        ListSuspiciousActivityReportRequest request,
        CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<SuspiciousActivityReportEntity> OrderByExpression()
    {
        return new SuspiciousActivityReportOrderByExpression();
    }

    protected override IQueryable<SuspiciousActivityReportEntity> FilterByPagedRequest(
        ListSuspiciousActivityReportRequest listSuspiciousActivityReportRequest)
    {
        var queryable = WithUsers()
            .Where(x => x.OrganizationId == listSuspiciousActivityReportRequest.OrganizationId);

        if (listSuspiciousActivityReportRequest.Status.HasValue)
        {
            queryable = queryable.Where(x => x.StatusId == (int)listSuspiciousActivityReportRequest.Status.Value);
        }

        if (listSuspiciousActivityReportRequest.CaseId.HasValue)
        {
            queryable = queryable.Where(x => x.CaseId == listSuspiciousActivityReportRequest.CaseId.Value);
        }

        if (listSuspiciousActivityReportRequest.IsOverdue.HasValue)
        {
            var now = timeProvider.GetUtcNow();

            queryable = listSuspiciousActivityReportRequest.IsOverdue.Value
                ? queryable.Where(x => x.DueAt < now && !SuspiciousActivityReportStatusExtensions.TerminalStatusIds.Contains(x.StatusId))
                : queryable.Where(x => x.DueAt >= now || SuspiciousActivityReportStatusExtensions.TerminalStatusIds.Contains(x.StatusId));
        }

        return queryable;
    }

    protected override IQueryable<SuspiciousActivityReportEntity> FilterByDateRange(
        IQueryable<SuspiciousActivityReportEntity> queryable,
        DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }

    private IQueryable<SuspiciousActivityReportEntity> WithUsers()
    {
        return context.SuspiciousActivityReports
            .Include(x => x.OpenedByUser)
            .Include(x => x.FiledByUser)
            .Include(x => x.DecidedByUser);
    }
}
