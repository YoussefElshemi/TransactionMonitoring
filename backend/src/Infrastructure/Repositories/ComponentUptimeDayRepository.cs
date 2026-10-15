using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Status;
using Core.ValueObjects;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ComponentUptimeDayRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : IComponentUptimeDayRepository
{
    public async Task UpsertAsync(ComponentUptimeDay componentUptimeDay, CancellationToken stoppingToken)
    {
        var date = (DateOnly)componentUptimeDay.Date;
        var componentId = (int)componentUptimeDay.Component;

        var existing = await context.ComponentUptimeDays
            .FirstOrDefaultAsync(x => x.ComponentId == componentId && x.Date == date, stoppingToken);

        if (existing is null)
        {
            await context.ComponentUptimeDays.AddAsync(componentUptimeDay.ToEntity(), stoppingToken);

            return;
        }

        existing.TotalProbes = componentUptimeDay.TotalProbes;
        existing.HealthyProbes = componentUptimeDay.HealthyProbes;
        existing.DowntimeSeconds = componentUptimeDay.DowntimeSeconds;
        existing.UptimePercentage = componentUptimeDay.UptimePercentage;
        existing.UpdatedAt = timeProvider.GetUtcNow();

        context.ComponentUptimeDays.Update(existing);
    }

    public async Task<List<ComponentUptimeDay>> ListAsync(ObservationDate fromDate, ObservationDate toDate, CancellationToken stoppingToken)
    {
        var from = (DateOnly)fromDate;
        var to = (DateOnly)toDate;

        var componentUptimeDayEntities = await context.ComponentUptimeDays
            .Where(x => x.Date >= from && x.Date <= to)
            .OrderBy(x => x.ComponentId)
            .ThenBy(x => x.Date)
            .ToListAsync(stoppingToken);

        return componentUptimeDayEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<ComponentUptimeDay?> GetAsync(ServiceComponent component, ObservationDate date, CancellationToken stoppingToken)
    {
        var day = (DateOnly)date;

        var componentUptimeDayEntity = await context.ComponentUptimeDays
            .FirstOrDefaultAsync(x => x.ComponentId == (int)component && x.Date == day, stoppingToken);

        return componentUptimeDayEntity?.ToModel();
    }
}
