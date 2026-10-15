using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Services;
using Quartz;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class ExpireHeldDecisionJob(IDecisionService decisionService, IModeContext modeContext) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var expired = await ModeSweepHelper.SweepEachModeAsync(modeContext, () => decisionService.ExpireDueAsync(context.CancellationToken));

        activity?.SetTag("decisions.holds.expired", (int)expired);
    }
}
