using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Services;
using Quartz;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class EscalateCaseJob(IEscalationRuleService escalationRuleService, IModeContext modeContext) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var escalated = await ModeSweepHelper.SweepEachModeAsync(modeContext, () => escalationRuleService.EscalateDueAsync(context.CancellationToken));

        activity?.SetTag("cases.escalated", (int)escalated);
    }
}
