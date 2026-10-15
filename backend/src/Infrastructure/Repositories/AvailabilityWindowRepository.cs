using Core.Constants;
using Core.Interfaces.Repositories;
using Core.Models.Availability;
using Core.Models.Revenue;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AvailabilityWindowRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : IAvailabilityWindowRepository
{
    public async Task MergeAsync(MergeAvailabilityWindowRequest mergeAvailabilityWindowRequest, CancellationToken stoppingToken)
    {
        var organizationId = (Guid)mergeAvailabilityWindowRequest.OrganizationId;
        var periodStart = (DateTimeOffset)mergeAvailabilityWindowRequest.PeriodStart;
        var now = timeProvider.GetUtcNow();

        var availabilityWindowEntity = await context.AvailabilityWindows
            .Include(x => x.LatencyBuckets)
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.PeriodStart == periodStart, stoppingToken);

        var isNew = false;

        if (availabilityWindowEntity is null)
        {
            isNew = true;
            availabilityWindowEntity = new AvailabilityWindowEntity
            {
                AvailabilityWindowId = Guid.NewGuid(),
                OrganizationId = organizationId,
                PeriodStart = periodStart,
                PeriodEnd = periodStart.AddMinutes(AvailabilityConstants.WindowMinutes),
                TotalRequests = 0,
                FailedRequests = 0,
                TotalLatencyMilliseconds = 0,
                MaxLatencyMilliseconds = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            await context.AvailabilityWindows.AddAsync(availabilityWindowEntity, stoppingToken);
        }

        availabilityWindowEntity.TotalRequests += mergeAvailabilityWindowRequest.TotalRequests;
        availabilityWindowEntity.FailedRequests += mergeAvailabilityWindowRequest.FailedRequests;
        availabilityWindowEntity.TotalLatencyMilliseconds += mergeAvailabilityWindowRequest.TotalLatencyMilliseconds;

        availabilityWindowEntity.MaxLatencyMilliseconds = Math.Max(
            availabilityWindowEntity.MaxLatencyMilliseconds,
            mergeAvailabilityWindowRequest.MaxLatencyMilliseconds);

        availabilityWindowEntity.UpdatedAt = now;
        var latencyBucketsByBound = availabilityWindowEntity.LatencyBuckets
            .ToDictionary(x => x.UpperBoundMilliseconds);

        foreach (var bucket in mergeAvailabilityWindowRequest.LatencyBuckets)
        {
            var bound = (int)bucket.UpperBoundMilliseconds;

            if (!latencyBucketsByBound.TryGetValue(bound, out var existing))
            {
                existing = new AvailabilityWindowLatencyBucketEntity
                {
                    AvailabilityWindowId = availabilityWindowEntity.AvailabilityWindowId,
                    UpperBoundMilliseconds = bound,
                    Count = bucket.Count,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                latencyBucketsByBound.Add(bound, existing);
                await context.AvailabilityWindowLatencyBuckets.AddAsync(existing, stoppingToken);
                continue;
            }

            existing.Count += bucket.Count;
            existing.UpdatedAt = now;

            if (context.Entry(existing).State != EntityState.Added)
            {
                context.AvailabilityWindowLatencyBuckets.Update(existing);
            }
        }

        if (!isNew)
        {
            context.AvailabilityWindows.Update(availabilityWindowEntity);
        }
    }

    public async Task<List<AvailabilityWindow>> ListAsync(OrganizationId organizationId, PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken)
    {
        var from = (DateTimeOffset)periodStart;
        var to = (DateTimeOffset)periodEnd;

        var availabilityWindowEntities = await context.AvailabilityWindows
            .Include(x => x.LatencyBuckets)
            .Where(x => x.OrganizationId == organizationId &&
                        x.PeriodStart >= from &&
                        x.PeriodStart < to)
            .OrderBy(x => x.PeriodStart)
            .ToListAsync(stoppingToken);

        return availabilityWindowEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<List<OrganizationRequestTotals>> SumByOrganizationAsync(
        PeriodStart periodStart, PeriodEnd periodEnd, CancellationToken stoppingToken)
    {
        var from = (DateTimeOffset)periodStart;
        var to = (DateTimeOffset)periodEnd;

        var grouped = await context.AvailabilityWindows
            .Where(x => x.PeriodStart >= from && x.PeriodStart < to)
            .GroupBy(x => x.OrganizationId)
            .Select(group => new
            {
                OrganizationId = group.Key,
                TotalRequests = group.Sum(x => x.TotalRequests),
                FailedRequests = group.Sum(x => x.FailedRequests)
            })
            .ToListAsync(stoppingToken);

        return grouped.ConvertAll(x => new OrganizationRequestTotals
        {
            OrganizationId = new OrganizationId(x.OrganizationId),
            TotalRequests = new RequestCount(x.TotalRequests),
            FailedRequests = new RequestCount(x.FailedRequests)
        });
    }
}
