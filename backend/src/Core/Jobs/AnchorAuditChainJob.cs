using Core.Interfaces.Services;
using Quartz;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class AnchorAuditChainJob(IAuditService auditService) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var anchored = await auditService.AnchorAsync(context.CancellationToken);

        activity?.SetTag("audit_chains.anchored", (int)anchored);
    }
}
