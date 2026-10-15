using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.SubOrganizations;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SubOrganizationRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider)
    : PagedRepository<ListSubOrganizationRequest, SubOrganizationEntity, SubOrganization>, ISubOrganizationRepository
{
    private const string LikeEscapeCharacter = "\\";

    public async Task AddAsync(SubOrganization subOrganization, CancellationToken stoppingToken)
    {
        await context.SubOrganizations.AddAsync(subOrganization.ToEntity(), stoppingToken);
    }

    public async Task<SubOrganization> GetAsync(SubOrganizationId subOrganizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var subOrganizationEntity = await context.SubOrganizations
            .FirstAsync(x => x.SubOrganizationId == subOrganizationId && (includeDeleted || !x.IsDeleted), stoppingToken);

        return subOrganizationEntity.ToModel();
    }

    public async Task<SubOrganization?> FindByNameAsync(OrganizationId organizationId, SubOrganizationName name, CancellationToken stoppingToken)
    {
        var value = EscapeLikePattern(name.ToString());

        var subOrganizationEntity = await context.SubOrganizations
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId &&
                                      EF.Functions.ILike(x.Name, value, LikeEscapeCharacter) &&
                                      !x.IsDeleted, stoppingToken);

        return subOrganizationEntity?.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, SubOrganizationId subOrganizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.SubOrganizations
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.SubOrganizationId == subOrganizationId &&
                           (includeDeleted || !x.IsDeleted), stoppingToken);

        return new Exists(exists);
    }

    public async Task<SubOrganizationUsage> GetUsageAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken)
    {
        var subOrganizationEntity = await context.SubOrganizations
            .FirstAsync(x => x.SubOrganizationId == subOrganizationId, stoppingToken);

        var userCount = await context.Users
            .CountAsync(x => x.SubOrganizationId == (Guid)subOrganizationId && !x.IsDeleted, stoppingToken);

        var queueCount = await context.Queues
            .CountAsync(x => x.SubOrganizationId == (Guid)subOrganizationId && !x.IsDeleted, stoppingToken);

        var ruleSetCount = await context.RuleSets
            .CountAsync(x => x.SubOrganizationId == (Guid)subOrganizationId && !x.IsDeleted, stoppingToken);

        var roleAssignmentCount = await context.RoleAssignments
            .CountAsync(x => x.SubOrganizationId == (Guid)subOrganizationId, stoppingToken);

        return new SubOrganizationUsage
        {
            SubOrganizationId = new SubOrganizationId(subOrganizationEntity.SubOrganizationId),
            Name = new SubOrganizationName(subOrganizationEntity.Name),
            UserCount = new TotalCount(userCount),
            QueueCount = new TotalCount(queueCount),
            RuleSetCount = new TotalCount(ruleSetCount),
            RoleAssignmentCount = new TotalCount(roleAssignmentCount)
        };
    }

    public async Task UpdateAsync(UpdateSubOrganizationRequest updateSubOrganizationRequest, CancellationToken stoppingToken)
    {
        var subOrganizationEntity = await context.SubOrganizations
            .FirstAsync(x => x.SubOrganizationId == updateSubOrganizationRequest.SubOrganizationId && !x.IsDeleted, stoppingToken);

        subOrganizationEntity.Name = updateSubOrganizationRequest.Name?.ToString() ?? subOrganizationEntity.Name;
        subOrganizationEntity.Description = updateSubOrganizationRequest.Description?.ToString() ?? subOrganizationEntity.Description;

        if (updateSubOrganizationRequest.ClearExternalReference)
        {
            subOrganizationEntity.ExternalReference = null;
        }
        else if (updateSubOrganizationRequest.ExternalReference.HasValue)
        {
            subOrganizationEntity.ExternalReference = updateSubOrganizationRequest.ExternalReference.Value;
        }

        subOrganizationEntity.IsEnabled = updateSubOrganizationRequest.IsEnabled ?? subOrganizationEntity.IsEnabled;
        subOrganizationEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.SubOrganizations.Update(subOrganizationEntity);
    }

    public async Task DeleteAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken)
    {
        var subOrganizationEntity = await context.SubOrganizations
            .FirstAsync(x => x.SubOrganizationId == subOrganizationId, stoppingToken);

        subOrganizationEntity.IsDeleted = true;
        subOrganizationEntity.DeletedAt = timeProvider.GetUtcNow();
        subOrganizationEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.SubOrganizations.Update(subOrganizationEntity);
    }

    public async Task RecoverAsync(SubOrganizationId subOrganizationId, CancellationToken stoppingToken)
    {
        var subOrganizationEntity = await context.SubOrganizations
            .FirstAsync(x => x.SubOrganizationId == subOrganizationId, stoppingToken);

        subOrganizationEntity.IsDeleted = false;
        subOrganizationEntity.DeletedAt = null;
        subOrganizationEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.SubOrganizations.Update(subOrganizationEntity);
    }

    protected override Task<List<SubOrganization>> MapPageAsync(List<SubOrganizationEntity> items, ListSubOrganizationRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<SubOrganizationEntity> OrderByExpression()
    {
        return new OrderByExpression<SubOrganizationEntity, string>(x => x.Name, false, x => x.CreatedAt);
    }

    protected override IQueryable<SubOrganizationEntity> FilterByPagedRequest(ListSubOrganizationRequest listSubOrganizationRequest)
    {
        var queryable = context.SubOrganizations
            .Where(x => x.OrganizationId == listSubOrganizationRequest.OrganizationId && !x.IsDeleted);

        if (listSubOrganizationRequest.Name.HasValue)
        {
            var term = EscapeLikePattern(listSubOrganizationRequest.Name.Value.ToString());

            queryable = queryable
                .Where(x => EF.Functions.ILike(x.Name, $"%{term}%", LikeEscapeCharacter));
        }

        if (listSubOrganizationRequest.IsEnabled.HasValue)
        {
            queryable = queryable
                .Where(x => x.IsEnabled == listSubOrganizationRequest.IsEnabled.Value);
        }

        return queryable;
    }

    protected override IQueryable<SubOrganizationEntity> FilterByDateRange(IQueryable<SubOrganizationEntity> queryable, DatedRequest datedRequest)
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

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter)
            .Replace("%", LikeEscapeCharacter + "%")
            .Replace("_", LikeEscapeCharacter + "_");
    }
}
