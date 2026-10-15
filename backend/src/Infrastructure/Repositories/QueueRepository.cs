using Core.Enums;
using Core.Extensions;
using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Queues;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class QueueRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider)
    : PagedRepository<ListQueueRequest, QueueEntity, Queue>, IQueueRepository
{
    private const string LikeEscapeCharacter = "\\";

    public async Task AddAsync(Queue queue, CancellationToken stoppingToken)
    {
        var queueEntity = queue.ToEntity();
        await context.Queues.AddAsync(queueEntity, stoppingToken);

        await AddRuleCriteriaAsync(queue.QueueId, queue.RuleIds, stoppingToken);
    }

    public async Task<Queue> GetAsync(QueueId queueId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var queueEntity = await Detail()
            .FirstAsync(x => x.QueueId == queueId && (includeDeleted || !x.IsDeleted), stoppingToken);

        return queueEntity.ToModel();
    }

    // Escaped: this is an equality test written as LIKE, and underscores would otherwise be wildcards.
    public async Task<Queue?> FindByNameAsync(OrganizationId organizationId, QueueName name, CancellationToken stoppingToken)
    {
        var value = EscapeLikePattern(name.ToString());

        var queueEntity = await context.Queues
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId &&
                                      EF.Functions.ILike(x.Name, value, LikeEscapeCharacter) &&
                                      !x.IsDeleted, stoppingToken);

        return queueEntity?.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, QueueId queueId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Queues
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.QueueId == queueId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<List<Queue>> GetRoutableAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var queueEntities = await context.Queues
            .Include(x => x.QueueRules)
            .Where(x => x.OrganizationId == organizationId && x.IsEnabled && !x.IsDeleted)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        return queueEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<QueueUsage> GetUsageAsync(QueueId queueId, CancellationToken stoppingToken)
    {
        var queueEntity = await context.Queues
            .FirstAsync(x => x.QueueId == queueId, stoppingToken);

        var caseCount = await context.Cases
            .CountAsync(x => x.QueueId == queueId && !x.IsDeleted, stoppingToken);

        var openCaseCount = await context.Cases
            .CountAsync(x => x.QueueId == queueId &&
                             !x.IsDeleted &&
                             !CaseStatusExtensions.TerminalStatusIds.Contains(x.StatusId), stoppingToken);

        var memberCount = await context.QueueMembers
            .CountAsync(x => x.QueueId == queueId, stoppingToken);

        return new QueueUsage
        {
            QueueId = new QueueId(queueEntity.QueueId),
            Name = new QueueName(queueEntity.Name),
            CaseCount = new TotalCount(caseCount),
            OpenCaseCount = new TotalCount(openCaseCount),
            MemberCount = new TotalCount(memberCount)
        };
    }

    public async Task<List<QueueStatistic>> GetStatisticsAsync(OrganizationId organizationId, QueueId? queueId, CancellationToken stoppingToken)
    {
        var queueEntities = await context.Queues
            .Where(x => x.OrganizationId == organizationId &&
                        !x.IsDeleted &&
                        (!queueId.HasValue || x.QueueId == queueId.Value))
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        if (queueEntities.Count == 0)
        {
            return [];
        }

        var queueIds = queueEntities.ConvertAll(x => x.QueueId);

        var openCases = context.Cases
            .Where(x => x.QueueId.HasValue &&
                        queueIds.Contains(x.QueueId.Value) &&
                        !x.IsDeleted &&
                        !CaseStatusExtensions.TerminalStatusIds.Contains(x.StatusId));

        var waiting = await openCases
            .Where(x => x.AssignedToUserId == null)
            .GroupBy(x => x.QueueId!.Value)
            .Select(x => new { QueueId = x.Key, Count = x.Count(), OldestCreatedAt = x.Min(y => y.CreatedAt) })
            .ToListAsync(stoppingToken);

        var inProgress = await openCases
            .Where(x => x.AssignedToUserId != null)
            .GroupBy(x => x.QueueId!.Value)
            .Select(x => new { QueueId = x.Key, Count = x.Count() })
            .ToListAsync(stoppingToken);

        var memberLoads = await GetMemberLoadsAsync(organizationId, queueIds, stoppingToken);
        var now = timeProvider.GetUtcNow();

        return queueEntities.ConvertAll(queueEntity =>
        {
            var queueWaiting = waiting.Find(x => x.QueueId == queueEntity.QueueId);

            return new QueueStatistic
            {
                QueueId = new QueueId(queueEntity.QueueId),
                Name = new QueueName(queueEntity.Name),
                Priority = new QueuePriority(queueEntity.Priority),
                RoutingPolicy = (QueueRoutingPolicy)queueEntity.RoutingPolicyId,
                IsEnabled = new IsEnabled(queueEntity.IsEnabled),
                Depth = new TotalCount(queueWaiting?.Count ?? 0),
                InProgressCount = new TotalCount(inProgress.Find(x => x.QueueId == queueEntity.QueueId)?.Count ?? 0),
                OldestWaitingFor = queueWaiting is not null ? now - queueWaiting.OldestCreatedAt : null,
                MemberLoads = memberLoads.TryGetValue(queueEntity.QueueId, out var loads) ? loads : []
            };
        });
    }

    public async Task UpdateAsync(UpdateQueueRequest updateQueueRequest, CancellationToken stoppingToken)
    {
        var queueEntity = await context.Queues
            .FirstAsync(x => x.QueueId == updateQueueRequest.QueueId && !x.IsDeleted, stoppingToken);

        queueEntity.Name = updateQueueRequest.Name?.ToString() ?? queueEntity.Name;
        queueEntity.Description = updateQueueRequest.Description?.ToString() ?? queueEntity.Description;
        queueEntity.Priority = updateQueueRequest.Priority ?? queueEntity.Priority;
        queueEntity.RoutingPolicyId = updateQueueRequest.RoutingPolicy.HasValue ? (int)updateQueueRequest.RoutingPolicy.Value : queueEntity.RoutingPolicyId;

        if (updateQueueRequest.ClearMinimumSeverity)
        {
            queueEntity.MinimumSeverityId = null;
        }
        else if (updateQueueRequest.MinimumSeverity.HasValue)
        {
            queueEntity.MinimumSeverityId = (int)updateQueueRequest.MinimumSeverity.Value;
        }

        if (updateQueueRequest.ClearMatcher)
        {
            queueEntity.Matcher = null;
        }
        else if (updateQueueRequest.Matcher is not null)
        {
            queueEntity.Matcher = updateQueueRequest.Matcher.ToBase64String();
        }

        if (updateQueueRequest.ClearSubOrganization)
        {
            queueEntity.SubOrganizationId = null;
        }
        else if (updateQueueRequest.SubOrganizationId.HasValue)
        {
            queueEntity.SubOrganizationId = updateQueueRequest.SubOrganizationId.Value;
        }

        queueEntity.IsEnabled = updateQueueRequest.IsEnabled ?? queueEntity.IsEnabled;
        queueEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Queues.Update(queueEntity);

        if (updateQueueRequest.ClearRuleIds)
        {
            await ReplaceRuleCriteriaAsync(updateQueueRequest.QueueId, [], stoppingToken);
        }
        else if (updateQueueRequest.RuleIds is not null)
        {
            await ReplaceRuleCriteriaAsync(updateQueueRequest.QueueId, updateQueueRequest.RuleIds, stoppingToken);
        }
    }

    public async Task DeleteAsync(QueueId queueId, CancellationToken stoppingToken)
    {
        var queueEntity = await context.Queues
            .FirstAsync(x => x.QueueId == queueId, stoppingToken);

        queueEntity.IsDeleted = true;
        queueEntity.DeletedAt = timeProvider.GetUtcNow();

        context.Queues.Update(queueEntity);
    }

    public async Task RecoverAsync(QueueId queueId, CancellationToken stoppingToken)
    {
        var queueEntity = await context.Queues
            .FirstAsync(x => x.QueueId == queueId, stoppingToken);

        queueEntity.IsDeleted = false;
        queueEntity.DeletedAt = null;

        context.Queues.Update(queueEntity);
    }

    protected override Task<List<Queue>> MapPageAsync(List<QueueEntity> items, ListQueueRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<QueueEntity> OrderByExpression()
    {
        return new OrderByExpression<QueueEntity, int>(x => x.Priority, true, x => x.CreatedAt);
    }

    protected override IQueryable<QueueEntity> FilterByPagedRequest(ListQueueRequest listQueueRequest)
    {
        var queryable = Detail()
            .Where(x => x.OrganizationId == listQueueRequest.OrganizationId && !x.IsDeleted)
            .ApplyPermissionFilter(listQueueRequest.PermissionFilter);

        if (listQueueRequest.Name.HasValue)
        {
            var term = EscapeLikePattern(listQueueRequest.Name.Value.ToString());

            queryable = queryable
                .Where(x => EF.Functions.ILike(x.Name, $"%{term}%", LikeEscapeCharacter));
        }

        if (listQueueRequest.RoutingPolicy.HasValue)
        {
            queryable = queryable
                .Where(x => x.RoutingPolicyId == (int)listQueueRequest.RoutingPolicy.Value);
        }

        if (listQueueRequest.IsEnabled.HasValue)
        {
            queryable = queryable
                .Where(x => x.IsEnabled == listQueueRequest.IsEnabled.Value);
        }

        if (listQueueRequest.MemberUserId.HasValue)
        {
            Guid memberUserId = listQueueRequest.MemberUserId.Value;

            queryable = queryable
                .Where(x => x.Members.Any(y => y.UserId == memberUserId));
        }

        return queryable;
    }

    protected override IQueryable<QueueEntity> FilterByDateRange(IQueryable<QueueEntity> queryable, DatedRequest datedRequest)
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

    private IQueryable<QueueEntity> Detail()
    {
        return context.Queues
            .Include(x => x.QueueRules)
            .Include(x => x.Members)
            .ThenInclude(x => x.User);
    }

    private async Task<Dictionary<Guid, List<QueueMemberLoad>>> GetMemberLoadsAsync(
        OrganizationId organizationId,
        List<Guid> queueIds,
        CancellationToken stoppingToken)
    {
        var members = await context.QueueMembers
            .Include(x => x.User)
            .ThenInclude(x => x.Organization)
            .Where(x => queueIds.Contains(x.QueueId))
            .ToListAsync(stoppingToken);

        if (members.Count == 0)
        {
            return [];
        }

        var counts = await OpenCaseCountsByUserAsync(organizationId, members.ConvertAll(x => x.UserId), stoppingToken);

        return members
            .GroupBy(x => x.QueueId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(member => new QueueMemberLoad
                {
                    UserId = new ApplicationUserId(member.UserId),
                    User = member.User is not null ? member.User.ToModel() : null,
                    OpenCaseCount = new OpenCaseCount(counts.GetValueOrDefault(member.UserId, 0)),
                    LastAssignedAt = member.LastAssignedAt.HasValue ? new LastAssignedAt(member.LastAssignedAt.Value) : null
                }).ToList());
    }

    private async Task<Dictionary<Guid, int>> OpenCaseCountsByUserAsync(
        OrganizationId organizationId,
        List<Guid> userIds,
        CancellationToken stoppingToken)
    {
        var counts = await context.Cases
            .Where(x => x.OrganizationId == organizationId &&
                        x.AssignedToUserId.HasValue &&
                        userIds.Contains(x.AssignedToUserId.Value) &&
                        !x.IsDeleted &&
                        !CaseStatusExtensions.TerminalStatusIds.Contains(x.StatusId))
            .GroupBy(x => x.AssignedToUserId!.Value)
            .Select(x => new { UserId = x.Key, Count = x.Count() })
            .ToListAsync(stoppingToken);

        return counts.ToDictionary(x => x.UserId, x => x.Count);
    }

    private async Task AddRuleCriteriaAsync(QueueId queueId, List<RuleId> ruleIds, CancellationToken stoppingToken)
    {
        if (ruleIds.Count == 0)
        {
            return;
        }

        var queueRuleEntities = ruleIds.ConvertAll(x => new QueueRuleEntity
        {
            QueueId = queueId,
            RuleId = x,
            CreatedAt = timeProvider.GetUtcNow(),
            UpdatedAt = timeProvider.GetUtcNow()
        });

        await context.QueueRules.AddRangeAsync(queueRuleEntities, stoppingToken);
    }

    private async Task ReplaceRuleCriteriaAsync(QueueId queueId, List<RuleId> ruleIds, CancellationToken stoppingToken)
    {
        var existing = await context.QueueRules
            .Where(x => x.QueueId == queueId)
            .ToListAsync(stoppingToken);

        context.QueueRules.RemoveRange(existing);

        await AddRuleCriteriaAsync(queueId, ruleIds, stoppingToken);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);
    }
}
