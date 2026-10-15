using Core.Interfaces.Services;
using Quartz;

namespace Core.Jobs;

[DisallowConcurrentExecution]
public class PurgeIdempotencyRecordJob(IIdempotencyService idempotencyService) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context)
    {
        using var activity = OpenTelemetry.OpenTelemetry.MyActivitySource.StartActivity(GetType().Name);

        var purged = await idempotencyService.PurgeExpiredAsync(context.CancellationToken);

        activity?.SetTag("idempotency.records.purged", (int)purged);
    }
}
