using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Organizations;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class OrganizationRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListOrganizationRequest, OrganizationEntity, Organization>, IOrganizationRepository
{
    public async Task AddAsync(Organization organization, CancellationToken stoppingToken)
    {
        var organizationEntity = organization.ToEntity();
        await context.Organizations.AddAsync(organizationEntity, stoppingToken);
    }

    public async Task UpdateAsync(UpdateOrganizationRequest updateOrganizationRequest, CancellationToken stoppingToken)
    {
        var organizationEntity = await context.Organizations
            .FirstAsync(x => x.OrganizationId == updateOrganizationRequest.OrganizationId && !x.IsDeleted, stoppingToken);

        organizationEntity.FriendlyName = updateOrganizationRequest.FriendlyName ?? organizationEntity.FriendlyName;
        organizationEntity.StatusId = updateOrganizationRequest.Status.HasValue ? (int)updateOrganizationRequest.Status : organizationEntity.StatusId;

        if (updateOrganizationRequest.HoldWindow.IsSet)
        {
            organizationEntity.HoldWindow = updateOrganizationRequest.HoldWindow.Value;
        }

        if (updateOrganizationRequest.HoldExpiryVerdict.IsSet)
        {
            organizationEntity.HoldExpiryVerdictId = (int?)updateOrganizationRequest.HoldExpiryVerdict.Value;
        }

        if (updateOrganizationRequest.DeduplicationWindow.IsSet)
        {
            organizationEntity.DeduplicationWindow = updateOrganizationRequest.DeduplicationWindow.Value;
        }

        if (updateOrganizationRequest.CaseGroupingPeriod.IsSet)
        {
            organizationEntity.CaseGroupingPeriod = updateOrganizationRequest.CaseGroupingPeriod.Value;
        }

        if (updateOrganizationRequest.CaseGroupingEnabled.IsSet)
        {
            organizationEntity.CaseGroupingEnabled = updateOrganizationRequest.CaseGroupingEnabled.Value;
        }

        if (updateOrganizationRequest.DeduplicationEnabled.IsSet)
        {
            organizationEntity.DeduplicationEnabled = updateOrganizationRequest.DeduplicationEnabled.Value;
        }

        if (updateOrganizationRequest.CaseGroupingScope.IsSet)
        {
            organizationEntity.CaseGroupingScopeId = (int?)updateOrganizationRequest.CaseGroupingScope.Value;
        }

        if (updateOrganizationRequest.CaseGroupingSeverityPolicy.IsSet)
        {
            organizationEntity.CaseGroupingSeverityPolicyId = (int?)updateOrganizationRequest.CaseGroupingSeverityPolicy.Value;
        }

        if (updateOrganizationRequest.MaxAlertsPerCase.IsSet)
        {
            organizationEntity.MaxAlertsPerCase = updateOrganizationRequest.MaxAlertsPerCase.Value;
        }

        if (updateOrganizationRequest.SandboxMonthlyCredits.IsSet)
        {
            organizationEntity.SandboxMonthlyCredits = updateOrganizationRequest.SandboxMonthlyCredits.Value;
        }

        if (updateOrganizationRequest.DefaultLocale.IsSet)
        {
            organizationEntity.DefaultLocale = updateOrganizationRequest.DefaultLocale.Value?.ToString();
        }

        if (updateOrganizationRequest.DefaultTimeZone.IsSet)
        {
            organizationEntity.DefaultTimeZone = updateOrganizationRequest.DefaultTimeZone.Value?.ToString();
        }

        organizationEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Organizations.Update(organizationEntity);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Organizations
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<Organization> GetAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var organizationEntity = await context.Organizations
            .FirstAsync(x => x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken);

        return organizationEntity.ToModel();
    }

    public async Task<List<Organization>> ListByIdsAsync(IReadOnlyCollection<OrganizationId> organizationIds, CancellationToken stoppingToken)
    {
        if (organizationIds.Count == 0)
        {
            return [];
        }

        var organizationIdValues = organizationIds.Select(x => (Guid)x).Distinct().ToList();

        var organizationEntities = await context.Organizations
            .Where(x => organizationIdValues.Contains(x.OrganizationId) && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        return organizationEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<List<Organization>> GetAllAsync(CancellationToken stoppingToken)
    {
        var organizationEntities = await context.Organizations
            .Where(x => !x.IsDeleted)
            .ToListAsync(stoppingToken);

        return organizationEntities.Select(x => x.ToModel()).ToList();
    }

    public async Task DeleteAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var organizationEntity = await context.Organizations
            .FirstAsync(x => x.OrganizationId == organizationId && !x.IsDeleted, stoppingToken);

        organizationEntity.IsDeleted = true;
        organizationEntity.DeletedAt = timeProvider.GetUtcNow();

        context.Organizations.Update(organizationEntity);
    }

    public async Task RecoverAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        var organizationEntity = await context.Organizations
            .FirstAsync(x => x.OrganizationId == organizationId, stoppingToken);

        organizationEntity.IsDeleted = false;
        organizationEntity.DeletedAt = null;

        context.Organizations.Update(organizationEntity);
    }

    protected override Task<List<Organization>> MapPageAsync(List<OrganizationEntity> items, ListOrganizationRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<OrganizationEntity> OrderByExpression()
    {
        return new OrderByExpression<OrganizationEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<OrganizationEntity> FilterByPagedRequest(ListOrganizationRequest listOrganizationRequest)
    {
        var queryable = context.Organizations
            .Where(x => !x.IsDeleted);

        if (listOrganizationRequest.FriendlyName.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.FriendlyName, $"%{listOrganizationRequest.FriendlyName}%"));
        }

        return queryable;
    }

    protected override IQueryable<OrganizationEntity> FilterByDateRange(IQueryable<OrganizationEntity> queryable, DatedRequest datedRequest)
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
}