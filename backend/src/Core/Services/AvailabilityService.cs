using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Availability;
using Core.ValueObjects;

namespace Core.Services;

public class AvailabilityService(
    IAvailabilityWindowRepository availabilityWindowRepository,
    IAvailabilityCommitmentService availabilityCommitmentService,
    IMaintenanceWindowService maintenanceWindowService,
    IUnitOfWork unitOfWork) : IAvailabilityService
{
    public async Task MergeAsync(IReadOnlyList<MergeAvailabilityWindowRequest> mergeAvailabilityWindowRequests, CancellationToken stoppingToken)
    {
        if (mergeAvailabilityWindowRequests.Count == 0)
        {
            return;
        }

        foreach (var mergeAvailabilityWindowRequest in mergeAvailabilityWindowRequests)
        {
            await availabilityWindowRepository.MergeAsync(mergeAvailabilityWindowRequest, stoppingToken);
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task<AvailabilityMeasurement> MeasureAsync(MeasureAvailabilityRequest measureAvailabilityRequest, CancellationToken stoppingToken)
    {
        var windows = await availabilityWindowRepository.ListAsync(
            measureAvailabilityRequest.OrganizationId,
            measureAvailabilityRequest.PeriodStart,
            measureAvailabilityRequest.PeriodEnd,
            stoppingToken);

        var announcedMaintenance = await maintenanceWindowService.ListAnnouncedOverlappingAsync(
            measureAvailabilityRequest.PeriodStart,
            measureAvailabilityRequest.PeriodEnd,
            stoppingToken);

        var commitment = await availabilityCommitmentService.GetInEffectAsync(
            measureAvailabilityRequest.OrganizationId,
            ((DateTimeOffset)measureAvailabilityRequest.PeriodEnd).AddTicks(-1),
            stoppingToken);

        return AvailabilityCalculator.Measure(
            measureAvailabilityRequest.OrganizationId,
            measureAvailabilityRequest.PeriodStart,
            measureAvailabilityRequest.PeriodEnd,
            windows,
            announcedMaintenance,
            commitment);
    }

    public Task<AvailabilityMeasurement> MeasureMonthAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken)
    {
        var periodStart = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);

        return MeasureAsync(new MeasureAvailabilityRequest
        {
            OrganizationId = organizationId,
            PeriodStart = new PeriodStart(periodStart),
            PeriodEnd = new PeriodEnd(periodStart.AddMonths(1))
        }, stoppingToken);
    }
}
