using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Services;
using Quartz;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class NotifyOverdueFilingJob(INotificationService notificationService, IModeContext modeContext) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var notified = await ModeSweepHelper.SweepEachModeAsync(modeContext, () => notificationService.NotifyOverdueFilingsAsync(context.CancellationToken));

        activity?.SetTag("suspicious_activity_reports.overdue_notified", (int)notified);
    }
}
