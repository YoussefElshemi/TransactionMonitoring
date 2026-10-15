using Core.Interfaces.Services;
using Core.ValueObjects;
using Quartz;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class RollUpComponentUptimeJob(IStatusService statusService, TimeProvider timeProvider) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        await statusService.RollUpDayAsync(new ObservationDate(today.AddDays(-1)), context.CancellationToken);
        await statusService.RollUpDayAsync(new ObservationDate(today), context.CancellationToken);

        await statusService.PurgeProbesAsync(context.CancellationToken);
    }
}
