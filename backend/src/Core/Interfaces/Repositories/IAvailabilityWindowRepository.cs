using Core.Models.Availability;
using Core.Models.Revenue;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IAvailabilityWindowRepository
{
    Task MergeAsync(MergeAvailabilityWindowRequest mergeAvailabilityWindowRequest, CancellationToken stoppingToken);

    Task<List<AvailabilityWindow>> ListAsync(OrganizationId organizationId, PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken);

    Task<List<OrganizationRequestTotals>> SumByOrganizationAsync(PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken);
}
