using Core.Configs;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Status;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using Quartz;
using ILogger = Serilog.ILogger;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class ProbeServiceComponentJob(
    IEnumerable<IServiceComponentProbe> probes,
    IStatusService statusService,
    IIncidentService incidentService,
    IOptions<AppConfig> appConfig,
    ILogger logger) : IJob
{
    private readonly StatusConfig _statusConfig = appConfig.Value.StatusConfig;

    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        foreach (var probe in probes)
        {
            try
            {
                var result = await probe.ProbeAsync(context.CancellationToken);
                await statusService.RecordProbeAsync(result, context.CancellationToken);

                if (!_statusConfig.RaiseIncidentsFromProbes)
                {
                    continue;
                }

                await ReconcileIncidentAsync(probe.Component, result, context.CancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.Error(
                    exception,
                    "Probing {Component} failed before the result could be recorded: {Message}. The status page will show this component as stale rather than unhealthy.",
                    probe.Component.ToString(),
                    exception.Message);
            }
        }
    }

    private async Task ReconcileIncidentAsync(ServiceComponent component, RecordComponentProbeRequest result, CancellationToken stoppingToken)
    {
        var openIncidents = await incidentService.ListActiveForComponentAsync(component, stoppingToken);

        if (result.IsHealthy)
        {
            await NoteRecoveryAsync(component, openIncidents, stoppingToken);

            return;
        }

        if (openIncidents.Count > 0)
        {
            return;
        }

        var recentProbes = await statusService.ListRecentProbesAsync(
            component, new Count(_statusConfig.UnhealthyProbeThreshold), stoppingToken);

        if (recentProbes.Count < _statusConfig.UnhealthyProbeThreshold ||
            recentProbes.Any(x => x.IsHealthy))
        {
            return;
        }

        await incidentService.CreateAsync(new CreateIncidentRequest
        {
            Title = new IncidentTitle($"{component} is not responding"),
            Summary = new IncidentSummary(
                $"Automated probes for {component} have failed {_statusConfig.UnhealthyProbeThreshold} times in a row. " +
                (result.Detail is null ? "No detail was reported." : $"The most recent reported: {result.Detail}")),

            Impact = IncidentImpact.Major,

            Components = [component],

            StartedAt = new StartedAt(recentProbes[^1].ObservedAt),

            CreatedByUserId = null
        }, stoppingToken);
    }

    private async Task NoteRecoveryAsync(ServiceComponent component, IReadOnlyList<Incident> openIncidents, CancellationToken stoppingToken)
    {
        foreach (var incident in openIncidents)
        {
            if (incident.CreatedByUserId is not null || incident.Status != IncidentStatus.Investigating)
            {
                continue;
            }

            await incidentService.PostUpdateAsync(new PostIncidentUpdateRequest
            {
                IncidentId = incident.IncidentId,
                Status = IncidentStatus.Monitoring,
                Body = new IncidentUpdateBody($"Automated probes for {component} are passing again. Monitoring."),
                PostedByUserId = null
            }, stoppingToken);
        }
    }
}
