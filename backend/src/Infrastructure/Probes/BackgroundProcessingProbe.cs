using System.Diagnostics;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Status;
using Quartz;
using Quartz.Impl.Matchers;

namespace Infrastructure.Probes;

public class BackgroundProcessingProbe(ISchedulerFactory schedulerFactory) : IServiceComponentProbe
{
    public ServiceComponent Component => ServiceComponent.BackgroundProcessing;

    public async Task<RecordComponentProbeRequest> ProbeAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var scheduler = await schedulerFactory.GetScheduler(stoppingToken);

            if (!scheduler.IsStarted || scheduler.InStandbyMode || scheduler.IsShutdown)
            {
                stopwatch.Stop();

                return ProbeResultFactory.Unhealthy(Component, stopwatch,
                    "The scheduler is not firing triggers: it is shut down or in standby.");
            }

            var triggers = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup(), stoppingToken);

            stopwatch.Stop();

            return triggers.Count == 0
                ? ProbeResultFactory.Unhealthy(Component, stopwatch, "The scheduler has no triggers registered.")
                : ProbeResultFactory.Healthy(Component, stopwatch);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            stopwatch.Stop();

            return ProbeResultFactory.Unhealthy(Component, stopwatch, exception.Message);
        }
    }
}
