using Core.Models.Availability;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IAvailabilityService
{
    Task MergeAsync(IReadOnlyList<MergeAvailabilityWindowRequest> mergeAvailabilityWindowRequests, CancellationToken stoppingToken);

    Task<AvailabilityMeasurement> MeasureAsync(MeasureAvailabilityRequest measureAvailabilityRequest, CancellationToken stoppingToken);

    Task<AvailabilityMeasurement> MeasureMonthAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken);
}
