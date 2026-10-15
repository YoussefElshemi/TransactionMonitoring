using Core.Configs;
using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Status;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class StatusService(
    IComponentProbeRepository componentProbeRepository,
    IComponentUptimeDayRepository componentUptimeDayRepository,
    IIncidentService incidentService,
    IMaintenanceWindowService maintenanceWindowService,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider) : IStatusService
{
    private readonly StatusConfig _statusConfig = appConfig.Value.StatusConfig;

    public async Task<ServiceStatus> GetAsync(CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();
        var activeIncidents = await incidentService.ListActiveAsync(stoppingToken);
        var upcomingMaintenance = await maintenanceWindowService.ListUpcomingAsync(stoppingToken);

        var toDate = new ObservationDate(DateOnly.FromDateTime(now.UtcDateTime));
        var fromDate = new ObservationDate(((DateOnly)toDate).AddDays(-(StatusConstants.PublicHistoryDays - 1)));
        var history = await componentUptimeDayRepository.ListAsync(fromDate, toDate, stoppingToken);

        var components = new List<ComponentHealth>();

        foreach (var component in Enum.GetValues<ServiceComponent>())
        {
            var recentProbes = await componentProbeRepository.ListRecentAsync(
                component, new Count(_statusConfig.UnhealthyProbeThreshold), stoppingToken);

            var componentIncidents = activeIncidents.Where(x => x.Components.Contains(component)).ToList();
            var componentHistory = history.Where(x => x.Component == component).ToList();

            var underMaintenance = upcomingMaintenance.Any(x =>
                x.Components.Contains(component) &&
                x.ScheduledStart <= now &&
                x.ScheduledEnd > now);

            components.Add(new ComponentHealth
            {
                Component = component,
                Status = ComponentStatusResolver.Resolve(
                    recentProbes,
                    componentIncidents,
                    underMaintenance,
                    _statusConfig.UnhealthyProbeThreshold,
                    _statusConfig.DegradedLatencyMilliseconds),
                LastProbedAt = recentProbes.Count == 0 ? null : recentProbes[0].ObservedAt,
                LatencyMilliseconds = recentProbes.Count == 0 ? null : recentProbes[0].LatencyMilliseconds,

                UptimePercentage = AvailabilityCalculator.CalculateUptimePercentage(
                    componentHistory.Sum(x => (long)(int)x.TotalProbes),
                    componentHistory.Sum(x => (long)(int)x.TotalProbes - (int)x.HealthyProbes)),

                History = componentHistory
            });
        }

        return new ServiceStatus
        {
            Status = ComponentStatusResolver.Worst(components.Select(x => x.Status)),
            ObservedAt = new ObservedAt(now),
            Components = components,
            ActiveIncidents = activeIncidents,
            UpcomingMaintenance = upcomingMaintenance
        };
    }

    public async Task<ComponentProbe> RecordProbeAsync(RecordComponentProbeRequest recordComponentProbeRequest, CancellationToken stoppingToken)
    {
        var componentProbe = recordComponentProbeRequest.ToModel(timeProvider);

        await componentProbeRepository.AddAsync(componentProbe, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        // The roll-up job only revisits today and yesterday, so a late observation must re-roll its own day here.
        var observedOn = DateOnly.FromDateTime(((DateTimeOffset)componentProbe.ObservedAt).UtcDateTime);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        if (observedOn < today)
        {
            await RollUpDayAsync(new ObservationDate(observedOn), stoppingToken);
        }

        return componentProbe;
    }

    public async Task<ComponentStatus> GetComponentStatusAsync(ServiceComponent component, CancellationToken stoppingToken)
    {
        var recentProbes = await componentProbeRepository.ListRecentAsync(
            component, new Count(_statusConfig.UnhealthyProbeThreshold), stoppingToken);

        var activeIncidents = await incidentService.ListActiveAsync(stoppingToken);

        return ComponentStatusResolver.Resolve(
            recentProbes,
            activeIncidents.Where(x => x.Components.Contains(component)).ToList(),
            false,
            _statusConfig.UnhealthyProbeThreshold,
            _statusConfig.DegradedLatencyMilliseconds);
    }

    public async Task<ServiceHeartbeat> GetHeartbeatAsync(CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();
        var intervalSeconds = StatusConstants.ProbeIntervalSeconds;
        var tolerance = TimeSpan.FromSeconds((double)intervalSeconds * _statusConfig.HeartbeatToleranceIntervals);

        var components = new List<ComponentHeartbeat>();

        foreach (var component in Enum.GetValues<ServiceComponent>())
        {
            var recentProbes = await componentProbeRepository.ListRecentAsync(component, new Count(1), stoppingToken);
            ObservedAt? lastProbedAt = recentProbes.Count == 0 ? null : recentProbes[0].ObservedAt;
            TimeSpan? staleFor = lastProbedAt is null ? null : now - lastProbedAt.Value;

            components.Add(new ComponentHeartbeat
            {
                Component = component,
                LastProbedAt = lastProbedAt,

                StaleForSeconds = staleFor is null ? null : new Count((int)Math.Max(staleFor.Value.TotalSeconds, 0)),

                IsFresh = new IsHealthy(staleFor is not null && staleFor.Value <= tolerance)
            });
        }

        return new ServiceHeartbeat
        {
            ObservedAt = new ObservedAt(now),
            ExpectedIntervalSeconds = new Count(intervalSeconds),
            Components = components,
            IsFresh = new IsHealthy(components.TrueForAll(x => x.IsFresh))
        };
    }

    public Task<List<ComponentProbe>> ListRecentProbesAsync(ServiceComponent component, Count count, CancellationToken stoppingToken)
    {
        return componentProbeRepository.ListRecentAsync(component, count, stoppingToken);
    }

    public async Task<Count> RollUpDayAsync(ObservationDate date, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();
        var dayStart = new DateTimeOffset(((DateOnly)date).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var announcedMaintenance = await maintenanceWindowService.ListAnnouncedOverlappingAsync(
            new PeriodStart(dayStart), new PeriodEnd(dayStart.AddDays(1)), stoppingToken);

        var rolled = 0;

        foreach (var component in Enum.GetValues<ServiceComponent>())
        {
            var probes = await componentProbeRepository.ListForDayAsync(component, date, stoppingToken);

            var componentUptimeDay = UptimeDayCalculator.Calculate(
                new ComponentUptimeDayId(Guid.NewGuid()),
                component,
                date,
                probes,
                announcedMaintenance,
                now);

            await componentUptimeDayRepository.UpsertAsync(componentUptimeDay, stoppingToken);
            rolled++;
        }

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new Count(rolled);
    }

    public async Task<Count> PurgeProbesAsync(CancellationToken stoppingToken)
    {
        var threshold = new ObservedAt(timeProvider.GetUtcNow().AddDays(-_statusConfig.ProbeRetentionDays));

        return await componentProbeRepository.PurgeOlderThanAsync(threshold, stoppingToken);
    }
}
