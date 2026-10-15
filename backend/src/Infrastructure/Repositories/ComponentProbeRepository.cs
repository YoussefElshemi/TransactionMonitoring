using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Status;
using Core.ValueObjects;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ComponentProbeRepository(TransactionMonitoringDbContext context) : IComponentProbeRepository
{
    public async Task AddAsync(ComponentProbe componentProbe, CancellationToken stoppingToken)
    {
        var componentProbeEntity = componentProbe.ToEntity();
        await context.ComponentProbes.AddAsync(componentProbeEntity, stoppingToken);
    }

    public async Task<List<ComponentProbe>> ListRecentAsync(ServiceComponent component, Count count, CancellationToken stoppingToken)
    {
        var componentProbeEntities = await context.ComponentProbes
            .Where(x => x.ComponentId == (int)component)
            .OrderByDescending(x => x.ObservedAt)
            .Take(count)
            .ToListAsync(stoppingToken);

        return componentProbeEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<List<ComponentProbe>> ListForDayAsync(ServiceComponent component, ObservationDate date, CancellationToken stoppingToken)
    {
        var from = new DateTimeOffset(((DateOnly)date).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = from.AddDays(1);

        var componentProbeEntities = await context.ComponentProbes
            .Where(x => x.ComponentId == (int)component &&
                        x.ObservedAt >= from &&
                        x.ObservedAt < to)
            .OrderBy(x => x.ObservedAt)
            .ToListAsync(stoppingToken);

        return componentProbeEntities.ConvertAll(x => x.ToModel());
    }

    public async Task<Count> PurgeOlderThanAsync(ObservedAt threshold, CancellationToken stoppingToken)
    {
        var cutoff = (DateTimeOffset)threshold;

        var deleted = await context.ComponentProbes
            .Where(x => x.ObservedAt < cutoff)
            .ExecuteDeleteAsync(stoppingToken);

        return new Count(deleted);
    }
}
