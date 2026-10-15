using Core.Interfaces.Services;
using Quartz;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class AdvanceDunningJob(IDunningService dunningService) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        await dunningService.ReconcileAsync(context.CancellationToken);
        await dunningService.AdvanceAsync(context.CancellationToken);
    }
}
