using Core.Interfaces.Repositories;
using Core.Models.Common;
using Core.Models.Discounts;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ContractDiscountRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListContractDiscountRequest, ContractDiscountEntity, ContractDiscount>, IContractDiscountRepository
{
    public async Task AddAsync(ContractDiscount contractDiscount, CancellationToken stoppingToken)
    {
        var contractDiscountEntity = contractDiscount.ToEntity();
        await context.ContractDiscounts.AddAsync(contractDiscountEntity, stoppingToken);
    }

    public async Task<ContractDiscount> GetAsync(ContractDiscountId contractDiscountId, CancellationToken stoppingToken)
    {
        var contractDiscountEntity = await context.ContractDiscounts
            .FirstAsync(x => x.ContractDiscountId == contractDiscountId && !x.IsDeleted, stoppingToken);

        return contractDiscountEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, ContractDiscountId contractDiscountId, CancellationToken stoppingToken)
    {
        var exists = await context.ContractDiscounts
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.ContractDiscountId == contractDiscountId &&
                           !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<List<ContractDiscount>> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        var contractDiscountEntities = await context.ContractDiscounts
            .Where(x => x.OrganizationId == organizationId &&
                        !x.IsDeleted &&
                        x.EffectiveFrom <= asOf &&
                        (x.EffectiveTo == null || x.EffectiveTo > asOf))
            .OrderBy(x => x.EffectiveFrom)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        return contractDiscountEntities.ConvertAll(x => x.ToModel());
    }

    public async Task EndAsync(ContractDiscountId contractDiscountId, EffectiveTo effectiveTo, CancellationToken stoppingToken)
    {
        var contractDiscountEntity = await context.ContractDiscounts
            .FirstAsync(x => x.ContractDiscountId == contractDiscountId && !x.IsDeleted, stoppingToken);

        contractDiscountEntity.EffectiveTo = effectiveTo;
        contractDiscountEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.ContractDiscounts.Update(contractDiscountEntity);
    }

    public async Task DeleteAsync(ContractDiscountId contractDiscountId, CancellationToken stoppingToken)
    {
        var contractDiscountEntity = await context.ContractDiscounts
            .FirstAsync(x => x.ContractDiscountId == contractDiscountId && !x.IsDeleted, stoppingToken);

        contractDiscountEntity.IsDeleted = true;
        contractDiscountEntity.DeletedAt = timeProvider.GetUtcNow();
        contractDiscountEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.ContractDiscounts.Update(contractDiscountEntity);
    }

    protected override Task<List<ContractDiscount>> MapPageAsync(List<ContractDiscountEntity> items, ListContractDiscountRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<ContractDiscountEntity> OrderByExpression()
    {
        return new OrderByExpression<ContractDiscountEntity, DateTimeOffset>(x => x.EffectiveFrom);
    }

    protected override IQueryable<ContractDiscountEntity> FilterByPagedRequest(ListContractDiscountRequest listContractDiscountRequest)
    {
        var queryable = context.ContractDiscounts
            .Where(x => x.OrganizationId == listContractDiscountRequest.OrganizationId && !x.IsDeleted);

        if (!listContractDiscountRequest.IncludeExpired)
        {
            var now = timeProvider.GetUtcNow();
            queryable = queryable.Where(x => x.EffectiveTo == null || x.EffectiveTo > now);
        }

        return queryable;
    }

    protected override IQueryable<ContractDiscountEntity> FilterByDateRange(IQueryable<ContractDiscountEntity> queryable, DatedRequest datedRequest)
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
