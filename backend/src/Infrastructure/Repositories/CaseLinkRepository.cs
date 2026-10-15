using Core.Interfaces.Repositories;
using Core.Models.CaseLinks;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CaseLinkRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListCaseLinkRequest, CaseLinkEntity, CaseLink>, ICaseLinkRepository
{
    public async Task AddAsync(CaseLink caseLink, CancellationToken stoppingToken)
    {
        var caseLinkEntity = caseLink.ToEntity();
        await context.CaseLinks.AddAsync(caseLinkEntity, stoppingToken);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, CaseLinkId caseLinkId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.CaseLinks
            .AnyAsync(x => x.CaseLinkId == caseLinkId &&
                           x.OrganizationId == organizationId &&
                           (x.CaseId == caseId || x.LinkedCaseId == caseId) &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> ExistsBetweenAsync(OrganizationId organizationId, CaseId caseId, CaseId linkedCaseId, CancellationToken stoppingToken)
    {
        var exists = await context.CaseLinks
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           !x.IsDeleted &&
                           ((x.CaseId == caseId && x.LinkedCaseId == linkedCaseId) ||
                            (x.CaseId == linkedCaseId && x.LinkedCaseId == caseId)), stoppingToken);

        return new Exists(exists);
    }

    public async Task<CaseLink> GetAsync(CaseLinkId caseLinkId, CaseId viewpointCaseId, CancellationToken stoppingToken)
    {
        var caseLinkEntity = await Detail()
            .FirstAsync(x => x.CaseLinkId == caseLinkId && !x.IsDeleted, stoppingToken);

        return caseLinkEntity.ToModel(viewpointCaseId);
    }

    public async Task DeleteAsync(CaseLinkId caseLinkId, CancellationToken stoppingToken)
    {
        var caseLinkEntity = await context.CaseLinks
            .FirstAsync(x => x.CaseLinkId == caseLinkId, stoppingToken);

        caseLinkEntity.IsDeleted = true;
        caseLinkEntity.DeletedAt = timeProvider.GetUtcNow();

        context.CaseLinks.Update(caseLinkEntity);
    }

    protected override Task<List<CaseLink>> MapPageAsync(List<CaseLinkEntity> items, ListCaseLinkRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel(request.CaseId)));
    }

    protected override IOrderByExpression<CaseLinkEntity> OrderByExpression()
    {
        return new OrderByExpression<CaseLinkEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<CaseLinkEntity> FilterByPagedRequest(ListCaseLinkRequest listCaseLinkRequest)
    {
        return Detail()
            .Where(x => x.OrganizationId == listCaseLinkRequest.OrganizationId &&
                        !x.IsDeleted &&
                        (x.CaseId == listCaseLinkRequest.CaseId || x.LinkedCaseId == listCaseLinkRequest.CaseId));
    }

    protected override IQueryable<CaseLinkEntity> FilterByDateRange(IQueryable<CaseLinkEntity> queryable, DatedRequest datedRequest)
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

    private IQueryable<CaseLinkEntity> Detail()
    {
        return context.CaseLinks
            .IgnoreAutoIncludes()
            .Include(x => x.Case)
            .Include(x => x.LinkedCase);
    }
}
