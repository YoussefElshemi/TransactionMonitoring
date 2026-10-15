using Core.Models.Billing;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IBillingEntityRepository : IPagedRepository<ListBillingEntityRequest, BillingEntity>
{
    Task AddAsync(BillingEntity billingEntity, CancellationToken stoppingToken);
    Task<BillingEntity> GetAsync(BillingEntityId billingEntityId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(BillingEntityId billingEntityId, IncludeDisabled includeDisabled, CancellationToken stoppingToken);
    Task UpdateAsync(UpdateBillingEntityRequest updateBillingEntityRequest, CancellationToken stoppingToken);

    Task<Exists> IsInUseAsync(BillingEntityId billingEntityId, CancellationToken stoppingToken);

    Task DeleteAsync(BillingEntityId billingEntityId, CancellationToken stoppingToken);
}
