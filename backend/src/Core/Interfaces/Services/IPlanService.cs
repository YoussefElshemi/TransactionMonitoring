using Core.Models.Common;
using Core.Models.Plans;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IPlanService : IPagedService<ListPlanRequest, Plan>
{
    Task<Plan> CreateAsync(CreatePlanRequest createPlanRequest, CancellationToken stoppingToken);
    Task<Plan> GetAsync(PlanId planId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(PlanId planId, CancellationToken stoppingToken);
    Task<Exists> ExistsByNameAsync(PlanName name, OrganizationId? organizationId, CancellationToken stoppingToken);
    Task<Plan> UpdateAsync(UpdatePlanRequest updatePlanRequest, CancellationToken stoppingToken);
    Task DeleteAsync(PlanId planId, CancellationToken stoppingToken);

    Task<PlanVersion> PublishVersionAsync(PublishPlanVersionRequest publishPlanVersionRequest, CancellationToken stoppingToken);
    Task<PlanVersion> GetVersionAsync(PlanVersionId planVersionId, CancellationToken stoppingToken);
    Task<Exists> VersionExistsAsync(PlanVersionId planVersionId, CancellationToken stoppingToken);
    Task<PagedResponse<PlanVersion>> ListVersionsAsync(ListPlanVersionRequest listPlanVersionRequest, CancellationToken stoppingToken);
    Task<TotalCount> GetVersionCountAsync(ListPlanVersionRequest listPlanVersionRequest, CancellationToken stoppingToken);
}
