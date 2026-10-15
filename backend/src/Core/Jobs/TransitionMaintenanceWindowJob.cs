using Core.Interfaces.Services;
using Quartz;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class TransitionMaintenanceWindowJob(IMaintenanceWindowService maintenanceWindowService) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        await maintenanceWindowService.TransitionDueAsync(context.CancellationToken);
    }
}
