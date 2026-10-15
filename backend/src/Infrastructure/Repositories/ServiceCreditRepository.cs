using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.ServiceCredits;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ServiceCreditRepository(
    TransactionMonitoringDbContext context) : PagedRepository<ListServiceCreditRequest, ServiceCreditEntity, ServiceCredit>, IServiceCreditRepository
{
    public async Task AddAsync(ServiceCredit serviceCredit, CancellationToken stoppingToken)
    {
        var serviceCreditEntity = serviceCredit.ToEntity();
        await context.ServiceCredits.AddAsync(serviceCreditEntity, stoppingToken);
    }

    public async Task<ServiceCredit> GetAsync(ServiceCreditId serviceCreditId, CancellationToken stoppingToken)
    {
        var serviceCreditEntity = await context.ServiceCredits
            .FirstAsync(x => x.ServiceCreditId == serviceCreditId, stoppingToken);

        return serviceCreditEntity.ToModel();
    }

    public async Task<Exists> ExistsByPeriodAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken)
    {
        var exists = await context.ServiceCredits
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.Month == month &&
                           x.Year == year, stoppingToken);

        return new Exists(exists);
    }

    protected override Task<List<ServiceCredit>> MapPageAsync(List<ServiceCreditEntity> items, ListServiceCreditRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<ServiceCreditEntity> OrderByExpression()
    {
        return new OrderByExpression<ServiceCreditEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<ServiceCreditEntity> FilterByPagedRequest(ListServiceCreditRequest listServiceCreditRequest)
    {
        var queryable = context.ServiceCredits
            .Where(x => x.OrganizationId == listServiceCreditRequest.OrganizationId);

        if (!listServiceCreditRequest.IncludeMet)
        {
            queryable = queryable.Where(x => x.CreditPercentage > 0);
        }

        return queryable;
    }

    protected override IQueryable<ServiceCreditEntity> FilterByDateRange(IQueryable<ServiceCreditEntity> queryable, DatedRequest datedRequest)
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
