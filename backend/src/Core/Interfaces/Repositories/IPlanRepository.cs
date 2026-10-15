using Core.Models.Plans;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IPlanRepository : IPagedRepository<ListPlanRequest, Plan>
{
    Task AddAsync(Plan plan, CancellationToken stoppingToken);
    Task<Plan> GetAsync(PlanId planId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(PlanId planId, CancellationToken stoppingToken);
    Task<Exists> ExistsByNameAsync(PlanName name, OrganizationId? organizationId, CancellationToken stoppingToken);
    Task<Exists> HasSubscriptionsAsync(PlanId planId, CancellationToken stoppingToken);
    Task UpdateAsync(UpdatePlanRequest updatePlanRequest, CancellationToken stoppingToken);
    Task SetCurrentVersionAsync(PlanId planId, VersionNumber versionNumber, CancellationToken stoppingToken);
    Task DeleteAsync(PlanId planId, CancellationToken stoppingToken);
}
