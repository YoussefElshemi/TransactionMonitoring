using Core.Interfaces.Repositories;
using Core.Models.Billing;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BillingEntityRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListBillingEntityRequest, BillingEntityEntity, BillingEntity>, IBillingEntityRepository
{
    public async Task AddAsync(BillingEntity billingEntity, CancellationToken stoppingToken)
    {
        await context.BillingEntities.AddAsync(billingEntity.ToEntity(), stoppingToken);
    }

    public async Task<BillingEntity> GetAsync(BillingEntityId billingEntityId, CancellationToken stoppingToken)
    {
        var billingEntityEntity = await context.BillingEntities
            .FirstAsync(x => x.BillingEntityId == billingEntityId && !x.IsDeleted, stoppingToken);

        return billingEntityEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(BillingEntityId billingEntityId, IncludeDisabled includeDisabled, CancellationToken stoppingToken)
    {
        var exists = await context.BillingEntities
            .AnyAsync(x => x.BillingEntityId == billingEntityId &&
                           !x.IsDeleted &&
                           (includeDisabled || x.IsEnabled), stoppingToken);

        return new Exists(exists);
    }

    public async Task UpdateAsync(UpdateBillingEntityRequest updateBillingEntityRequest, CancellationToken stoppingToken)
    {
        var billingEntityEntity = await context.BillingEntities
            .FirstAsync(x => x.BillingEntityId == updateBillingEntityRequest.BillingEntityId && !x.IsDeleted, stoppingToken);

        billingEntityEntity.LegalName = updateBillingEntityRequest.LegalName ?? billingEntityEntity.LegalName;
        billingEntityEntity.Address = updateBillingEntityRequest.Address ?? billingEntityEntity.Address;
        billingEntityEntity.CountryCode = updateBillingEntityRequest.CountryCode ?? billingEntityEntity.CountryCode;

        billingEntityEntity.TaxIdentifier = updateBillingEntityRequest.TaxIdentifier.HasValue
            ? updateBillingEntityRequest.TaxIdentifier.Value
            : null;

        billingEntityEntity.CurrencyId = updateBillingEntityRequest.Currency.HasValue
            ? (int)updateBillingEntityRequest.Currency.Value
            : billingEntityEntity.CurrencyId;

        billingEntityEntity.IsEnabled = updateBillingEntityRequest.IsEnabled ?? billingEntityEntity.IsEnabled;
        billingEntityEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.BillingEntities.Update(billingEntityEntity);
    }

    public async Task<Exists> IsInUseAsync(BillingEntityId billingEntityId, CancellationToken stoppingToken)
    {
        var hasProfiles = await context.BillingProfiles
            .AnyAsync(x => x.BillingEntityId == billingEntityId && !x.IsDeleted, stoppingToken);

        if (hasProfiles)
        {
            return new Exists(true);
        }

        var hasInvoices = await context.Invoices
            .AnyAsync(x => x.BillingEntityId == billingEntityId, stoppingToken);

        return new Exists(hasInvoices);
    }

    public async Task DeleteAsync(BillingEntityId billingEntityId, CancellationToken stoppingToken)
    {
        var billingEntityEntity = await context.BillingEntities
            .FirstAsync(x => x.BillingEntityId == billingEntityId && !x.IsDeleted, stoppingToken);

        billingEntityEntity.IsDeleted = true;
        billingEntityEntity.DeletedAt = timeProvider.GetUtcNow();
        billingEntityEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.BillingEntities.Update(billingEntityEntity);
    }

    protected override Task<List<BillingEntity>> MapPageAsync(List<BillingEntityEntity> items, ListBillingEntityRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<BillingEntityEntity> OrderByExpression()
    {
        return new OrderByExpression<BillingEntityEntity, string>(x => x.LegalName);
    }

    protected override IQueryable<BillingEntityEntity> FilterByPagedRequest(ListBillingEntityRequest listBillingEntityRequest)
    {
        var queryable = context.BillingEntities.Where(x => !x.IsDeleted);

        if (!listBillingEntityRequest.IncludeDisabled)
        {
            queryable = queryable.Where(x => x.IsEnabled);
        }

        return queryable;
    }

    protected override IQueryable<BillingEntityEntity> FilterByDateRange(IQueryable<BillingEntityEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable.Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}
