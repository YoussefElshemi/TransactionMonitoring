using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Permissions;
using Core.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PermissionScopeRepository(TransactionMonitoringDbContext context) : IPermissionScopeRepository
{
    public Task<ResourceScope?> FindCaseScopeAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        return context.Cases
            .Where(x => x.CaseId == caseId)
            .Select(x => new ResourceScope
            {
                OrganizationId = new OrganizationId(x.OrganizationId),
                SubOrganizationId = x.Queue != null && x.Queue.SubOrganizationId.HasValue
                    ? new SubOrganizationId(x.Queue.SubOrganizationId.Value)
                    : null,
                RuleSetId = null,
                QueueId = x.QueueId.HasValue ? new QueueId(x.QueueId.Value) : null
            })
            .FirstOrDefaultAsync(stoppingToken);
    }

    public Task<ResourceScope?> FindQueueScopeAsync(QueueId queueId, CancellationToken stoppingToken)
    {
        return context.Queues
            .Where(x => x.QueueId == queueId)
            .Select(x => new ResourceScope
            {
                OrganizationId = new OrganizationId(x.OrganizationId),
                SubOrganizationId = x.SubOrganizationId.HasValue ? new SubOrganizationId(x.SubOrganizationId.Value) : null,
                RuleSetId = null,
                QueueId = new QueueId(x.QueueId)
            })
            .FirstOrDefaultAsync(stoppingToken);
    }

    public Task<ResourceScope?> FindRuleScopeAsync(RuleId ruleId, CancellationToken stoppingToken)
    {
        return context.Rules
            .Where(x => x.RuleId == ruleId)
            .Select(x => new ResourceScope
            {
                OrganizationId = new OrganizationId(x.OrganizationId),
                SubOrganizationId = x.RuleSet != null && x.RuleSet.SubOrganizationId.HasValue
                    ? new SubOrganizationId(x.RuleSet.SubOrganizationId.Value)
                    : null,
                RuleSetId = new RuleSetId(x.RuleSetId),
                QueueId = null
            })
            .FirstOrDefaultAsync(stoppingToken);
    }

    public Task<ResourceScope?> FindRuleSetScopeAsync(RuleSetId ruleSetId, CancellationToken stoppingToken)
    {
        return context.RuleSets
            .Where(x => x.RuleSetId == ruleSetId)
            .Select(x => new ResourceScope
            {
                OrganizationId = new OrganizationId(x.OrganizationId),
                SubOrganizationId = x.SubOrganizationId.HasValue ? new SubOrganizationId(x.SubOrganizationId.Value) : null,
                RuleSetId = new RuleSetId(x.RuleSetId),
                QueueId = null
            })
            .FirstOrDefaultAsync(stoppingToken);
    }

    public async Task<Exists> IsScopeWithinOrganizationAsync(OrganizationId organizationId, PermissionScope scope, CancellationToken stoppingToken)
    {
        var exists = scope.ScopeType switch
        {
            PermissionScopeType.Organization => true,

            PermissionScopeType.SubOrganization when scope.SubOrganizationId.HasValue =>
                await context.SubOrganizations.AnyAsync(
                    x => x.SubOrganizationId == scope.SubOrganizationId.Value && x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken),

            PermissionScopeType.RuleSet when scope.RuleSetId.HasValue =>
                await context.RuleSets.AnyAsync(
                    x => x.RuleSetId == scope.RuleSetId.Value && x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken),

            PermissionScopeType.Queue when scope.QueueId.HasValue =>
                await context.Queues.AnyAsync(
                    x => x.QueueId == scope.QueueId.Value && x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken),

            _ => false
        };

        return new Exists(exists);
    }

    public async Task<List<QueueId>> ListQueueIdsInSubOrganizationAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken)
    {
        var queueIds = await context.Queues
            .Where(x => x.SubOrganizationId == (Guid)subOrganizationId && !x.IsDeleted)
            .Select(x => x.QueueId)
            .ToListAsync(stoppingToken);

        return queueIds.ConvertAll(x => new QueueId(x));
    }

    public async Task<List<RuleSetId>> ListRuleSetIdsInSubOrganizationAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken)
    {
        var ruleSetIds = await context.RuleSets
            .Where(x => x.SubOrganizationId == (Guid)subOrganizationId && !x.IsDeleted)
            .Select(x => x.RuleSetId)
            .ToListAsync(stoppingToken);

        return ruleSetIds.ConvertAll(x => new RuleSetId(x));
    }
}
