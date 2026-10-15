using Core.Models.Plans;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IPlanVersionRepository : IPagedRepository<ListPlanVersionRequest, PlanVersion>
{
    Task AddAsync(PlanVersion planVersion, CancellationToken stoppingToken);
    Task<PlanVersion> GetAsync(PlanVersionId planVersionId, CancellationToken stoppingToken);
    Task<PlanVersion?> GetCurrentAsync(PlanId planId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(PlanVersionId planVersionId, CancellationToken stoppingToken);
    Task<VersionNumber> GetNextVersionNumberAsync(PlanId planId, CancellationToken stoppingToken);
}
