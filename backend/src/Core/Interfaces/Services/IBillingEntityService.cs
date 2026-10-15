using Core.Models.Billing;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IBillingEntityService : IPagedService<ListBillingEntityRequest, BillingEntity>
{
    Task<BillingEntity> CreateAsync(CreateBillingEntityRequest createBillingEntityRequest, CancellationToken stoppingToken);
    Task<BillingEntity> UpdateAsync(UpdateBillingEntityRequest updateBillingEntityRequest, CancellationToken stoppingToken);
    Task<BillingEntity> GetAsync(BillingEntityId billingEntityId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(BillingEntityId billingEntityId, IncludeDisabled includeDisabled, CancellationToken stoppingToken);
    Task DeleteAsync(BillingEntityId billingEntityId, CancellationToken stoppingToken);
}
